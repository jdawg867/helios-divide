using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用事件转发类
/// 使用队列管理事件，使用字典管理事件类型和对应的回调列表
/// </summary>
public class EventDispatcher
{
    /// <summary>
    /// 事件数据基类
    /// </summary>
    public class EventData
    {
        public Enum eventType;
        public object[] parameters;
        
        public EventData(Enum type, params object[] args)
        {
            eventType = type;
            parameters = args;
        }
    }

    // 事件队列字典：key是事件类型（枚举），value是该类型事件的队列
    private static Dictionary<Enum, Queue<EventData>> eventQueues = new Dictionary<Enum, Queue<EventData>>();
    
    // 回调字典：key是事件类型（枚举），value是该类型事件的回调列表
    private static Dictionary<Enum, List<Action<EventData>>> eventCallbacks = new Dictionary<Enum, List<Action<EventData>>>();
    
    // 复用列表，避免 BroadcastAllEvents 每帧 new List
    private static List<Enum> _eventTypesToProcess = new List<Enum>();
    
    // 队列消费驱动（游戏启动时创建）
    private static EventDispatcherDriver _driver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateDriver()
    {
        if (_driver != null) return;
        var go = new GameObject("EventDispatcherDriver");
        _driver = go.AddComponent<EventDispatcherDriver>();
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    // 单例实例
    private static EventDispatcher instance;
    public static EventDispatcher Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new EventDispatcher();
            }
            return instance;
        }
    }

    #region 注册和注销回调

    /// <summary>
    /// 注册事件监听回调
    /// </summary>
    /// <param name="eventType">事件类型（枚举）</param>
    /// <param name="callback">回调函数</param>
    public static void Register(Enum eventType, Action<EventData> callback)
    {
        // 如果该事件类型还没有回调列表，创建一个
        if (!eventCallbacks.ContainsKey(eventType))
        {
            eventCallbacks[eventType] = new List<Action<EventData>>();
        }
        
        // 添加回调到列表
        if (!eventCallbacks[eventType].Contains(callback))
        {
            eventCallbacks[eventType].Add(callback);
            Debug.Log($"[EventDispatcher] 注册回调: 事件类型={eventType}, 当前回调数={eventCallbacks[eventType].Count}");
        }
    }

    /// <summary>
    /// 注销事件监听回调
    /// </summary>
    /// <param name="eventType">事件类型（枚举）</param>
    /// <param name="callback">回调函数</param>
    public static void Unregister(Enum eventType, Action<EventData> callback)
    {
        if (eventCallbacks.ContainsKey(eventType))
        {
            eventCallbacks[eventType].Remove(callback);
            Debug.Log($"[EventDispatcher] 注销回调: 事件类型={eventType}, 剩余回调数={eventCallbacks[eventType].Count}");
            
            // 如果该事件类型没有回调了，删除整个列表
            if (eventCallbacks[eventType].Count == 0)
            {
                eventCallbacks.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// 注销某个事件类型的所有回调
    /// </summary>
    /// <param name="eventType">事件类型（枚举）</param>
    public static void UnregisterAll(Enum eventType)
    {
        if (eventCallbacks.ContainsKey(eventType))
        {
            int count = eventCallbacks[eventType].Count;
            eventCallbacks.Remove(eventType);
            Debug.Log($"[EventDispatcher] 注销所有回调: 事件类型={eventType}, 已注销{count}个回调");
        }
    }

    /// <summary>
    /// 清空所有事件回调
    /// </summary>
    public static void ClearAllCallbacks()
    {
        eventCallbacks.Clear();
        Debug.Log("[EventDispatcher] 已清空所有事件回调");
    }

    #endregion

    #region 发送事件

    /// <summary>
    /// 发送事件（加入队列）
    /// </summary>
    /// <param name="eventType">事件类型（枚举）</param>
    /// <param name="parameters">事件参数</param>
    public static void SendEvent(Enum eventType, params object[] parameters)
    {
        // 如果该事件类型还没有队列，创建一个
        if (!eventQueues.ContainsKey(eventType))
        {
            eventQueues[eventType] = new Queue<EventData>();
        }
        
        // 创建事件数据并加入队列
        EventData eventData = new EventData(eventType, parameters);
        eventQueues[eventType].Enqueue(eventData);
        
        Debug.Log($"[EventDispatcher] 事件入队: 类型={eventType}, 队列长度={eventQueues[eventType].Count}");
    }

    #endregion

    #region 广播事件

    /// <summary>
    /// 广播指定类型的所有事件（从队列中逐个取出并调用回调）
    /// </summary>
    /// <param name="eventType">事件类型（枚举）</param>
    public static void BroadcastEvents(Enum eventType)
    {
        // 检查是否有该类型的事件队列
        if (!eventQueues.ContainsKey(eventType) || eventQueues[eventType].Count == 0)
        {
            return;
        }
        
        // 检查是否有注册的回调
        if (!eventCallbacks.ContainsKey(eventType) || eventCallbacks[eventType].Count == 0)
        {
            Debug.LogWarning($"[EventDispatcher] 事件类型 {eventType} 没有注册的回调，清空队列");
            eventQueues[eventType].Clear();
            return;
        }
        
        // 获取队列和回调列表
        Queue<EventData> queue = eventQueues[eventType];
        List<Action<EventData>> callbacks = eventCallbacks[eventType];
        
        int eventCount = queue.Count;
        Debug.Log($"[EventDispatcher] 开始广播: 类型={eventType}, 事件数={eventCount}, 回调数={callbacks.Count}");
        
        // 逐个处理队列中的事件
        while (queue.Count > 0)
        {
            EventData eventData = queue.Dequeue();
            
            // 调用所有注册的回调
            for (int i = 0; i < callbacks.Count; i++)
            {
                try
                {
                    callbacks[i]?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventDispatcher] 回调执行错误: 事件类型={eventType}, 错误={e.Message}");
                }
            }
        }
        
        Debug.Log($"[EventDispatcher] 广播完成: 类型={eventType}, 已处理{eventCount}个事件");
    }

    /// <summary>
    /// 广播所有类型的事件
    /// </summary>
    public static void BroadcastAllEvents()
    {
        if (eventQueues.Count == 0) return;
        _eventTypesToProcess.Clear();
        _eventTypesToProcess.AddRange(eventQueues.Keys);
        foreach (Enum eventType in _eventTypesToProcess)
        {
            BroadcastEvents(eventType);
        }
    }

    /// <summary>
    /// 立即发送并广播事件（不经过队列）
    /// </summary>
    /// <param name="eventType">事件类型（枚举）</param>
    /// <param name="parameters">事件参数</param>
    public static void SendEventImmediate(Enum eventType, params object[] parameters)
    {
        // 检查是否有注册的回调
        if (!eventCallbacks.ContainsKey(eventType) || eventCallbacks[eventType].Count == 0)
        {
            Debug.LogWarning($"[EventDispatcher] 事件类型 {eventType} 没有注册的回调");
            return;
        }
        
        // 创建事件数据
        EventData eventData = new EventData(eventType, parameters);
        List<Action<EventData>> callbacks = eventCallbacks[eventType];
        
        Debug.Log($"[EventDispatcher] 立即广播: 类型={eventType}, 回调数={callbacks.Count}");
        
        // 立即调用所有回调
        for (int i = 0; i < callbacks.Count; i++)
        {
            try
            {
                callbacks[i]?.Invoke(eventData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventDispatcher] 回调执行错误: 事件类型={eventType}, 错误={e.Message}");
            }
        }
    }

    #endregion

    #region 查询方法

    /// <summary>
    /// 获取指定事件类型的队列长度
    /// </summary>
    public static int GetQueueLength(Enum eventType)
    {
        if (eventQueues.ContainsKey(eventType))
        {
            return eventQueues[eventType].Count;
        }
        return 0;
    }

    /// <summary>
    /// 获取指定事件类型的回调数量
    /// </summary>
    public static int GetCallbackCount(Enum eventType)
    {
        if (eventCallbacks.ContainsKey(eventType))
        {
            return eventCallbacks[eventType].Count;
        }
        return 0;
    }

    /// <summary>
    /// 清空指定事件类型的队列
    /// </summary>
    public static void ClearQueue(Enum eventType)
    {
        if (eventQueues.ContainsKey(eventType))
        {
            int count = eventQueues[eventType].Count;
            eventQueues[eventType].Clear();
            Debug.Log($"[EventDispatcher] 清空队列: 事件类型={eventType}, 清空了{count}个事件");
        }
    }

    /// <summary>
    /// 清空所有事件队列
    /// </summary>
    public static void ClearAllQueues()
    {
        eventQueues.Clear();
        Debug.Log("[EventDispatcher] 已清空所有事件队列");
    }

    /// <summary>
    /// 打印所有事件信息（调试用）
    /// </summary>
    public static void PrintEventInfo()
    {
        Debug.Log("========== 事件转发器状态 ==========");
        Debug.Log($"事件类型总数: {eventQueues.Count}");
        
        foreach (var kvp in eventQueues)
        {
            Enum eventType = kvp.Key;
            int queueLength = kvp.Value.Count;
            int callbackCount = GetCallbackCount(eventType);
            
            Debug.Log($"事件类型: {eventType} | 队列长度: {queueLength} | 回调数: {callbackCount}");
        }
        
        Debug.Log("====================================");
    }

    #endregion
}

/// <summary>
/// 队列消费驱动，每帧 LateUpdate 广播队列中的事件。游戏启动时由 RuntimeInitializeOnLoadMethod 创建，DontDestroyOnLoad，无需手动挂载。
/// </summary>
internal class EventDispatcherDriver : MonoBehaviour
{
    private void LateUpdate()
    {
        EventDispatcher.BroadcastAllEvents();
    }
}