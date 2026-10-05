using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// 提供一组用于反射操作的扩展方法。
/// </summary>
public static class ReflectionExtensions
{
    /// <summary>
    /// 在指定的程序集中查找所有带有特定特性（Attribute）的成员。
    /// </summary>
    /// <param name="flags">绑定标志，用于控制搜索。</param>
    /// <param name="assembliesToScan">要扫描的程序集数组。如果为空，则默认扫描游戏相关的程序集。</param>
    /// <returns>包含成员信息和对应特性的元组的枚举。</returns>
    public static IEnumerable<(MemberInfo memberInfo, T attribute)> FindAllMembersByAttribute<T>(BindingFlags flags, params Assembly[] assembliesToScan) where T : Attribute
    {
        if (assembliesToScan.Length <= 0)
        {
            assembliesToScan = GetGameAssemblies();
        }

        return assembliesToScan.SelectMany(assembly => assembly.GetTypes()).SelectMany(type => GetMembersByAttribute<T>(flags, type));
    }

    /// <summary>
    /// 在指定的类型中获取所有带有特定特性（Attribute）的成员。
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="client"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IEnumerable<(MemberInfo memberInfo, T attribute)> GetMembersByAttribute<T>(BindingFlags flags, Type client) where T : Attribute
    {
        return client.GetMembers(flags).Select(memberInfo => (memberInfo, memberInfo.GetCustomAttribute<T>())).Where(tuple => tuple.Item2 != null);
    }

    /// <summary>
    /// 判断成员是否是静态的。
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    public static bool IsStatic(this MemberInfo member)
    {
        return member switch
        {
            MethodInfo method => method.IsStatic,
            PropertyInfo property => (property.GetGetMethod(true)?.IsStatic ?? property.GetSetMethod(true)?.IsStatic) ?? false,
            FieldInfo field => field.IsStatic,
            _ => false
        };
    }

    /// <summary>
    /// 获取与游戏逻辑相关的程序集，过滤掉 Unity 引擎和系统库。
    /// </summary>
    /// <returns></returns>
    private static Assembly[] GetGameAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies().Where(assembly =>
            !(assembly.FullName.StartsWith("Unity") ||
              assembly.FullName.StartsWith("System") ||
              assembly.FullName.StartsWith("mscorlib") ||
              assembly.FullName.StartsWith("netstandard") ||
              assembly.FullName.StartsWith("Microsoft"))
        ).ToArray();
    }

    #region 查找表 (用于类型显示名称和转换)

    private static readonly Dictionary<Type, string> TypeDisplayNames = new Dictionary<Type, string>
    {
        { typeof(int), "int" },
        { typeof(float), "float" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(string), "string" },
        { typeof(bool), "bool" },
        { typeof(byte), "byte" },
        { typeof(sbyte), "sbyte" },
        { typeof(uint), "uint" },
        { typeof(short), "short" },
        { typeof(ushort), "ushort" },
        { typeof(long), "decimal" },
        { typeof(ulong), "ulong" },
        { typeof(char), "char" },
        { typeof(object), "object" }
    };

    private static readonly Type[] ValueTupleTypes =
    {
        typeof(ValueTuple<>),
        typeof(ValueTuple<,>),
        typeof(ValueTuple<,,>),
        typeof(ValueTuple<,,,>),
        typeof(ValueTuple<,,,,>),
        typeof(ValueTuple<,,,,,>),
        typeof(ValueTuple<,,,,,,>),
        typeof(ValueTuple<,,,,,,,>)
    };

    private static readonly Type[][] PrimitiveTypeCastHierarchy =
    {
        new[] { typeof(byte), typeof(sbyte), typeof(char) },
        new[] { typeof(short), typeof(ushort) },
        new[] { typeof(int), typeof(uint) },
        new[] { typeof(long), typeof(ulong) },
        new[] { typeof(float) },
        new[] { typeof(double) }
    };

    #endregion

    /// <summary>判断一个类型是否是委托。</summary>
    /// <returns>如果类型是委托，则为 true。</returns>
    public static bool IsDelegate(this Type type)
    {
        if (!typeof(Delegate).IsAssignableFrom(type))
        {
            return false;
        }

        return true;
    }

    /// <summary>判断一个类型是否是强类型委托。</summary>
    /// <returns>如果类型是强类型委托，则为 true。</returns>
    public static bool IsStrongDelegate(this Type type)
    {
        if (!type.IsDelegate())
        {
            return false;
        }

        if (type.IsAbstract)
        {
            return false;
        }

        return true;
    }

    /// <summary>判断一个字段是否是委托。</summary>
    /// <returns>如果字段是委托，则为 true。</returns>
    public static bool IsDelegate(this FieldInfo fieldInfo)
    {
        return fieldInfo.FieldType.IsDelegate();
    }

    /// <summary>判断一个字段是否是强类型委托。</summary>
    /// <returns>如果字段是强类型委托，则为 true。</returns>
    public static bool IsStrongDelegate(this FieldInfo fieldInfo)
    {
        return fieldInfo.FieldType.IsStrongDelegate();
    }

    /// <summary>
    /// 判断一个泛型类型是否是由给定的非泛型类型定义创建的。
    /// </summary>
    /// <param name="nonGenericType">要测试的非泛型类型定义。</param>
    /// <returns>如果是，则为 true。</returns>
    public static bool IsGenericTypeOf(this Type genericType, Type nonGenericType)
    {
        if (!genericType.IsGenericType)
        {
            return false;
        }

        return genericType.GetGenericTypeDefinition() == nonGenericType;
    }

    /// <summary>
    /// 判断一个类型是否是给定基类型的派生类型。
    /// </summary>
    /// <param name="baseType">要测试的基类型。</param>
    /// <returns>如果是，则为 true。</returns>
    public static bool IsDerivedTypeOf(this Type type, Type baseType)
    {
        return baseType.IsAssignableFrom(type);
    }

    /// <summary>
    /// 判断一个类型的对象是否可以被转换为指定类型。
    /// </summary>
    /// <param name="to">目标转换类型。</param>
    /// <param name="implicitly">如果为 true，则只考虑隐式转换。</param>
    /// <returns>如果可以转换，则为 true。</returns>
    public static bool IsCastableTo(this Type from, Type to, bool implicitly = false)
    {
        return to.IsAssignableFrom(from) || from.HasCastDefined(to, implicitly);
    }

    private static bool HasCastDefined(this Type from, Type to, bool implicitly)
    {
        if ((from.IsPrimitive || from.IsEnum) && (to.IsPrimitive || to.IsEnum))
        {
            if (!implicitly)
            {
                return from == to || (from != typeof(bool) && to != typeof(bool));
            }

            IEnumerable<Type> lowerTypes = Enumerable.Empty<Type>();
            foreach (Type[] types in PrimitiveTypeCastHierarchy)
            {
                if (types.Any(t => t == to))
                {
                    return lowerTypes.Any(t => t == from);
                }

                lowerTypes = lowerTypes.Concat(types);
            }

            return false; // IntPtr, UIntPtr, Enum, Boolean
        }

        return IsCastDefined(to, m => m.GetParameters()[0].ParameterType, _ => from, implicitly, false)
               || IsCastDefined(from, _ => to, m => m.ReturnType, implicitly, true);
    }

    private static bool IsCastDefined(Type type, Func<MethodInfo, Type> baseType, Func<MethodInfo, Type> derivedType, bool implicitly, bool lookInBase)
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Static | (lookInBase ? BindingFlags.FlattenHierarchy : BindingFlags.DeclaredOnly);
        MethodInfo[] methods = type.GetMethods(flags);

        return methods.Where(m => m.Name == "op_Implicit" || (!implicitly && m.Name == "op_Explicit"))
            .Any(m => baseType(m).IsAssignableFrom(derivedType(m)));
    }

    /// <summary>
    /// 动态地将一个对象转换为指定类型。
    /// </summary>
    /// <param name="type">目标转换类型。</param>
    /// <param name="data">要转换的对象。</param>
    /// <returns>动态转换后的对象。</returns>
    public static object Cast(this Type type, object data)
    {
        if (type.IsInstanceOfType(data))
        {
            return data;
        }

        try
        {
            return Convert.ChangeType(data, type);
        }
        catch (InvalidCastException)
        {
            Type srcType = data.GetType();
            ParameterExpression dataParam = Expression.Parameter(srcType, "data");
            Expression body = Expression.Convert(Expression.Convert(dataParam, srcType), type);

            Delegate run = Expression.Lambda(body, dataParam).Compile();
            return run.DynamicInvoke(data);
        }
    }

    /// <summary>判断给定的方法是否是一个重写（override）方法。</summary>
    /// <returns>如果是重写方法，则为 true。</returns>
    public static bool IsOverride(this MethodInfo methodInfo)
    {
        return methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType;
    }

    /// <summary>
    /// 判断提供者是否具有指定的特性。
    /// </summary>
    /// <typeparam name="T">要测试的特性类型。</typeparam>
    /// <param name="provider">特性提供者。</param>
    /// <param name="searchInherited">如果为 true，则搜索继承链。</param>
    /// <returns>如果特性存在，则为 true。</returns>
    public static bool HasAttribute<T>(this ICustomAttributeProvider provider, bool searchInherited = true) where T : Attribute
    {
        try
        {
            return provider.IsDefined(typeof(T), searchInherited);
        }
        catch (MissingMethodException)
        {
            return false;
        }
    }

    /// <summary>
    /// 获取给定类型的格式化显示名称。
    /// </summary>
    /// <param name="type">要生成显示名称的类型。</param>
    /// <param name="includeNamespace">如果为 true，则在生成类型名称时包含命名空间。</param>
    /// <returns>生成的显示名称。</returns>
    public static string GetDisplayName(this Type type, bool includeNamespace = false)
    {
        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (type.IsArray)
        {
            int rank = type.GetArrayRank();
            string innerTypeName = GetDisplayName(type.GetElementType(), includeNamespace);
            return $"{innerTypeName}[{new string(',', rank - 1)}]";
        }

        if (TypeDisplayNames.ContainsKey(type))
        {
            string baseName = TypeDisplayNames[type];
            if (type.IsGenericType && !type.IsConstructedGenericType)
            {
                Type[] genericArgs = type.GetGenericArguments();
                return $"{baseName}<{new string(',', genericArgs.Length - 1)}>";
            }

            return baseName;
        }

        if (type.IsGenericTypeOf(typeof(Nullable<>)))
        {
            Type innerType = type.GetGenericArguments()[0];
            return $"{innerType.GetDisplayName()}?";
        }

        if (type.IsGenericType)
        {
            Type baseType = type.GetGenericTypeDefinition();
            Type[] genericArgs = type.GetGenericArguments();

            if (ValueTupleTypes.Contains(baseType))
            {
                return GetTupleDisplayName(type, includeNamespace);
            }

            if (type.IsConstructedGenericType)
            {
                string[] genericNames = new string[genericArgs.Length];
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    genericNames[i] = GetDisplayName(genericArgs[i], includeNamespace);
                }

                string baseName = GetDisplayName(baseType, includeNamespace).Split('<')[0];
                return $"{baseName}<{string.Join(", ", genericNames)}>";
            }

            string typeName = includeNamespace
                ? type.FullName
                : type.Name;

            return $"{typeName.Split('`')[0]}<{new string(',', genericArgs.Length - 1)}>";
        }

        Type declaringType = type.DeclaringType;
        if (declaringType != null)
        {
            string declaringName = GetDisplayName(declaringType, includeNamespace);
            return $"{declaringName}.{type.Name}";
        }

        return includeNamespace
            ? type.FullName
            : type.Name;
    }

    private static string GetTupleDisplayName(this Type type, bool includeNamespace = false)
    {
        IEnumerable<string> parts = type
            .GetGenericArguments()
            .Select(x => x.GetDisplayName(includeNamespace));

        return $"({string.Join(", ", parts)})";
    }

    /// <summary>
    /// 判断两个来自不同类型的方法是否具有相同的签名。
    /// </summary>
    /// <param name="a">First method</param>
    /// <param name="b">Second method</param>
    /// <returns><c>true</c> if they are equal</returns>
    public static bool AreMethodsEqual(MethodInfo a, MethodInfo b)
    {
        if (a.Name != b.Name) return false;

        ParameterInfo[] paramsA = a.GetParameters();
        ParameterInfo[] paramsB = b.GetParameters();

        if (paramsA.Length != paramsB.Length) return false;
        for (int i = 0; i < paramsA.Length; i++)
        {
            ParameterInfo pa = paramsA[i];
            ParameterInfo pb = paramsB[i];

            if (pa.Name != pb.Name) return false;
            if (pa.HasDefaultValue != pb.HasDefaultValue) return false;

            Type ta = pa.ParameterType;
            Type tb = pb.ParameterType;

            if (!ta.ContainsGenericParameters && !tb.ContainsGenericParameters)
            {
                if (ta != tb) return false;
            }
        }

        if (a.IsGenericMethod != b.IsGenericMethod) return false;
        if (a.IsGenericMethod && b.IsGenericMethod)
        {
            Type[] genericA = a.GetGenericArguments();
            Type[] genericB = b.GetGenericArguments();

            if (genericA.Length != genericB.Length) return false;
            for (int i = 0; i < genericA.Length; i++)
            {
                Type ga = genericA[i];
                Type gb = genericB[i];

                if (ga.Name != gb.Name) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 通过查找具有相同签名的方法，将一个方法“重定位”到新的类型上。
    /// </summary>
    /// <param name="method">Method to rebase</param>
    /// <param name="newBase">New type to rebase the method onto</param>
    /// <returns>The rebased method</returns>
    public static MethodInfo RebaseMethod(this MethodInfo method, Type newBase)
    {
        BindingFlags flags = BindingFlags.Default;

        flags |= method.IsStatic
            ? BindingFlags.Static
            : BindingFlags.Instance;

        flags |= method.IsPublic
            ? BindingFlags.Public
            : BindingFlags.NonPublic;

        MethodInfo[] candidates = newBase.GetMethods(flags)
            .Where(x => AreMethodsEqual(x, method))
            .ToArray();

        if (candidates.Length == 0)
        {
            throw new ArgumentException($"Could not rebase method {method} onto type {newBase} as no matching candidates were found");
        }

        if (candidates.Length > 1)
        {
            throw new ArgumentException($"Could not rebase method {method} onto type {newBase} as too many matching candidates were found");
        }

        return candidates[0];
    }
}