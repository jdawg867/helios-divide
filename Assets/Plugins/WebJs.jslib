mergeInto(LibraryManager.library, {

    sendMethod: function (str) {
        window.unityCallback(UTF8ToString(str));
    },

    IsInSeele: function() {
        return (window.unityCallback != undefined) ? true : false;
    },

    IsInSeeleOnline: function() {
        if (
          (location.hostname === "www.seeles.ai" || location.hostname === "seeles.ai") &&
          new URLSearchParams(location.search).get("debug") !== "true"
        ) {
          return true;
        }
        return false;
    },

    LogViaConsole: function(ptr) {
        var msg = UTF8ToString(ptr)
        console.log(msg);
    },

    InitRealtimeJS: function (urlPtr, keyPtr, myIdPtr, roomIdPtr, playerMetaJsonPtr) {
        var DEFAULT_SUPABASE_URL = "https://wlrdtgttnidaqviujsyz.supabase.co";
        var DEFAULT_SUPABASE_PUBLISHABLE_KEY = "sb_publishable_quLfjK1EgrPmRGFCoavQow_G_nQ721t";

        var normalizeConfigValue = function (value) {
            if (typeof value !== 'string') return "";
            var trimmed = value.trim();
            var markdownLinkMatch = trimmed.match(/^\[(https?:\/\/[^\]]+)\]\((https?:\/\/[^\)]+)\)$/);
            if (markdownLinkMatch) return markdownLinkMatch[2];
            return trimmed.replace(/^\[|\]$/g, "");
        };

        var resolveRuntimeConfig = function () {
            var appConfig = window.APP_CONFIG || window.__APP_CONFIG || window.__SEELE_CONFIG || {};
            return {
                url: normalizeConfigValue(appConfig.NEXT_PUBLIC_SUPABASE_URL || window.NEXT_PUBLIC_SUPABASE_URL || DEFAULT_SUPABASE_URL),
                key: normalizeConfigValue(appConfig.NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY || window.NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY || DEFAULT_SUPABASE_PUBLISHABLE_KEY)
            };
        };

        var runtimeConfig = resolveRuntimeConfig();
        var url = normalizeConfigValue(urlPtr ? UTF8ToString(urlPtr) : "") || runtimeConfig.url;
        var key = normalizeConfigValue(keyPtr ? UTF8ToString(keyPtr) : "") || runtimeConfig.key;
        var myId = UTF8ToString(myIdPtr);
        var roomId = UTF8ToString(roomIdPtr);
        var playerMetaJson = playerMetaJsonPtr ? UTF8ToString(playerMetaJsonPtr) : "{}";

        if (typeof window.unityInstance === 'undefined' && typeof unityInstance !== 'undefined') {
            window.unityInstance = unityInstance;
        }

        if (!window.__sbRT) window.__sbRT = {};
        var rt = window.__sbRT;

        if (rt.channel && rt.roomId === roomId && rt.myId === myId) return;

        var sendToUnity = function (method, data) {
            if (window.unityInstance) {
                window.unityInstance.SendMessage("NetworkManager", method, data || "");
            }
        };

        var safeJsonParse = function (str, fallback) {
            try { return JSON.parse(str); } catch (_) { return fallback; }
        };

        var cleanupCurrent = function () {
            if (rt.channel && rt.client) {
                try { rt.channel.unsubscribe(); } catch (_) {}
                try { rt.client.removeChannel(rt.channel); } catch (_) {}
            }
            rt.channel = null;
            rt.connected = false;
        };

        cleanupCurrent();

        rt.url = url;
        rt.key = key;
        rt.myId = myId;
        rt.roomId = roomId;
        rt.seq = 0;
        rt.connected = false;
        rt.lastSeqByPlayer = {};
        rt.processedReliableEvents = {};
        rt.playerMeta = safeJsonParse(playerMetaJson, {});

        var handleEnvelope = function (payload, unityMethod) {
            var env = payload && payload.payload ? payload.payload : null;
            if (!env || !env.playerId || !env.type) return;
            if (env.roomId !== roomId) return;

            var lastSeq = rt.lastSeqByPlayer[env.playerId] || 0;
            var isReliable = !!env.reliable;
            var eventId = env.eventId || "";

            if (!isReliable) {
                if (env.seq <= lastSeq) return;
                rt.lastSeqByPlayer[env.playerId] = env.seq;
            } else {
                if (eventId && rt.processedReliableEvents[eventId]) return;
                if (eventId) rt.processedReliableEvents[eventId] = true;
                if (env.seq > lastSeq) rt.lastSeqByPlayer[env.playerId] = env.seq;
            }

            sendToUnity(unityMethod, JSON.stringify(env));
        };

        var runInit = function () {
            try {
                if (!window.supabase || !window.supabase.createClient) {
                    sendToUnity("OnRealtimeError", JSON.stringify({ code: "SDK_NOT_READY", message: "Supabase SDK not ready" }));
                    return;
                }

                if (!url || !key) {
                    sendToUnity("OnRealtimeError", JSON.stringify({ code: "CONFIG_MISSING", message: "Supabase URL or publishable key missing" }));
                    return;
                }

                rt.client = window.supabase.createClient(url, key);
                rt.channel = rt.client.channel("room:" + roomId, {
                    config: {
                        presence: { key: myId },
                        broadcast: { self: false }
                    }
                });

                rt.channel
                    .on('broadcast', { event: 'player_move' }, function (payload) {
                        handleEnvelope(payload, "OnReceiveMove");
                    })
                    .on('broadcast', { event: 'food_eaten' }, function (payload) {
                        handleEnvelope(payload, "OnReceiveFoodEaten");
                    })
                    .on('broadcast', { event: 'player_eaten' }, function (payload) {
                        handleEnvelope(payload, "OnReceivePlayerEaten");
                    })
                    .on('broadcast', { event: 'spore_ejected' }, function (payload) {
                        handleEnvelope(payload, "OnReceiveSporeEjected");
                    })
                    .on('broadcast', { event: 'spore_eaten' }, function (payload) {
                        handleEnvelope(payload, "OnReceiveSporeEaten");
                    })
                    .on('broadcast', { event: 'snapshot_request' }, function (payload) {
                        handleEnvelope(payload, "OnSnapshotRequest");
                    })
                    .on('broadcast', { event: 'snapshot_state' }, function (payload) {
                        handleEnvelope(payload, "OnSnapshotState");
                    })
                    .on('presence', { event: 'sync' }, function () {
                        var state = rt.channel.presenceState();
                        sendToUnity("OnPresenceSync", JSON.stringify(state));
                    })
                    .on('presence', { event: 'join' }, function (payload) {
                        sendToUnity("OnPresenceJoin", JSON.stringify(payload));
                    })
                    .on('presence', { event: 'leave' }, function (payload) {
                        sendToUnity("OnPresenceLeave", JSON.stringify(payload));
                    })
                    .subscribe(function (status) {
                        if (status === 'SUBSCRIBED') {
                            rt.connected = true;
                            sendToUnity("OnRealtimeConnected", JSON.stringify({ roomId: roomId, playerId: myId }));
                            rt.channel.track({
                                userId: myId,
                                roomId: roomId,
                                joinedAt: new Date().toISOString(),
                                meta: rt.playerMeta
                            });
                        } else if (status === 'CHANNEL_ERROR') {
                            rt.connected = false;
                            sendToUnity("OnRealtimeError", JSON.stringify({ code: "CHANNEL_ERROR", roomId: roomId }));
                        } else if (status === 'TIMED_OUT') {
                            rt.connected = false;
                            sendToUnity("OnRealtimeDisconnected", JSON.stringify({ code: "TIMED_OUT", roomId: roomId }));
                        } else if (status === 'CLOSED') {
                            rt.connected = false;
                            sendToUnity("OnRealtimeDisconnected", JSON.stringify({ code: "CLOSED", roomId: roomId }));
                        }
                    });
            } catch (e) {
                sendToUnity("OnRealtimeError", JSON.stringify({ code: "INIT_EXCEPTION", message: String(e) }));
            }
        };

        if (typeof window.supabase === 'undefined') {
            var existing = document.getElementById("supabase-sdk-script");
            if (existing) {
                existing.addEventListener("load", runInit, { once: true });
                return;
            }

            var script = document.createElement('script');
            script.id = "supabase-sdk-script";
            script.src = "https://cdn.jsdelivr.net/npm/@supabase/supabase-js";
            script.onload = runInit;
            script.onerror = function () {
                sendToUnity("OnRealtimeError", JSON.stringify({ code: "SDK_LOAD_FAILED" }));
            };
            document.head.appendChild(script);
        } else {
            runInit();
        }
    },

    SendPlayerDataJS: function (jsonPtr) {
        var rt = window.__sbRT;
        if (!rt || !rt.channel || !rt.connected) return;

        try {
            var data = JSON.parse(UTF8ToString(jsonPtr));
            rt.channel.send({
                type: 'broadcast',
                event: 'player_move',
                payload: {
                    type: 'player_move',
                    roomId: rt.roomId,
                    playerId: rt.myId,
                    seq: ++rt.seq,
                    ts: Date.now(),
                    eventId: rt.myId + "_" + Date.now() + "_move",
                    reliable: false,
                    data: data
                }
            });
        } catch (_) {}
    },

    SendReliableEventJS: function (eventTypePtr, jsonPtr) {
        var rt = window.__sbRT;
        if (!rt || !rt.channel || !rt.connected) return;

        try {
            var eventType = UTF8ToString(eventTypePtr);
            var data = JSON.parse(UTF8ToString(jsonPtr));
            rt.channel.send({
                type: 'broadcast',
                event: eventType,
                payload: {
                    type: eventType,
                    roomId: rt.roomId,
                    playerId: rt.myId,
                    seq: ++rt.seq,
                    ts: Date.now(),
                    eventId: rt.myId + "_" + Date.now() + "_" + Math.floor(Math.random() * 1000000),
                    reliable: true,
                    data: data
                }
            });
        } catch (_) {}
    },

    RequestSnapshotJS: function () {
        var rt = window.__sbRT;
        if (!rt || !rt.channel || !rt.connected) return;

        rt.channel.send({
            type: 'broadcast',
            event: 'snapshot_request',
            payload: {
                type: 'snapshot_request',
                roomId: rt.roomId,
                playerId: rt.myId,
                seq: ++rt.seq,
                ts: Date.now(),
                eventId: rt.myId + "_" + Date.now() + "_snapshot_req",
                reliable: true,
                data: {}
            }
        });
    },

    SendSnapshotStateJS: function (jsonPtr) {
        var rt = window.__sbRT;
        if (!rt || !rt.channel || !rt.connected) return;

        try {
            var data = JSON.parse(UTF8ToString(jsonPtr));
            rt.channel.send({
                type: 'broadcast',
                event: 'snapshot_state',
                payload: {
                    type: 'snapshot_state',
                    roomId: rt.roomId,
                    playerId: rt.myId,
                    seq: ++rt.seq,
                    ts: Date.now(),
                    eventId: rt.myId + "_" + Date.now() + "_snapshot_state",
                    reliable: true,
                    data: data
                }
            });
        } catch (_) {}
    },

    IsRealtimeConnectedJS: function () {
        var rt = window.__sbRT;
        return (rt && rt.connected) ? 1 : 0;
    },

    DisposeSupabaseJS: function () {
        var rt = window.__sbRT;
        if (!rt) return;

        try {
            if (rt.channel) rt.channel.unsubscribe();
            if (rt.client && rt.channel) rt.client.removeChannel(rt.channel);
        } catch (_) {}

        rt.channel = null;
        rt.client = null;
        rt.connected = false;
    }
});
