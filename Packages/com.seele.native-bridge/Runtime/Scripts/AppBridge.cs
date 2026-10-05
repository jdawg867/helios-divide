using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Collections;

// 基础结果码枚举
public enum SeeleResultCode
{
    Success = 0,                    // 成功
    BizError = -1,                  // 业务调用失败
    MethodNotFound = -2,            // 方法不存在
    UnknownError = -3,              // 调用异常
    InitializationError = -4,       // 初始化错误
    NetworkError = -5,              // 网络错误
    ParseError = -6,                // 解析错误
    ResourceNotFound = -7,          // 资源未找到
    ResourceLoadError = -8,         // 资源加载错误
    InvalidParameter = -9,          // 无效参数
}

[Serializable]
public class SeeleMethodResult
{
    public int Code { get; set; }
    public string Message { get; set; }
    public JObject Data { get; set; }
    public object CallbackData { get; set; }

    public SeeleMethodResult(int code, JObject data = null, string message = null, object callbackdata = null)
    {
        Code = code;
        Data = data;
        Message = message ?? GetResultMessage(code);
        CallbackData = callbackdata;
    }
    
    public static readonly SeeleMethodResult Ok = Success();

    public static SeeleMethodResult Success(JObject data = null, string message = null, object callbackdata = null) => 
        new((int)SeeleResultCode.Success, data, message, callbackdata);

    public static SeeleMethodResult Error(SeeleResultCode code, string data, string message = null, object callbackdata = null)
    {
        Debug.LogError($"SeeleMethodResult Error code:{code} message:{message}");
        return new((int)code, new JObject() { ["Data"] = data }, message, callbackdata);
    }

    public bool IsSuccess => Code == (int)SeeleResultCode.Success;

    private static string GetResultMessage(int code)
    {
        return code switch
        {
            (int)SeeleResultCode.Success => "操作成功",
            (int)SeeleResultCode.BizError => "业务调用失败",
            (int)SeeleResultCode.MethodNotFound => "方法未找到",
            (int)SeeleResultCode.UnknownError => "调用异常",
            (int)SeeleResultCode.InitializationError => "初始化错误",
            (int)SeeleResultCode.NetworkError => "网络错误",
            (int)SeeleResultCode.ParseError => "解析错误",
            (int)SeeleResultCode.ResourceNotFound => "资源未找到",
            (int)SeeleResultCode.ResourceLoadError => "资源加载错误",
            (int)SeeleResultCode.InvalidParameter => "无效参数",
            _ => $"未知结果码: {code}"
        };
    }
}

[Serializable]
public class SeeleMethodMessage
{
    // 请求ID，用于匹配请求和响应
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    
    // 方法标识符
    public string Method { get; set; }
    
    // 方法参数（JObject类型）
    public JObject Parameters { get; set; }

    
    // 结果信息（仅用于响应消息）
    public SeeleMethodResult Result { get; set; }

    // 创建从Unity调用Native的方法消息
    public static SeeleMethodMessage CreateUnityToNativeCall(string methodName, JObject parameters = null)
    {
        var message = new SeeleMethodMessage
        {
            Method = methodName
        };

        if (parameters != null && parameters.Count > 0)
        {
            message.Parameters = parameters;
        }

        return message;
    }

    // 创建响应消息
    public static SeeleMethodMessage CreateResponse(string originalRequestId, string methodName, SeeleMethodResult result)
    {
        return new SeeleMethodMessage
        {
            RequestId = originalRequestId,
            Method = methodName,
            Result = result
        };
    }

    // 将消息转换为JSON字符串
    public string ToJson()
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        return JsonConvert.SerializeObject(this, settings);
    }

    // 从JSON字符串创建消息
    public static SeeleMethodMessage FromJson(string json)
    {
        return JsonConvert.DeserializeObject<SeeleMethodMessage>(json);
    }
    
}

// Unity端发送消息到Native的接口
public interface IUnityToNativeSender
{
    // Unity发送方法调用到Native
    void SendMethodCallToNative(SeeleMethodMessage message);
    // Unity发送响应给Native
    void SendResponseToNative(SeeleMethodMessage originalMessage, SeeleMethodResult result);
}

// Native端处理Unity调用的接口
public interface INativeToUnityHandler
{
    // 处理Native发送到Unity的方法调用
    UniTask<SeeleMethodResult> HandleMethodCallToUnity(SeeleMethodMessage message);
    // 处理Native发送到Unity的响应
    void HandleResponseToUnity(SeeleMethodMessage message);
}

public class AppBridge : MonoBehaviour, IUnityToNativeSender, INativeToUnityHandler
{
    public enum SeeleUnityEvent
    {
        UNITY_INITED, // Unity 初始化完成，可以执行加载
        GAME_LOAD_START, // 游戏开始加载
        GAME_LOAD_COMPLETED, // 游戏加载完成
        GAME_LOAD_FAILED, // 游戏加载失败
        GAME_AD_START, // 游戏广告开始
        GAME_LIFE_CYCLE, // 游戏生命周期
    }

    public Dictionary<SeeleUnityEvent, string> SeeleUnityEventMessages = new Dictionary<SeeleUnityEvent, string>()
    {
        [SeeleUnityEvent.UNITY_INITED] = "Seele unity initialized.",
        [SeeleUnityEvent.GAME_LOAD_START] = "Game loading started.",
        [SeeleUnityEvent.GAME_LOAD_COMPLETED] = "Game loading completed.",
        [SeeleUnityEvent.GAME_LOAD_FAILED] = "Game loading failed.",
        [SeeleUnityEvent.GAME_AD_START] = "Game advertising start.",
        [SeeleUnityEvent.GAME_LIFE_CYCLE] = "Game Life cycle event.",
    };
    
    private static AppBridge _instance;

    public static AppBridge Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("AppBridge");
                _instance = go.AddComponent<AppBridge>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    

    // 存储Unity调用Native的回调
    private readonly Dictionary<string, UniTaskCompletionSource<SeeleMethodResult>> _unityToNativeCallbacks = new();
    
    // 存储注册的处理器
    private INativeToUnityHandler _registeredHandler;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

// #if UNITY_WEBGL && !UNITY_EDITOR
//             if (GetComponent<WebGLMemoryWatchdog>() == null)
//             {
//                 gameObject.AddComponent<WebGLMemoryWatchdog>();
//             }
// #endif

            if (IsInSeeleWebOnline())
            {
                // AppBridgeCommands.HideDebugger();
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 注册处理器
    // public void RegisterHandler(INativeToUnityHandler handler)
    // {
    //     if (handler != null && handler != this)
    //     {
    //         _registeredHandler = handler;
    //     }
    // }

    // // 注销处理器
    // public void UnregisterHandler(INativeToUnityHandler handler)
    // {
    //     if (_registeredHandler == handler)
    //     {
    //         _registeredHandler = null;
    //     }
    // }

    // IUnityToNativeSender implementation
    public void SendMethodCallToNative(SeeleMethodMessage message)
    {
        var tcs = new UniTaskCompletionSource<SeeleMethodResult>();
        _unityToNativeCallbacks[message.RequestId] = tcs;
        SendToNative(message.ToJson());
    }
    
    public void SendUnityEventToNative(SeeleUnityEvent e)
    {
        SendUnityEventToNative(e, null);
    }
    
    public void SendUnityEventToNative(SeeleUnityEvent e, JObject data)
    {
        var parameters = new JObject()
        {
            ["event"] = e.ToString(),
            ["message"] = SeeleUnityEventMessages[e]
        };
        
        if (data != null)
        {
            parameters["data"] = data;
        }
        
        SendMethodCallToNative(new SeeleMethodMessage()
        {
            Method = "UnityEvent",
            Parameters = parameters,
            RequestId = Guid.NewGuid().ToString()
        });
    }
    
    public UniTask<SeeleMethodResult> SendMethodCallToNativeAsync(SeeleMethodMessage message)
    {
        var tcs = new UniTaskCompletionSource<SeeleMethodResult>();
        _unityToNativeCallbacks[message.RequestId] = tcs;
        SendToNative(message.ToJson());
        // _ = DelayThenActAsync(message);
        return tcs.Task;
    }
    
    // 延迟调用测试
    private async UniTaskVoid DelayThenActAsync(SeeleMethodMessage message)
    {
        await UniTask.Delay(3000);
        // Debug.Log($"RequestId {requestId} triggered after 3 seconds");

        // CallSomeCallback(requestId);
        var rspResultData = JObject.FromObject(new { foo = "bar" });
        SeeleMethodMessage rsp = SeeleMethodMessage.CreateResponse(message.RequestId, message.Method, new SeeleMethodResult(0, rspResultData));
        HandleResponseToUnity(rsp);
    }

    public void SendResponseToNative(SeeleMethodMessage originalMessage, SeeleMethodResult result)
    {
        var response = SeeleMethodMessage.CreateResponse(originalMessage.RequestId, originalMessage.Method, result);
        SendToNative(response.ToJson());
    }

    // INativeToUnityHandler implementation
    public async UniTask<SeeleMethodResult> HandleMethodCallToUnity(SeeleMethodMessage message)
    {
        try
        {
            var result = await CommandInvoker.InvokeCommand(message.Method, message.Parameters);
            // todo 注册的桥接方法要保证返回值为 SeeleMethodResult
            return result as SeeleMethodResult ?? SeeleMethodResult.Error(SeeleResultCode.UnknownError, "Invalid result type");
            // return SeeleMethodResult.Success();
        }
        catch (Exception e)
        {
            return SeeleMethodResult.Error(SeeleResultCode.UnknownError, e.Message);
        }
    }
    

    public void HandleResponseToUnity(SeeleMethodMessage message)
    {
        try
        {
            // 处理回调
            if (_unityToNativeCallbacks.TryGetValue(message.RequestId, out var tcs))
            {
                tcs.TrySetResult(message.Result);
                _unityToNativeCallbacks.Remove(message.RequestId);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling response from native: {e}");
        }
    }

    // 处理从Native接收到的消息
    public void ProcessMessageFromNative(string messageJson)
    {
        Debug.Log($"ProcessMessageFromNative: {messageJson}");
        try
        {
            var message = SeeleMethodMessage.FromJson(messageJson);
            if (message == null)
            {
                Debug.LogError($"Failed to parse message from native: {messageJson}");
                var errorMessage = SeeleMethodMessage.CreateResponse(
                    messageJson, 
                    "Unknown",
                    SeeleMethodResult.Error(SeeleResultCode.UnknownError, "Failed to parse message")
                );
                SendToNative(errorMessage.ToJson());
                return;
            }

            if (message.Result != null)
            {
                // 处理Native的响应
                HandleResponseToUnity(message);
            }
            else
            {
                // 处理Native的调用
                ProcessMethodCallAsync(message);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing message from native: {e}");
            var errorMessage = SeeleMethodMessage.CreateResponse(
                messageJson, 
                "Unknown",
                SeeleMethodResult.Error(SeeleResultCode.UnknownError, e.Message)
            );
            SendToNative(errorMessage.ToJson());
        }
    }

    // 异步处理方法调用
    private async void ProcessMethodCallAsync(SeeleMethodMessage message)
    {
        try
        {
            var result = await HandleMethodCallToUnity(message);
            SendResponseToNative(message, result);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing method call: {e}");
            SendResponseToNative(message, SeeleMethodResult.Error(SeeleResultCode.UnknownError, e.Message));
        }
    }


    
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void sendMethod(string data);
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void sendMethod(string data);
    [DllImport("__Internal")]
    private static extern bool IsInSeele();
    [DllImport("__Internal")]
    private static extern bool IsInSeeleOnline();
#endif

    // 发送消息到Native平台
    private void SendToNative(string messageJson)
    {
// #if UNITY_ANDROID && !UNITY_EDITOR
//         var kokoAndroidJava = new AndroidJavaObject($"com.seele.koko.unity.UnityJavaMethod");
//         kokoAndroidJava.CallStatic("sendMethod", messageJson);
// #elif UNITY_IOS && !UNITY_EDITOR
//         sendMethod(messageJson);
#if UNITY_WEBGL && !UNITY_EDITOR
        if (IsInSeeleWeb()){
            sendMethod(messageJson);
        }
#endif
        Debug.Log($"Sending to native: {messageJson}");
        
    }

    // 判断 WebGL 是否运行在 Seele 网页中，IsInSeele 定义在 seele.jslib 中
    public bool IsInSeeleWeb()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return IsInSeele();
#endif
        return false;
    }
    
    public bool IsInSeeleWebOnline()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return IsInSeeleOnline();
#endif
        return false;
    }
    
    public bool IsInStandAloneWeb()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return !IsInSeele();
#endif
        return false;
    }
    
}



