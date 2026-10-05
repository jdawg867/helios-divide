using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using UnityEngine;

// 必须确保命名空间和类名与 OptionalInvoker 调用的字符串一致
public static class SeeleLifeCycleManager
{
    public enum LifeCycleEvent
    {
        START,
        END,
        PAUSE,
        NEXT,
        REWARD
    }

    // 用于存储当前的 TaskCompletionSource，将异步转同步
    private static TaskCompletionSource<bool> _currentReportTask;
    private static bool _isStarted = false; // 防止重入标记

    /// <summary>
    /// 被反射调用的方法。
    /// </summary>
    public static async Task<bool> StartGame()
    {
        return await SendReportRequest(LifeCycleEvent.START);
    }

    /// <summary>
    /// 被反射调用的方法。
    /// </summary>
    public static async Task<bool> Start()
    {
        return await SendReportRequest(LifeCycleEvent.START);
    }

    /// <summary>
    /// 被反射调用的方法。
    /// </summary>
    public static async Task<bool> GameOver()
    {
        return await SendReportRequest(LifeCycleEvent.END);
    }

    /// <summary>
    /// 被反射调用的方法。
    /// </summary>
    public static async Task<bool> Pause()
    {
        return await SendReportRequest(LifeCycleEvent.PAUSE);
    }

    /// <summary>
    /// 被反射调用的方法。
    /// </summary>
    public static async Task<bool> Next()
    {
        return await SendReportRequest(LifeCycleEvent.NEXT);
    }

    /// <summary>
    /// 被反射调用的方法。
    /// </summary>
    public static async Task<bool> Reward()
    {
        return await SendReportRequest(LifeCycleEvent.REWARD);
    }

    private static async Task<bool> SendReportRequest(LifeCycleEvent lifeCycle)
    {
        Debug.Log($"[SeeleLifeCycleManager] Requesting Start");

        bool adResult = false;

        try
        {
            // 2. 初始化 TaskCompletionSource，准备等待前端结果
            _currentReportTask = new TaskCompletionSource<bool>();

            // 为了演示，这里假设 AppBridge 也是通过类似机制运作，或者我们直接调用 JS
            CallFrontendLifeCycle(lifeCycle);

            // 4. 等待结果 (这行代码会让程序"卡"在这里，直到 SetResult 被调用)
            adResult = await _currentReportTask.Task;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SeeleLifeCycleManager] Error: {e.Message}");
            adResult = false; // 出错视为失败
        }
        finally
        {
            _currentReportTask = null;
        }

        return adResult;
    }

    public static void SendReportRequest(Action<bool> callback, LifeCycleEvent lifeCycle)
    {
        if(callback == null)
        {
            Debug.LogWarning("[SeeleLifeCycleManager] callback null.");
            return;
        }

        if (_isStarted)
        {
            Debug.LogWarning("[SeeleLifeCycleManager] Ad is already start, ignoring request.");
            callback?.Invoke(false); // 直接返回失败或忽略
            return;
        }

        Debug.Log($"[SeeleLifeCycleManager] Requesting (Callback Mode)");

        // 1. 标记状态
        _isStarted = true;

        CallFrontendLifeCycle(lifeCycle);
    }

    // --- WebGL 交互部分 ---

    // 模拟发送给 JS
    private static void CallFrontendLifeCycle(LifeCycleEvent lifeCycle)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var data = new JObject()
        {
            ["lifyCycle"] = lifeCycle.ToString().ToLower()
        };

        AppBridge.Instance.SendUnityEventToNative(AppBridge.SeeleUnityEvent.GAME_LIFE_CYCLE, data);
#else
        // 编辑器模式下的模拟：延时 1 秒后模拟埋点上报
        Debug.Log("[SeeleLifeCycleManager] Editor Mode: Simulating Report...");
        SimulateEditorLifeCycleResult();
#endif
    }

    private static async void SimulateEditorLifeCycleResult()
    {
        await Task.Delay(2000); // 模拟广告时长
        // OnCallback(new JObject
        // {
        //     ["code"] = 0,
        //     ["message"] = "",
        //     ["data"] = new JObject
        //     {
        //         ["canvasId"] = "xxx",
        //     }
        // });   // 模拟前端回调
        var result = new JObject
        {
            ["Method"] = "OnGameLifeCycle",
            ["Parameters"] = new JObject
            {
                ["code"] = 0,
                ["message"] = "success",
                ["data"] = new JObject {}
                ["RequestId"] = "c11540df-a1f0-4ced-ad0e-11374a64691f"
            }

        };
        AppBridge.Instance.ProcessMessageFromNative(result.ToString());
    }

    /// <summary>
    /// 供前端 JS 调用的回调方法 (使用 SendMessage 或 jslib 回调)
    /// </summary>
    public static void OnCallback(JObject result)
    {
        Debug.Log($"[SeeleLifeCycleManager] Received Callback: {result}");

        var code = result["code"]?.ToObject<int>() ?? -1;
        var isSuccess = code == 0;

        if (_currentReportTask != null)
        {
            _currentReportTask.TrySetResult(isSuccess);
        }
    }

    /// <summary>
    /// 生命周期
    /// </summary>
    [Command("OnGameLifeCycle")]
    public static SeeleMethodResult OnGameLifeCycle(JObject parameters)
    {
        OnCallback(parameters);
        return SeeleMethodResult.Success();
    }
}