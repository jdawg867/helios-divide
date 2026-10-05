#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 提供一个全局的、静态的引用数据库缓存静态类，用于命令系统。
/// </summary>
public static class CommandDatabase
{
    private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

    // 主数据存储，用唯一的 FullName作为键
    private static readonly Dictionary<string, CommandProviderInfo> CommandMap = new(StringComparer.OrdinalIgnoreCase);

    // 别名映射字典，将所有别名 (Name, FullName, Alias) 映射到唯一键
    private static readonly Dictionary<string, string> AliasToUniqueKeyMap = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> GetAllCommands()
    {
        return AliasToUniqueKeyMap.Keys;
    }

    public static CommandProviderInfo? GetCommand(string key)
    {
        // 通过别名查找唯一键
        if (AliasToUniqueKeyMap.TryGetValue(key, out var uniqueKey))
        {
            // 通过唯一键从主数据存储中获取命令信息
            if (CommandMap.TryGetValue(uniqueKey, out var commandInfo))
            {
                return commandInfo;
            }
        }

        return null;
    }

    public static Type[] GetCommandParameterTypes(string key)
    {
        CommandProviderInfo? commandProvider = GetCommand(key);
        if (commandProvider == null)
        {
            return Type.EmptyTypes;
        }

        try
        {
            return commandProvider.Member.GetParameterTypes();
        }
        catch (ArgumentException ex)
        {
            Debug.LogWarning($"Attempted to get parameter types for unsupported MemberType '{commandProvider.Member.MemberType}' for command '{key}'. Details: {ex.Message}");
            return Type.EmptyTypes;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Init()
    {
        CommandMap.Clear();
        AliasToUniqueKeyMap.Clear();

        Assembly commandAssembly = typeof(CommandDatabase).Assembly;

        var membersByAttribute = ReflectionExtensions.FindAllMembersByAttribute<CommandAttribute>(StaticFlags | InstanceFlags, commandAssembly);

        foreach (var (memberInfo, attribute) in membersByAttribute)
        {
            if (!memberInfo.IsStatic() && (memberInfo.DeclaringType == null || !memberInfo.DeclaringType.IsSubclassOf(typeof(MonoBehaviour))))
            {
                continue;
            }
            
            // 生成方法签名字符串
            string signature = "";
            if (memberInfo is MethodInfo methodInfo)
            {
                // 将方法的参数类型拼接成一个易读的签名字符串，例如 "(System.String, System.Int32)"
                signature = $"({string.Join(", ", methodInfo.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name))})";
            }

            // 生成所有键
            string nameKey = memberInfo.Name;
            string? declaringTypeName = memberInfo.DeclaringType?.FullName;
            string fullNameKey = string.IsNullOrWhiteSpace(declaringTypeName) ? nameKey : $"{declaringTypeName}.{memberInfo.Name}";
            string aliasKey = attribute.Alias;

            // 命令的唯一键，例如 "MyClass.MyMethod(String, Int32)" 
            string uniqueKey = $"{fullNameKey}{signature}";

            // 检查主数据存储中是否已存在该命令
            if (CommandMap.ContainsKey(uniqueKey))
            {
                Debug.LogWarning($"Command with unique key '{uniqueKey}' is already registered. Skipping new registration.");
                continue;
            }

            // 收集并检查所有别名是否已被占用
            var allKeys = new List<string> { nameKey, fullNameKey };
            // 只有当 Alias 不为空字符串时，才将其作为键
            if (!string.IsNullOrWhiteSpace(aliasKey))
            {
                allKeys.Add(aliasKey);
            }

            // 确保别名不重复
            allKeys = allKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            bool hasConflict = false;
            foreach (var key in allKeys)
            {
                if (AliasToUniqueKeyMap.TryGetValue(key, out var value))
                {
                    Debug.LogWarning($"Command key or alias '{key}' for command '{uniqueKey}' is already in use by another command ('{value}'). Skipping registration for this command.");
                    hasConflict = true;
                    break;
                }
            }

            if (hasConflict)
            {
                continue;
            }

            // 注册命令
            // 对于实例命令，Client 在 Init 阶段是 null。它需要在执行时动态查找。
            // CommandInvoker 在执行实例命令时需要负责找到目标对象。
            var commandProvider = new CommandProviderInfo(memberInfo, null, attribute.TargetType);

            // 添加到主数据存储
            CommandMap.Add(uniqueKey, commandProvider);

            // 将所有别名映射到唯一键
            foreach (var key in allKeys)
            {
                AliasToUniqueKeyMap.Add(key, uniqueKey);
            }
        }

        Debug.Log($"CommandDatabase initialized. Found {CommandMap.Count} command definitions with {AliasToUniqueKeyMap.Count} aliases.");
    }
}