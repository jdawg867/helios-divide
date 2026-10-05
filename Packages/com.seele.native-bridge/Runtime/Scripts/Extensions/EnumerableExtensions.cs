using System;
using System.Collections.Generic;

public static class EnumerableExtensions
{
    /// <summary>
    /// 将单个元素转换为一个可枚举序列
    /// </summary>
    public static IEnumerable<T> Yield<T>(this T item)
    {
        yield return item;
    }
}