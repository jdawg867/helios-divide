#nullable enable
using System.Reflection;

public class CommandProviderInfo
{
    internal readonly MemberInfo Member;
    internal readonly object? Client;
    internal readonly MonoTargetType TargetType;

    public CommandProviderInfo(MemberInfo member, object? client, MonoTargetType targetType)
    {
        Member = member;
        Client = client;
        TargetType = targetType;
    }
}