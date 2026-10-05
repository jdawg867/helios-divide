using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 提供一个全局的、静态的对象注册表，用于命令系统。
/// </summary>
public static class CommandRegistry
{
    private static readonly Dictionary<Type, List<object>> ObjectRegistry = new Dictionary<Type, List<object>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        lock (ObjectRegistry)
        {
            ObjectRegistry.Clear();
        }
    }

    private static bool IsNull(object x)
    {
        if (x is UnityEngine.Object u)
        {
            return !u;
        }

        return x is null;
    }

    /// <summary>将对象添加到注册表。</summary>
    /// <param name="obj">要添加到注册表的对象。</param>
    /// <typeparam name="T">要添加到注册表的对象类型。</typeparam>
    public static void RegisterObject<T>(T obj) where T : class
    {
        RegisterObject(typeof(T), obj);
    }

    /// <summary>将对象添加到注册表。</summary>
    /// <param name="type">要添加到注册表的对象类型。</param>
    /// <param name="obj">要添加到注册表的对象。</param>
    public static void RegisterObject(Type type, object obj)
    {
        if (!type.IsClass)
        {
            throw new Exception("Registry may only contain class types");
        }

        lock (ObjectRegistry)
        {
            if (ObjectRegistry.ContainsKey(type))
            {
                if (ObjectRegistry[type].Contains(obj))
                {
                    throw new ArgumentException($"Could not register object '{obj}' of type {type.GetDisplayName()} as it was already registered.");
                }

                ObjectRegistry[type].Add(obj);
            }
            else
            {
                ObjectRegistry.Add(type, new List<object>() { obj });
            }
        }
    }

    /// <summary>从注册表中移除对象。</summary>
    /// <param name="obj">要从注册表中移除的对象。</param>
    /// <typeparam name="T">要从注册表中移除的对象类型。</typeparam>
    public static void DeregisterObject<T>(T obj) where T : class
    {
        DeregisterObject(typeof(T), obj);
    }

    /// <summary>从注册表中移除对象。</summary>
    /// <param name="type">要从注册表中移除的对象类型。</param>
    /// <param name="obj">要从注册表中移除的对象。</param>
    public static void DeregisterObject(Type type, object obj)
    {
        if (!type.IsClass)
        {
            throw new Exception("Registry may only contain class types");
        }

        lock (ObjectRegistry)
        {
            if (ObjectRegistry.ContainsKey(type) && ObjectRegistry[type].Contains(obj))
            {
                ObjectRegistry[type].Remove(obj);
            }
            else
            {
                throw new ArgumentException($"Could not deregister object '{obj}' of type {type.GetDisplayName()} as it was not found in the registry.");
            }
        }
    }

    /// <summary>获取指定注册表的大小。</summary>
    /// <returns>注册表大小。</returns>
    /// <typeparam name="T">要查询的注册表。</typeparam>
    public static int GetRegistrySize<T>() where T : class
    {
        return GetRegistrySize(typeof(T));
    }

    /// <summary>获取指定注册表的大小。</summary>
    /// <returns>注册表大小。</returns>
    /// <param name="type">要查询的注册表。</param>
    public static int GetRegistrySize(Type type)
    {
        return GetRegistryContents(type).Count();
    }

    /// <summary>获取指定注册表的内容。</summary>
    /// <returns>注册表内容。</returns>
    /// <typeparam name="T">要查询的注册表。</typeparam>
    public static IEnumerable<T> GetRegistryContents<T>() where T : class
    {
        foreach (object obj in GetRegistryContents(typeof(T)))
        {
            yield return (T)obj;
        }
    }

    /// <summary>获取指定注册表的内容。</summary>
    /// <returns>注册表内容。</returns>
    /// <param name="type">要查询的注册表。</param>
    public static IEnumerable<object> GetRegistryContents(Type type)
    {
        if (!type.IsClass)
        {
            throw new Exception("Registry may only contain class types");
        }

        lock (ObjectRegistry)
        {
            if (ObjectRegistry.TryGetValue(type, out var registry))
            {
                registry.RemoveAll(IsNull);
                return registry;
            }

            return Enumerable.Empty<object>();
        }
    }

    /// <summary>清空指定注册表的内容。</summary>
    /// <typeparam name="T">要清空的注册表。</typeparam>
    public static void ClearRegistryContents<T>() where T : class
    {
        ClearRegistryContents(typeof(T));
    }

    /// <summary>清空指定注册表的内容。</summary>
    /// <param name="type">要清空的注册表。</param>
    public static void ClearRegistryContents(Type type)
    {
        if (!type.IsClass)
        {
            throw new Exception("Registry may only contain class types");
        }

        lock (ObjectRegistry)
        {
            if (ObjectRegistry.TryGetValue(type, out var value))
            {
                value.Clear();
            }
        }
    }
}