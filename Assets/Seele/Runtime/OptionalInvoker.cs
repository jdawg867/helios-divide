#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Utility for optional reflection calls.
/// - Editor: target type/method may not exist → returns default without throwing.
/// - Runtime (preview env): calls the method (supports sync, Task, Task&lt;T&gt;).
/// Usage:
///   var adResult = await OptionalInvoker.InvokeIfExistsAsync&lt;bool&gt;("SeeleAdManager", "ShowAd");
///   if (adResult == true) { /* ad played */ } else { /* missing or failed */ }
/// </summary>
public static class OptionalInvoker
{
    /// <summary>
    /// Invoke a method if it exists. Returns default(T) when type/method missing.
    /// </summary>
    public static Task<object?> InvokeIfExistsAsync(string typeName, string methodName, params object[] args)
        => InvokeIfExistsAsync<object?>(typeName, methodName, args);

    /// <summary>
    /// Invoke a method if it exists. Supports sync return, Task, Task&lt;T&gt;.
    /// </summary>
    public static async Task<T?> InvokeIfExistsAsync<T>(string typeName, string methodName, params object[] args)
    {
        Debug.Log($"[OptionalInvoker] InvokeIfExistsAsync<{typeof(T).Name}> called: typeName={typeName}, methodName={methodName}");
        try
        {
            var type = ResolveType(typeName);
            if (type == null)
            {
                Debug.LogWarning($"[OptionalInvoker] Type not found: {typeName}");
                return default;
            }
            Debug.Log($"[OptionalInvoker] Type found: {type.FullName}");

            var method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (method == null)
            {
                Debug.LogWarning($"[OptionalInvoker] Method not found: {typeName}.{methodName}");
                return default;
            }
            Debug.Log($"[OptionalInvoker] Method found: {method.Name}, ReturnType={method.ReturnType.FullName}, IsStatic={method.IsStatic}");

            object? target = null;
            if (!method.IsStatic)
            {
                target = ResolveInstance(type);
                if (target == null)
                {
                    Debug.LogWarning($"[OptionalInvoker] Instance not found for type: {typeName}");
                    return default;
                }
                Debug.Log($"[OptionalInvoker] Instance resolved: {target.GetType().FullName}");
            }

            Debug.Log($"[OptionalInvoker] Invoking method...");
            var result = method.Invoke(target, args ?? Array.Empty<object>());
            Debug.Log($"[OptionalInvoker] Method invoked, result type: {result?.GetType().FullName ?? "null"}");
            
            var unwrappedResult = await UnwrapResultAsync<T>(result);
            Debug.Log($"[OptionalInvoker] Unwrapped result: {unwrappedResult}, type: {unwrappedResult?.GetType().FullName ?? "null"}, is default: {unwrappedResult == null}");
            return unwrappedResult;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OptionalInvoker] Exception: {ex.Message}\n{ex.StackTrace}");
            return default;
        }
    }

    /// <summary>
    /// Invoke a method with callback pattern (void return with Action&lt;TResult&gt; callback as last param).
    /// If type/method missing, invokes callback with default(TResult) immediately.
    /// Usage:
    ///   OptionalInvoker.InvokeIfExistsWithCallback&lt;bool&gt;("SeeleAdManager", "ShowAd", (success) => { /* handle result */ });
    /// </summary>
    public static void InvokeIfExistsWithCallback<TResult>(
        string typeName,
        string methodName,
        Action<TResult> callback,
        params object[] args)
    {
        Debug.Log($"[OptionalInvoker] InvokeIfExistsWithCallback<{typeof(TResult).Name}> called: typeName={typeName}, methodName={methodName}");
        
        if (callback == null)
        {
            Debug.LogWarning($"[OptionalInvoker] Callback is null, ignoring call");
            return;
        }

        try
        {
            var type = ResolveType(typeName);
            if (type == null)
            {
                Debug.LogWarning($"[OptionalInvoker] Type not found: {typeName}, invoking callback with default value");
                callback(default(TResult)!);
                return;
            }
            Debug.Log($"[OptionalInvoker] Type found: {type.FullName}");

            // Find method matching the signature: void MethodName(..., Action<TResult>)
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == methodName && m.ReturnType == typeof(void))
                .ToList();

            if (methods.Count == 0)
            {
                Debug.LogWarning($"[OptionalInvoker] Method not found: {typeName}.{methodName} (void return), invoking callback with default value");
                callback(default(TResult)!);
                return;
            }

            // Find method with last parameter matching Action<TResult>
            MethodInfo? targetMethod = null;
            var expectedCallbackType = typeof(Action<TResult>);

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length > 0)
                {
                    var lastParam = parameters[parameters.Length - 1];
                    var lastParamType = lastParam.ParameterType;

                    // Check if last parameter is Action<TResult>
                    if (lastParamType == expectedCallbackType)
                    {
                        // Verify parameter count matches (args.Length + 1 for callback)
                        if (parameters.Length == (args?.Length ?? 0) + 1)
                        {
                            targetMethod = method;
                            break;
                        }
                    }
                }
            }

            if (targetMethod == null)
            {
                Debug.LogWarning($"[OptionalInvoker] No matching method signature found for {typeName}.{methodName}(..., Action<{typeof(TResult).Name}>), invoking callback with default value");
                callback(default(TResult)!);
                return;
            }

            Debug.Log($"[OptionalInvoker] Method found: {targetMethod.Name}, ReturnType={targetMethod.ReturnType.FullName}, IsStatic={targetMethod.IsStatic}");

            object? target = null;
            if (!targetMethod.IsStatic)
            {
                target = ResolveInstance(type);
                if (target == null)
                {
                    Debug.LogWarning($"[OptionalInvoker] Instance not found for type: {typeName}, invoking callback with default value");
                    callback(default(TResult)!);
                    return;
                }
                Debug.Log($"[OptionalInvoker] Instance resolved: {target.GetType().FullName}");
            }

            // Callback should directly match Action<TResult>
            // Since we already verified the signature matches, we can use callback directly
            object callbackWrapper = callback;

            // Construct parameters: original args + callback
            var methodParams = targetMethod.GetParameters();
            var invokeArgs = new object[methodParams.Length];
            
            // Copy original args
            var originalArgs = args ?? Array.Empty<object>();
            for (int i = 0; i < originalArgs.Length; i++)
            {
                invokeArgs[i] = originalArgs[i];
            }
            
            // Set callback as last parameter
            invokeArgs[invokeArgs.Length - 1] = callbackWrapper;

            Debug.Log($"[OptionalInvoker] Invoking method with {invokeArgs.Length} parameters...");
            targetMethod.Invoke(target, invokeArgs);
            Debug.Log($"[OptionalInvoker] Method invoked successfully, callback will be called by target method");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OptionalInvoker] Exception: {ex.Message}\n{ex.StackTrace}");
            callback(default(TResult)!);
        }
    }

    private static async Task<T?> UnwrapResultAsync<T>(object? result)
    {
        Debug.Log($"[OptionalInvoker] UnwrapResultAsync<{typeof(T).Name}> called, result type: {result?.GetType().FullName ?? "null"}");
        
        switch (result)
        {
            case null:
                Debug.Log($"[OptionalInvoker] Result is null, returning default");
                return default;
            case T typed:
                Debug.Log($"[OptionalInvoker] Result is already type {typeof(T).Name}, value: {typed}");
                return typed;
            case Task task:
                Debug.Log($"[OptionalInvoker] Result is Task, type: {task.GetType().FullName}, IsCompleted: {task.IsCompleted}");
#if UNITY_WEBGL && !UNITY_EDITOR
                await task;
#else
                await task.ConfigureAwait(false);
#endif
                Debug.Log($"[OptionalInvoker] Task awaited, IsCompleted: {task.IsCompleted}, Status: {task.Status}");
                var taskType = task.GetType();
                if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    Debug.Log($"[OptionalInvoker] Task is Task<>, generic args: {string.Join(", ", taskType.GetGenericArguments().Select(t => t.FullName))}");
                    var resProp = taskType.GetProperty("Result");
                    if (resProp != null)
                    {
                        var taskValue = resProp.GetValue(task);
                        Debug.Log($"[OptionalInvoker] Task.Result retrieved: {taskValue}, type: {taskValue?.GetType().FullName ?? "null"}");
                        var converted = ConvertResult<T>(taskValue);
                        Debug.Log($"[OptionalInvoker] Converted result: {converted}, type: {converted?.GetType().FullName ?? "null"}");
                        return converted;
                    }
                    Debug.LogWarning($"[OptionalInvoker] Task<> but Result property not found");
                }
                else
                {
                    Debug.Log($"[OptionalInvoker] Task is not Task<>, returning default");
                }
                return default;
            default:
                Debug.Log($"[OptionalInvoker] Result is other type, attempting conversion");
                return ConvertResult<T>(result);
        }
    }

    private static T? ConvertResult<T>(object? value)
    {
        Debug.Log($"[OptionalInvoker] ConvertResult<{typeof(T).Name}> called, value: {value}, type: {value?.GetType().FullName ?? "null"}");
        if (value is null)
        {
            Debug.Log($"[OptionalInvoker] Value is null, returning default");
            return default;
        }
        try
        {
            if (value is T cast)
            {
                Debug.Log($"[OptionalInvoker] Direct cast successful: {cast}");
                return cast;
            }
            Debug.Log($"[OptionalInvoker] Attempting ChangeType from {value.GetType().FullName} to {typeof(T).FullName}");
            var converted = (T?)Convert.ChangeType(value, typeof(T));
            Debug.Log($"[OptionalInvoker] ChangeType successful: {converted}");
            return converted;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OptionalInvoker] ConvertResult failed: {ex.Message}");
            return default;
        }
    }

    private static Type? ResolveType(string typeName)
    {
        // Full name first
        var type = Type.GetType(typeName);
        if (type != null) return type;

        // Search loaded assemblies by short or full name
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in SafeGetTypes(asm))
            {
                if (string.Equals(t.FullName, typeName, StringComparison.Ordinal) ||
                    string.Equals(t.Name, typeName, StringComparison.Ordinal))
                {
                    return t;
                }
            }
        }

        return null;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch { return Array.Empty<Type>(); }
    }

    private static object? ResolveInstance(Type type)
    {
        // Prefer common singleton pattern: public static <Type> Instance { get; }
        var instanceProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceProp != null && instanceProp.PropertyType == type)
        {
            return instanceProp.GetValue(null, null);
        }

        // Fallback: try parameterless ctor
        var ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor != null)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }
}

