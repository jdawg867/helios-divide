#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class CommandInvoker
{
    private const string VoidTaskResultFullName = "System.Threading.Tasks.VoidTaskResult";
    private const string ResultFullName = "Result";

    private static readonly MethodInfo ConvertUniTaskResultToObjectMethodDef = typeof(CommandInvoker).GetMethod(nameof(ConvertUniTaskResultToObject), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ConvertAwaitableResultToObjectMethodDef = typeof(CommandInvoker).GetMethod(nameof(ConvertAwaitableResultToObject), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ConvertValueTaskResultToObjectMethodDef = typeof(CommandInvoker).GetMethod(nameof(ConvertValueTaskResultToObject), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// 调用指定命令
    /// </summary>
    /// <param name="commandName">命令名</param>
    /// <param name="arguments">参数数组（外部明确知道参数类型和参数数量）</param>
    /// <returns></returns>
    public static async UniTask<SeeleMethodResult> InvokeCommand(string commandName, params object[]? arguments)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            string message = $"Parsed command name is empty from input: '{commandName}'. Command cannot be invoked.";
            return SeeleMethodResult.Error(SeeleResultCode.MethodNotFound, null, message);
        }

        SeeleMethodResult result;
        try
        {
            // 从命令数据库获取命令信息
            var targetMemberProvider = CommandDatabase.GetCommand(commandName);
            if (targetMemberProvider == null)
            {
                string message = $"Command \"{commandName}\" not found.";
                return SeeleMethodResult.Error(SeeleResultCode.MethodNotFound, null, message);
            }

            // 执行命令调用
            result = await InvokeMember(targetMemberProvider, arguments);
        }
        catch (ArgumentException ex)
        {
            string message = $"Argument error processing command '{commandName}': {ex.Message}";
            result = SeeleMethodResult.Error(SeeleResultCode.UnknownError, null, message);
        }
        catch (InvalidCastException ex)
        {
            string message = $"Type conversion error for command '{commandName}': {ex.Message}";
            result = SeeleMethodResult.Error(SeeleResultCode.UnknownError, null, message);
        }
        catch (TargetInvocationException ex)
        {
            string message = $"Error during execution of command '{commandName}': {ex.InnerException?.Message ?? ex.Message}\n{ex.InnerException?.StackTrace ?? ex.StackTrace}";
            result = SeeleMethodResult.Error(SeeleResultCode.UnknownError, null, message);
        }
        catch (TargetParameterCountException ex)
        {
            string message = $"Internal parameter count mismatch for command '{commandName}': {ex.Message}";
            result = SeeleMethodResult.Error(SeeleResultCode.UnknownError, null, message);
        }
        catch (Exception ex)
        {
            string message = $"An unexpected error occurred while processing command '{commandName}': {ex.Message}\n{ex.StackTrace}";
            result = SeeleMethodResult.Error(SeeleResultCode.UnknownError, null, message);
        }

        return result;
    }

    /// <summary>
    /// 执行具体的成员（方法、属性、字段）调用。
    /// </summary>
    /// <param name="targetMemberProvider">命令提供者信息。</param>
    /// <param name="arguments">要传递的参数数组。</param>
    /// <exception cref="TargetParameterCountException">当属性或字段的参数数量不为 1 时抛出。</exception>
    private static async UniTask<SeeleMethodResult> InvokeMember(CommandProviderInfo targetMemberProvider, object[]? arguments)
    {
        var member = targetMemberProvider.Member;

        if (member.IsStatic())
        {
            return await InvokeOnTarget(targetMemberProvider, null, arguments);
        }
        
        Type targetType = member.DeclaringType!;
        IEnumerable<object> targets = InvocationTargetFactory.FindTargets(targetType, targetMemberProvider.TargetType);
        
        bool anyTargetFound = false;
        SeeleMethodResult lastResult = null!;
        foreach (var target in targets)
        {
            anyTargetFound = true;
            lastResult = await InvokeOnTarget(targetMemberProvider, target, arguments);
        }
        
        if (!anyTargetFound)
        {
            string message = $"No target instances of type '{targetType.Name}' found for command '{member.Name}' with target type '{targetMemberProvider.TargetType}'.";
            return SeeleMethodResult.Error(SeeleResultCode.MethodNotFound, null, message);
        }
        
        return lastResult;
    }

    /// <summary>
    /// 对单个目标实例执行命令。
    /// </summary>
    /// <param name="targetMemberProvider">命令的定义信息。</param>
    /// <param name="client">命令执行的目标实例。对于静态命令，此参数为 null。</param>
    /// <param name="arguments">要传递给命令的参数数组。</param>
    /// <exception cref="TargetParameterCountException">当属性或字段的参数数量不为 1 时抛出。</exception>
    private static async UniTask<SeeleMethodResult> InvokeOnTarget(CommandProviderInfo targetMemberProvider, object? client, object[]? arguments)
    {
        var member = targetMemberProvider.Member;

        switch (member)
        {
            case MethodInfo methodInfo:
            {
                return await InvokeMethod(methodInfo, client, arguments);
            }
            case PropertyInfo propertyInfo:
            {
                return InvokeProperty(propertyInfo, client, arguments);
            }
            case FieldInfo fieldInfo:
            {
                return InvokeField(fieldInfo, client, arguments);
            }
            default:
            {
                string message = $"Unsupported member type '{member.MemberType}' for command '{member.Name}' in InvokeMember.";
                return SeeleMethodResult.Error(SeeleResultCode.UnknownError, null, message);
            }
        }
    }

    /// <summary>
    /// 执行方法调用
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <param name="client"></param>
    /// <param name="arguments"></param>
    private static async UniTask<SeeleMethodResult> InvokeMethod(MethodInfo methodInfo, object client, object[]? arguments)
    {
        object? invocationResult = methodInfo.Invoke(client, arguments);

        if (invocationResult != null)
        {
            Type resultType = invocationResult.GetType();
            bool isGenericType = resultType.IsGenericType;

            if (isGenericType)
            {
                // --- 处理泛型类型 ---

                Type genericTypeDefinition = resultType.GetGenericTypeDefinition();

                if (genericTypeDefinition == typeof(UniTask<>))
                {
                    try
                    {
                        var genericArguments = resultType.GetGenericArguments();
                        Type genericArgType = genericArguments[0];
                        MethodInfo convertMethodDefinition = ConvertUniTaskResultToObjectMethodDef;

                        MethodInfo concreteConvertMethod = convertMethodDefinition.MakeGenericMethod(genericArgType);
                        UniTask<SeeleMethodResult> resultUniTask = (UniTask<SeeleMethodResult>)concreteConvertMethod.Invoke(null, new object[] { invocationResult });
                        return await resultUniTask;
                    }
                    catch (Exception ex)
                    {
                        string message = $"Error processing UniTask<{resultType.GenericTypeArguments[0].Name}> via helper: {ex.Message}";
                        return SeeleMethodResult.Error(SeeleResultCode.UnknownError, null, message);
                    }
                }

                if (genericTypeDefinition == typeof(Awaitable<>))
                {
                    try
                    {
                        var genericArguments = resultType.GetGenericArguments();
                        Type genericArgType = genericArguments[0];
                        MethodInfo convertMethodDefinition = ConvertAwaitableResultToObjectMethodDef;

                        MethodInfo concreteConvertMethod = convertMethodDefinition.MakeGenericMethod(genericArgType);
                        UniTask<SeeleMethodResult> resultUniTask = (UniTask<SeeleMethodResult>)concreteConvertMethod.Invoke(null, new object[] { invocationResult });
                        return await resultUniTask;
                    }
                    catch (Exception ex)
                    {
                        string message = $"Error processing Awaitable<{resultType.GenericTypeArguments[0].Name}> via helper: {ex.Message}";
                        return SeeleMethodResult.Error(SeeleResultCode.UnknownError, null, message);
                    }
                }

                if (genericTypeDefinition == typeof(Task<>))
                {
                    Task genericTask = (Task)invocationResult;
                    await genericTask;

                    var genericArguments = resultType.GetGenericArguments();
                    Type genericArgType = genericArguments[0];
                    if (genericArgType.FullName == VoidTaskResultFullName)
                    {
                        // 视为无返回值，返回成功
                        return SeeleMethodResult.Ok;
                    }

                    var genericTaskType = genericTask.GetType();
                    var genericTaskProperty = genericTaskType.GetProperty(ResultFullName);
                    object? genericValue = genericTaskProperty!.GetValue(genericTask);
                    
                    // 如果 Result 本身就是 SeeleMethodResult，直接返回
                    if (genericValue is SeeleMethodResult seeleMethodResult)
                    {
                        return seeleMethodResult;
                    }
                    
                    // 将 Result 作为 CallbackData 包装成成功的 SeeleMethodResult
                    return SeeleMethodResult.Success(callbackdata: genericValue);
                }

                if (genericTypeDefinition == typeof(ValueTask<>))
                {
                    try
                    {
                        var genericArguments = resultType.GetGenericArguments();
                        Type genericArgType = genericArguments[0];
                        MethodInfo convertMethodDefinition = ConvertValueTaskResultToObjectMethodDef;

                        MethodInfo concreteConvertMethod = convertMethodDefinition.MakeGenericMethod(genericArgType);
                        UniTask<SeeleMethodResult> resultUniTask = (UniTask<SeeleMethodResult>)concreteConvertMethod.Invoke(null, new object[] { invocationResult });
                        return await resultUniTask;
                    }
                    catch (Exception ex)
                    {
                        string message = $"Error processing ValueTask<{resultType.GenericTypeArguments[0].Name}> via helper: {ex.Message}";
                        return SeeleMethodResult.Error(SeeleResultCode.UnknownError, null, message);
                    }
                }
            }
            else
            {
                // --- 处理非泛型类型 ---

                if (resultType == typeof(UniTask))
                {
                    await (UniTask)invocationResult;
                    return SeeleMethodResult.Ok;
                }

                // UniTaskVoid (无返回值)
                if (resultType == typeof(UniTaskVoid))
                {
                    return SeeleMethodResult.Ok;
                }

                if (resultType == typeof(Awaitable))
                {
                    await (Awaitable)invocationResult;
                    return SeeleMethodResult.Ok;
                }

                if (resultType == typeof(Task))
                {
                    await (Task)invocationResult;
                    return SeeleMethodResult.Ok;
                }

                if (resultType == typeof(ValueTask))
                {
                    await (ValueTask)invocationResult;
                    return SeeleMethodResult.Ok;
                }
                
                // 如果 invocationResult 本身就是 SeeleMethodResult
                if (invocationResult is SeeleMethodResult seeleMethodResult)
                {
                    return seeleMethodResult;
                }
                
                // 如果 invocationResult 是其他非 TaskLike 类型，视为成功，并将其作为 CallbackData
                return SeeleMethodResult.Success(callbackdata: invocationResult);
            }
        }

        // 如果 invocationResult 是 null (void 方法)，视为成功
        return SeeleMethodResult.Ok;
    }

    /// <summary>
    /// 执行属性调用
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <param name="client"></param>
    /// <param name="arguments"></param>
    /// <exception cref="TargetParameterCountException"></exception>
    private static SeeleMethodResult InvokeProperty(PropertyInfo propertyInfo, object client, object[]? arguments)
    {
        if (arguments is { Length: 1 })
        {
            if (!propertyInfo.CanWrite)
            {
                throw new InvalidOperationException($"Attempted to set value for read-only property '{propertyInfo.Name}'.");
            }

            propertyInfo.SetValue(client, arguments[0]);
        }
        else
        {
            throw new TargetParameterCountException($"Property '{propertyInfo.Name}' expected 1 argument, but InvokeMember received {arguments?.Length ?? 0}. This indicates an internal logic error in argument preparation.");
        }

        if (!propertyInfo.CanRead)
        {
            throw new InvalidOperationException($"Property '{propertyInfo.Name}' is write-only. Cannot get its value.");
        }

        object? result = propertyInfo.GetValue(client);
        return SeeleMethodResult.Success(callbackdata: result);
    }

    /// <summary>
    /// 执行字段调用
    /// </summary>
    /// <param name="fieldInfo"></param>
    /// <param name="client"></param>
    /// <param name="arguments"></param>
    /// <exception cref="TargetParameterCountException"></exception>
    private static SeeleMethodResult InvokeField(FieldInfo fieldInfo, object client, object[]? arguments)
    {
        if (arguments is { Length: 1 })
        {
            fieldInfo.SetValue(client, arguments[0]);
        }
        else
        {
            throw new TargetParameterCountException($"Field '{fieldInfo.Name}' expected 1 argument, but InvokeMember received {arguments?.Length ?? 0}. This indicates an internal logic error in argument preparation.");
        }

        object? result = fieldInfo.GetValue(client);
        return SeeleMethodResult.Success(callbackdata: result);
    }

    /// <summary>
    /// 将 UniTask<TResult> 转换为 UniTask<object?>
    /// </summary>
    /// <param name="uniTask"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    private static async UniTask<SeeleMethodResult> ConvertUniTaskResultToObject<TResult>(UniTask<TResult> uniTask)
    {
        TResult result = await uniTask;
        
        if (result is SeeleMethodResult seeleMethodResult)
        {
            return seeleMethodResult;
        }
        
        if (typeof(TResult).FullName == VoidTaskResultFullName)
        {
            return SeeleMethodResult.Ok;
        }

        return SeeleMethodResult.Success(callbackdata: result);
    }

    /// <summary>
    /// 将 Awaitable<TResult> 转换为 UniTask<object?>
    /// </summary>
    /// <param name="awaitable"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    private static async UniTask<SeeleMethodResult> ConvertAwaitableResultToObject<TResult>(Awaitable<TResult> awaitable)
    {
        TResult result = await awaitable;
        
        if (result is SeeleMethodResult seeleMethodResult)
        {
            return seeleMethodResult;
        }
        
        if (typeof(TResult).FullName == VoidTaskResultFullName)
        {
            return SeeleMethodResult.Ok;
        }

        return SeeleMethodResult.Success(callbackdata: result);
    }

    /// <summary>
    /// 将 ValueTask<TResult> 转换为 UniTask<object?>
    /// </summary>
    /// <param name="valueTask"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    private static async UniTask<SeeleMethodResult> ConvertValueTaskResultToObject<TResult>(ValueTask<TResult> valueTask)
    {
        TResult result = await valueTask;
        
        if (result is SeeleMethodResult seeleMethodResult)
        {
            return seeleMethodResult;
        }
        
        if (typeof(TResult).FullName == VoidTaskResultFullName)
        {
            return SeeleMethodResult.Ok;
        }

        return SeeleMethodResult.Success(callbackdata: result);
    }
}