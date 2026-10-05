using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

public partial class InputManager : MonoBehaviour
{
    private static InputManager _instance;
    public static InputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<InputManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("InputManager");
                    _instance = go.AddComponent<InputManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    // --- 内部存储变量 ---
    private Dictionary<KeyCode, bool> simKeys = new Dictionary<KeyCode, bool>();
    private HashSet<KeyCode> simKeysDown = new HashSet<KeyCode>();
    private HashSet<KeyCode> simKeysUp = new HashSet<KeyCode>();

    private Dictionary<string, bool> simButtons = new Dictionary<string, bool>();
    private HashSet<string> simButtonsDown = new HashSet<string>();
    private HashSet<string> simButtonsUp = new HashSet<string>();

    private Dictionary<int, bool> simMouseButtons = new Dictionary<int, bool>();
    private HashSet<int> simMouseButtonsDown = new HashSet<int>();
    private HashSet<int> simMouseButtonsUp = new HashSet<int>();

    private Dictionary<string, float> simAxes = new Dictionary<string, float>();
    private Dictionary<string, float> simAxesRaw = new Dictionary<string, float>();

    private List<Touch> simTouches = new List<Touch>();
    private Vector3 simMousePos;
    private Vector2 simMouseScroll;
    private Vector3 simAcc;
    private Quaternion simGyro = Quaternion.identity;
    private string simCompString;

    private ConcurrentQueue<System.Action> _commandQueue = new ConcurrentQueue<System.Action>();
    private int simulationStack = 0;
    private bool useSimulation => simulationStack > 0;

    #region Unity 生命周期

    void Update() {
        ClearTransientStates();

        while (_commandQueue.TryDequeue(out var cmd)) {
            cmd.Invoke();
        }

        if (!useSimulation) return;

    }

    private void ClearTransientStates()
    {
        simKeysDown.Clear();
        simKeysUp.Clear();
        simButtonsDown.Clear();
        simButtonsUp.Clear();
        simMouseButtonsDown.Clear();
        simMouseButtonsUp.Clear();
    }

    private void DecrementSimulationStack()
    {
        simulationStack--;
        if (simulationStack < 0)
        {
            simulationStack = 0;
        }
    }

    #endregion
    #region --- 异步 Simulate 接口 (持续 N 帧) ---

    private async Task RunSimulationTask(System.Action start, System.Action end, int frameCount)
    {
        // 确保至少持续 1 帧
        int duration = Mathf.Max(1, frameCount);

        // --- 第 1 帧：触发开始逻辑 (例如 Down) ---
        _commandQueue.Enqueue(() => {
            simulationStack++;
            start?.Invoke();
        });
        
        // 等待第 1 帧的 Update 执行完成
        await Task.Yield();

        // --- 第 2 到第 N 帧：维持状态 ---
        // 如果 duration 是 1，则不进入循环，直接去触发 end
        for (int i = 0; i < duration - 1; i++)
        {
            await Task.Yield();
        }

        // --- 第 N+1 帧：触发结束逻辑 (例如 Up) ---
        _commandQueue.Enqueue(() => {
            end?.Invoke();
            DecrementSimulationStack();
        });

        // 确保最后一帧的 Up 状态能被业务层捕获
        await Task.Yield();
    }

    // --- 键盘模拟 (持续 frameCount 帧) ---
    public async Task SimulateKey(KeyCode k, int frameCount)
    {
        // 这里的逻辑手动展开以确保 Down/Up 严丝合缝
        _commandQueue.Enqueue(() => {
            simulationStack++;
            simKeys[k] = true;
            simKeysDown.Add(k);
        });
        await Task.Yield();

        for (int i = 0; i < frameCount - 1; i++) await Task.Yield();

        _commandQueue.Enqueue(() => {
            simKeys[k] = false;
            simKeysUp.Add(k);
            simKeysDown.Remove(k);
        });
        await Task.Yield();
        _commandQueue.Enqueue(() => {
            DecrementSimulationStack();
        });
        await Task.Yield();


    }

    // 注意：SimulateKeyDown/Up 在这种模式下通常也代表按键的完整生命周期
    public async Task SimulateKeyDown(KeyCode k, int frameCount)
    {
        await RunSimulationTask(() => { simKeysDown.Add(k); simKeys[k] = true; }, 
                                () => { simKeys[k] = false; simKeysDown.Remove(k);}, frameCount);
    }

    public async Task SimulateKeyUp(KeyCode k, int frameCount)
    {
        // Up 事件通常只持续一帧，但按住状态可以持续 frameCount
        await RunSimulationTask(() => { simKeysUp.Add(k); simKeys[k] = false; }, 
                                () => { simKeysUp.Remove(k);}, frameCount);
    }

    // --- 虚拟按钮模拟 ---
    public async Task SimulateButton(string b, int frameCount)
    {
        _commandQueue.Enqueue(() => {
            simulationStack++;
            simButtons[b] = true;
            simButtonsDown.Add(b);
        });
        await Task.Yield();
        for (int i = 0; i < frameCount - 1; i++) await Task.Yield();
        _commandQueue.Enqueue(() => {
            simButtons[b] = false;
            simButtonsUp.Add(b);
            simButtonsDown.Remove(b);
        });
        await Task.Yield();
        _commandQueue.Enqueue(() => {
            DecrementSimulationStack();
        });
        await Task.Yield();
    }

    public async Task SimulateButtonDown(string b, int frameCount)
    {
        await RunSimulationTask(() => { simButtonsDown.Add(b); simButtons[b] = true; }, 
                                () => { simButtons[b] = false; simButtonsDown.Remove(b);}, frameCount);
    }

    public async Task SimulateButtonUp(string b, int frameCount) 
    {
        await RunSimulationTask(() => { simButtonsUp.Add(b); simButtons[b] = false; }, 
                                () => { simButtonsUp.Remove(b);}, frameCount);
    }

    // --- 鼠标模拟 ---
    public async Task SimulateMouseButton(int b, int frameCount)
    { 
        _commandQueue.Enqueue(() => {
            simulationStack++;
            simMouseButtons[b] = true;
            simMouseButtonsDown.Add(b);
        });
        await Task.Yield();
        for (int i = 0; i < frameCount - 1; i++) await Task.Yield();
        _commandQueue.Enqueue(() => {
            simMouseButtons[b] = false;
            simMouseButtonsUp.Add(b);
            simMouseButtonsDown.Remove(b);
        });
        await Task.Yield();
        _commandQueue.Enqueue(() => {
            DecrementSimulationStack();
        });
        await Task.Yield();
    }

    public async Task SimulateMouseButtonDown(int b, int frameCount)
    {
        await RunSimulationTask(() => { simMouseButtonsDown.Add(b); simMouseButtons[b] = true; }, 
                                () => { simMouseButtons[b] = false; simMouseButtonsDown.Remove(b);}, frameCount);
    }

    public async Task SimulateMouseButtonUp(int b, int frameCount)
    {
        await RunSimulationTask(() => { simMouseButtonsUp.Add(b); simMouseButtons[b] = false; }, 
                                () => { simMouseButtonsUp.Remove(b);}, frameCount);
    }

    // --- 坐标与数值模拟 ---
    // 这种模拟通常指该数值在这 frameCount 帧内有效
    public async Task SimulateMousePosition(Vector3 p, int frameCount)
    {
        await RunSimulationTask(() => simMousePos = p, () => {simMousePos = Vector3.zero;}, frameCount);
    }

    public async Task SimulateMouseScrollDelta(Vector2 d, int frameCount)
    {
        await RunSimulationTask(() => simMouseScroll = d, () => {simMouseScroll = Vector2.zero;}, frameCount);
    }

    public async Task SimulateAxis(string n, float v, int frameCount) 
    {
        await RunSimulationTask(() => simAxes[n] = v, () => simAxes[n] = 0f, frameCount);
    }

    public async Task SimulateAxisRaw(string n, float v, int frameCount)
    {
        await RunSimulationTask(() => simAxesRaw[n] = v, () => simAxesRaw[n] = 0f, frameCount);
    }

    public async Task SimulateAcceleration(Vector3 a, int frameCount)
    { 
        await RunSimulationTask(() => simAcc = a, () => simAcc = Vector3.zero, frameCount);
    }

    public async Task SimulateGyro(Quaternion g, int frameCount) 
    {
        await RunSimulationTask(() => simGyro = g, () => simGyro = Quaternion.identity, frameCount);
    }

    public async Task SimulateTouch(List<Touch> t, int frameCount)
    {
        await RunSimulationTask(() => simTouches = t, () => simTouches.Clear(), frameCount);
    }

    #endregion

    #region --- Get 接口 (保持不变) ---
    public static bool GetKey(KeyCode k) => Instance.useSimulation ? (Instance.simKeys.TryGetValue(k, out bool v) && v) : Input.GetKey(k);
    public static bool GetKeyDown(KeyCode k) => Instance.useSimulation ? Instance.simKeysDown.Contains(k) : Input.GetKeyDown(k);
    public static bool GetKeyUp(KeyCode k) => Instance.useSimulation ? Instance.simKeysUp.Contains(k) : Input.GetKeyUp(k);
    
    public static bool GetButton(string b) => Instance.useSimulation ? (Instance.simButtons.TryGetValue(b, out bool v) && v) : Input.GetButton(b);
    public static bool GetButtonDown(string b) => Instance.useSimulation ? Instance.simButtonsDown.Contains(b) : Input.GetButtonDown(b);
    public static bool GetButtonUp(string b) => Instance.useSimulation ? Instance.simButtonsUp.Contains(b) : Input.GetButtonUp(b);
    
    public static bool GetMouseButton(int b) => Instance.useSimulation ? (Instance.simMouseButtons.TryGetValue(b, out bool v) && v) : Input.GetMouseButton(b);
    public static bool GetMouseButtonDown(int b) => Instance.useSimulation ? Instance.simMouseButtonsDown.Contains(b) : Input.GetMouseButtonDown(b);
    public static bool GetMouseButtonUp(int b) => Instance.useSimulation ? Instance.simMouseButtonsUp.Contains(b) : Input.GetMouseButtonUp(b);

    public static Vector3 mousePosition => Instance.useSimulation ? Instance.simMousePos : Input.mousePosition;
    public static Vector2 mouseScrollDelta => Instance.useSimulation ? Instance.simMouseScroll : Input.mouseScrollDelta;

    public static float GetAxis(string n) => Instance.useSimulation ? (Instance.simAxes.ContainsKey(n) ? Instance.simAxes[n] : 0f) : Input.GetAxis(n);
    public static float GetAxisRaw(string n) => Instance.useSimulation ? (Instance.simAxesRaw.ContainsKey(n) ? Instance.simAxesRaw[n] : 0f) : Input.GetAxisRaw(n);
    
    public static Vector3 acceleration => Instance.useSimulation ? Instance.simAcc : Input.acceleration;
    public static Quaternion gyroAttitude => Instance.useSimulation ? Instance.simGyro : Input.gyro.attitude;
    public static int touchCount => Instance.useSimulation ? Instance.simTouches.Count : Input.touchCount;
    public static Touch GetTouch(int i) => Instance.useSimulation ? Instance.simTouches[i] : Input.GetTouch(i);
    
    public static bool anyKey => Instance.useSimulation ? (Instance.simKeys.Values.Any(v => v) || Instance.simMouseButtons.Values.Any(v => v)) : Input.anyKey;
    public static bool anyKeyDown => Instance.useSimulation ? (Instance.simKeysDown.Count > 0 || Instance.simMouseButtonsDown.Count > 0) : Input.anyKeyDown;
    public static string compositionString => Instance.useSimulation ? Instance.simCompString : Input.compositionString;
    #endregion
}