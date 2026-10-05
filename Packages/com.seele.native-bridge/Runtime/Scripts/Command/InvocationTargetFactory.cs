using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class InvocationTargetFactory
{
    private static readonly Dictionary<(MonoTargetType, Type), object> TargetCache = new Dictionary<(MonoTargetType, Type), object>();

    public static IEnumerable<T> FindTargets<T>(MonoTargetType method) where T : MonoBehaviour
    {
        foreach (object target in FindTargets(typeof(T), method))
        {
            yield return target as T;
        }
    }
    
    /// <summary>
    /// 根据类型和查找策略，动态查找场景中的 MonoBehaviour 实例 或 普通类型事例
    /// </summary>
    /// <param name="classType">类的Type。</param>
    /// <param name="method">查找策略。</param>
    /// <returns>找到的目标实例枚举。</returns>
    public static IEnumerable<object> FindTargets(Type classType, MonoTargetType method)
    {
        switch (method)
        {
            case MonoTargetType.Single:
            {
                // 查找第一个Active的实例
                
                UnityEngine.Object singleTarget = UnityEngine.Object.FindFirstObjectByType(classType);
                return singleTarget == null ? Enumerable.Empty<object>() : singleTarget.Yield();
            }
            case MonoTargetType.All:
            {
                // 查找所有Active的实例（自然排序，xx1，xx2）

                return UnityEngine.Object.FindObjectsByType(classType, FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .OrderBy(x => x.name, new AlphanumComparator());
            }
            case MonoTargetType.Registry:
            {
                // 注册类型
                
                return CommandRegistry.GetRegistryContents(classType);
            }
            case MonoTargetType.Singleton:
            {
                // 获取或实例化单例
                
                return GetSingletonInstance(classType).Yield();
            }
            case MonoTargetType.SingleInactive:
            {
                // 查找第一个Active的或Inactive的实例
                
                return WrapSingleCached(classType, method, type =>
                {
                    return Resources.FindObjectsOfTypeAll(type)
                        .FirstOrDefault(x => !x.hideFlags.HasFlag(HideFlags.HideInHierarchy));
                });
            }
            case MonoTargetType.AllInactive:
            {
                // 查找所有Active的或Inactive的实例（自然排序，xx1，xx2）
                
                return Resources.FindObjectsOfTypeAll(classType)
                    .Where(x => !x.hideFlags.HasFlag(HideFlags.HideInHierarchy))
                    .OrderBy(x => x.name, new AlphanumComparator());
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(method), $"Unsupported MonoTargetType: {method}");
        }
    }
    
    private static IEnumerable<object> WrapSingleCached(Type classType, MonoTargetType method, Func<Type, object> targetFinder)
    {
        if (!TargetCache.TryGetValue((method, classType), out object target) || target as UnityEngine.Object == null)
        {
            target = targetFinder(classType);
            TargetCache[(method, classType)] = target;
        }

        return target == null ? Enumerable.Empty<object>() : target.Yield();
    }

    private static object GetSingletonInstance(Type classType)
    {
        if (CommandRegistry.GetRegistrySize(classType) > 0)
        {
            return CommandRegistry.GetRegistryContents(classType).First();
        }

        if (classType.IsSubclassOf(typeof(MonoBehaviour)))
        {
            //  检查 classType 是否是 MonoBehaviour，只有是才创建
            
            object target = CreateCommandSingletonInstance(classType);
            var existingInstance = CommandRegistry.GetRegistryContents(classType).FirstOrDefault();
            if (existingInstance == null)
            {
                CommandRegistry.RegisterObject(classType, target);
            }
            return target;
        }

        // 对于非 MonoBehaviour，发出警告并返回 null
        Debug.LogWarning($"Cannot automatically create a Singleton instance for non-MonoBehaviour type '{classType.Name}'. Please register it manually using CommandRegistry.RegisterObject().");
        return null;
    }

    private static Component CreateCommandSingletonInstance(Type classType)
    {
        GameObject obj = new GameObject($"{classType} [Singleton]");
        UnityEngine.Object.DontDestroyOnLoad(obj);
        return obj.AddComponent(classType);
    }
}