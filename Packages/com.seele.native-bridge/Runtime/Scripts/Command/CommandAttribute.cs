using System;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CommandAttribute : Attribute
{
    public readonly string Alias;
    public readonly MonoTargetType TargetType;

    public CommandAttribute(string aliasOverride = null)
    {
        Alias = aliasOverride;
        TargetType = MonoTargetType.Single;
    }

    public CommandAttribute(MonoTargetType targetType, string aliasOverride = null)
    {
        Alias = aliasOverride;
        TargetType = targetType;
    }
}