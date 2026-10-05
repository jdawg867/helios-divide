/// <summary>
/// 非静态 MonoBehaviour 命令的目标实例查找类型
/// Registry还支持普通Class
/// </summary>
public enum MonoTargetType
{
    /// <summary>
    /// 查找场景中第一个Active的 MonoBehaviour 实例
    /// </summary>
    Single = 0,

    /// <summary>
    /// 查找场景中所有Active的 MonoBehaviour 实例
    /// </summary>
    All = 1,
    
    /// <summary>
    /// 查找场景中第一个Active的或Inactive的 MonoBehaviour 实例
    /// </summary>
    SingleInactive = 2,

    /// <summary>
    /// 查找场景中所有Active的或Inactive的 MonoBehaviour 实例
    /// </summary>
    AllInactive = 3,
    
    /// <summary>
    /// 查找在 CommandRegistry 中注册的所有实例。实例可以通过 <c>CommandRegistry.RegisterObject</c> 添加。
    /// 唯一支持非静态以及非 MonoBehaviour 类中指令的目标类型。
    /// </summary>
    Registry = 4,

    /// <summary>
    /// 如果实例尚不存在，则自动创建一个实例并将其添加到注册表中，并DontDestroyOnLoad，所有后续函数调用都将使用该实例。
    /// </summary>
    Singleton = 5,
}