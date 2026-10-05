using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public static class CommandDataResolver
{
    public const char CommandNameDelimiter = ':'; // 解析命令名称分隔符
    public const string ArgumentsDelimiter = ";"; // 解析命令参数分隔符
    
    /// <summary>
    /// 将单个字符串值转换为目标类型。
    /// </summary>
    /// <param name="value">要转换的字符串值（已 Trim）。</param>
    /// <param name="type">目标类型。</param>
    /// <returns>转换后的 object。</returns>
    /// <exception cref="ArgumentException">当为 UnityEngine.Object 类型提供空路径时抛出，或 Enum.Parse 遇到无效名称时。</exception>
    /// <exception cref="InvalidCastException">当参数值无法转换为期望类型时抛出。</exception>
    public static object GetData(string value, Type type)
    {
        // 处理 Nullable 类型和 "null" 字面量
        if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            {
                // 如果是不可为空的值类型，但收到了 "null" 字符串，则抛出错误
                throw new InvalidCastException($"Cannot convert 'null' string to non-nullable value type '{type.Name}'.");
            }

            return null; // 对于引用类型和可空值类型，将 "null" 字符串转换为真正的 null
        }

        // 处理目标类型是string的字符串
        if (type == typeof(string))
        {
            return value;
        }

        // 处理 UnityEngine.Object 及其子类
        if (type.IsSubclassOf(typeof(UnityEngine.Object)) || type == typeof(UnityEngine.Object))
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"Path for UnityEngine.Object type '{type.Name}' cannot be empty or null.");
            }

            var loadedAsset = Resources.Load(value, type);
            if (loadedAsset == null && !type.IsInterface && !type.IsAbstract && !type.IsGenericTypeDefinition)
            {
                Debug.LogWarning($"Resources.Load for value '{value}' of type '{type.Name}' returned null. Asset might be missing or path incorrect.");
            }

            return loadedAsset;
        }
        // 处理 Enum 类型
        else if (type.IsEnum)
        {
            string enumValueName = value;
            int lastDotIndex = value.LastIndexOf('.');
            if (lastDotIndex > 0 && lastDotIndex < value.Length - 1)
            {
                string typeNamePart = value.Substring(0, lastDotIndex);
                if (string.Equals(typeNamePart, type.Name, StringComparison.OrdinalIgnoreCase))
                {
                    enumValueName = value.Substring(lastDotIndex + 1);
                }
            }

            try
            {
                return Enum.Parse(type, enumValueName, true);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidCastException(
                    $"Failed to convert '{value}' to enum type '{type.Name}'. " +
                    $"Valid names are: {string.Join(", ", Enum.GetNames(type))}. Details: {ex.Message}", ex);
            }
        }
        // 处理 Unity 数学结构体 (Vector2, Vector3, Vector4, Color, Quaternion, Rect, Bounds)
        else if (type == typeof(Vector2))
        {
            return ParseVector2(value);
        }
        else if (type == typeof(Vector3))
        {
            return ParseVector3(value);
        }
        else if (type == typeof(Vector4))
        {
            return ParseVector4(value);
        }
        else if (type == typeof(Color))
        {
            return ParseColor(value);
        }
        else if (type == typeof(Quaternion))
        {
            return ParseQuaternion(value);
        }
        else if (type == typeof(Rect))
        {
            return ParseRect(value);
        }
        else if (type == typeof(Bounds))
        {
            return ParseBounds(value);
        }
        // 处理 System.Type
        else if (type == typeof(Type))
        {
            Type foundType = Type.GetType(value, false, true); // false: 不抛出异常, true: 忽略大小写
            if (foundType == null)
            {
                // 尝试在所有加载的Assembly中查找
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foundType = assembly.GetType(value, false, true);
                    if (foundType != null) break;
                }
            }

            if (foundType == null)
            {
                throw new ArgumentException($"Type '{value}' not found.");
            }

            return foundType;
        }
        // 处理 bool 类型的多种字符串表示
        else if (type == typeof(bool))
        {
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("on", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("1", StringComparison.OrdinalIgnoreCase))
                return true;
            if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value.Equals("off", StringComparison.OrdinalIgnoreCase) || value.Equals("no", StringComparison.OrdinalIgnoreCase) || value.Equals("0", StringComparison.OrdinalIgnoreCase))
                return false;
            throw new InvalidCastException($"Failed to convert '{value}' to bool. Valid inputs are 'true', 'false', 'on', 'off', 'yes', 'no', '1', '0'.");
        }
        // 处理Newtonsoft.Json.Linq.JObject类型
        else if(type == typeof(Newtonsoft.Json.Linq.JObject))
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(value, type);
        }
        // 处理所有基本类型、字符串、数字等
        else if (type.IsPrimitive || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid) || type == typeof(TimeSpan) || type == typeof(Uri))
        {
            try
            {
                return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
            {
                throw new InvalidCastException($"Failed to convert '{value}' to type '{type.Name}'. Input was not in a correct format. Details: {ex.Message}", ex);
            }
        }
        // 处理可空值类型 (例如 int?, bool?)
        else if (Nullable.GetUnderlyingType(type) != null) 
        {
            Type underlyingType = Nullable.GetUnderlyingType(type)!;
            
            if (string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            
            // 将字符串值转换为可空类型的基础类型。
            object convertedValue = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
            return convertedValue;
        }
        // 处理自定义或复杂类型（类或结构体），默认字符串是其 JSON 表达。
        else
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(value, type);
        }
    }

    private static float ParseFloat(string s, string paramName)
    {
        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }

        throw new InvalidCastException($"Invalid float format for {paramName}: '{s}'.");
    }

    private static Vector2 ParseVector2(string value)
    {
        string[] components = value.Trim().TrimStart('(').TrimEnd(')').Split(',');
        if (components.Length != 2) throw new ArgumentException($"Invalid Vector2 format: '{value}'. Expected 2 components (e.g., 'X,Y').");
        return new Vector2(ParseFloat(components[0], "X"), ParseFloat(components[1], "Y"));
    }

    private static Vector3 ParseVector3(string value)
    {
        string[] components = value.Trim().TrimStart('(').TrimEnd(')').Split(',');
        if (components.Length != 3) throw new ArgumentException($"Invalid Vector3 format: '{value}'. Expected 3 components (e.g., 'X,Y,Z').");
        return new Vector3(ParseFloat(components[0], "X"), ParseFloat(components[1], "Y"), ParseFloat(components[2], "Z"));
    }

    private static Vector4 ParseVector4(string value)
    {
        string[] components = value.Trim().TrimStart('(').TrimEnd(')').Split(',');
        if (components.Length != 4) throw new ArgumentException($"Invalid Vector4 format: '{value}'. Expected 4 components (e.g., 'X,Y,Z,W').");
        return new Vector4(ParseFloat(components[0], "X"), ParseFloat(components[1], "Y"), ParseFloat(components[2], "Z"), ParseFloat(components[3], "W"));
    }

    private static Color ParseColor(string value)
    {
        string[] components = value.Trim().TrimStart('(').TrimEnd(')').Split(',');
        if (components.Length != 3 && components.Length != 4) throw new ArgumentException($"Invalid Color format: '{value}'. Expected 3 (RGB) or 4 (RGBA) components (e.g., 'R,G,B' or 'R,G,B,A').");

        float r = ParseFloat(components[0], "R");
        float g = ParseFloat(components[1], "G");
        float b = ParseFloat(components[2], "B");
        float a = (components.Length == 4) ? ParseFloat(components[3], "A") : 1.0f;
        return new Color(r, g, b, a);
    }

    private static Quaternion ParseQuaternion(string value)
    {
        string[] components = value.Trim().TrimStart('(').TrimEnd(')').Split(',');
        if (components.Length != 4) throw new ArgumentException($"Invalid Quaternion format: '{value}'. Expected 4 components (e.g., 'X,Y,Z,W').");
        return new Quaternion(ParseFloat(components[0], "X"), ParseFloat(components[1], "Y"), ParseFloat(components[2], "Z"), ParseFloat(components[3], "W"));
    }

    private static Rect ParseRect(string value)
    {
        string[] components = value.Trim().TrimStart('(').TrimEnd(')').Split(',');
        if (components.Length != 4) throw new ArgumentException($"Invalid Rect format: '{value}'. Expected 4 components (e.g., 'X,Y,Width,Height').");
        return new Rect(ParseFloat(components[0], "X"), ParseFloat(components[1], "Y"), ParseFloat(components[2], "Width"), ParseFloat(components[3], "Height"));
    }

    private static Bounds ParseBounds(string value)
    {
        string[] components = value.Trim().TrimStart('(').TrimEnd(')').Split(',');
        if (components.Length != 6) throw new ArgumentException($"Invalid Bounds format: '{value}'. Expected 6 components (e.g., 'CenterX,CenterY,CenterZ,SizeX,SizeY,SizeZ').");

        Vector3 center = new Vector3(ParseFloat(components[0], "CenterX"), ParseFloat(components[1], "CenterY"), ParseFloat(components[2], "CenterZ"));
        Vector3 size = new Vector3(ParseFloat(components[3], "SizeX"), ParseFloat(components[4], "SizeY"), ParseFloat(components[5], "SizeZ"));
        return new Bounds(center, size);
    }


    public static Type GetParameterType(this MemberInfo member)
    {
        switch (member.MemberType)
        {
            case MemberTypes.Event:
                return ((EventInfo)member).EventHandlerType;
            case MemberTypes.Field:
                return ((FieldInfo)member).FieldType;
            case MemberTypes.Method:
                var method = (MethodInfo)member;
                var parameters = method.GetParameters();
                if (parameters.Length > 1)
                    throw new Exception("Method can only have one parameter");
                return parameters.Length == 0 ? null : parameters[0].ParameterType;
            case MemberTypes.Property:
                return ((PropertyInfo)member).PropertyType;
            default:
                throw new ArgumentException
                (
                    "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
                );
        }
    }

    /// <summary>
    /// 获取指定成员（方法/属性/字段）的所有参数类型。
    /// </summary>
    /// <param name="member">成员信息。</param>
    /// <returns>参数类型数组。</returns>
    /// <exception cref="ArgumentException">当 MemberInfo 类型不被支持时抛出。</exception>
    public static Type[] GetParameterTypes(this MemberInfo member)
    {
        switch (member.MemberType)
        {
            case MemberTypes.Field:
                return new[] { ((FieldInfo)member).FieldType };
            case MemberTypes.Method:
                var method = (MethodInfo)member;
                return method.GetParameters().Select(p => p.ParameterType).ToArray();
            case MemberTypes.Property:
                return new[] { ((PropertyInfo)member).PropertyType };
            default:
                throw new ArgumentException($"Input MemberInfo must be of type FieldInfo, MethodInfo, or PropertyInfo. Received: {member.MemberType}");
        }
    }

    /// <summary>
    /// 将字符串数组值转换为目标类型数组。
    /// 支持 UnityEngine.Object 类型通过 Resources.Load 加载。
    /// 强调严格的参数数量匹配和类型转换。
    /// </summary>
    /// <param name="stringValues">要转换的原始字符串值数组。建议为非 null。</param>
    /// <param name="expectedTypes">目标参数类型数组。不应为 null。</param>
    /// <returns>转换后的 object 数组。</returns>
    /// <exception cref="ArgumentNullException">当 expectedTypes 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当参数数量不匹配，或必要参数未提供时抛出。</exception>
    /// <exception cref="InvalidCastException">当参数值无法转换为期望类型时抛出。</exception>
    public static object[] ConvertArguments(string[] stringValues, Type[] expectedTypes)
    {
        if (expectedTypes == null)
        {
            throw new ArgumentNullException(nameof(expectedTypes), "Expected types array cannot be null.");
        }

        if (stringValues == null)
        {
            stringValues = Array.Empty<string>();
        }

        // 无参数
        if (expectedTypes.Length == 0)
        {
            if (stringValues.Length > 0 && !(stringValues.Length == 1 && string.IsNullOrEmpty(stringValues[0])))
            {
                throw new ArgumentException($"Command expects no arguments, but received {stringValues.Length}: '{string.Join(",", stringValues)}'.");
            }

            return Array.Empty<object>();
        }

        // 有参数，但未提供任何有意义的参数字符串
        bool noMeaningfulValuesProvided = (stringValues.Length == 0);
        if (noMeaningfulValuesProvided)
        {
            throw new ArgumentException($"Command expects {expectedTypes.Length} argument(s) (types: {string.Join(", ", expectedTypes.Select(t => t.Name))}), but none were provided.");
        }

        // 参数数量不匹配 (在提供了有意义参数的前提下)
        if (stringValues.Length != expectedTypes.Length)
        {
            throw new ArgumentException($"Argument count mismatch. Command expects {expectedTypes.Length} (types: {string.Join(", ", expectedTypes.Select(t => t.Name))}), but received {stringValues.Length} (values: '{string.Join(",", stringValues)}').");
        }

        object[] convertedArgs = new object[expectedTypes.Length];
        for (int i = 0; i < expectedTypes.Length; i++)
        {
            string currentStringValue = stringValues[i].Trim();
            Type currentExpectedType = expectedTypes[i];
            try
            {
                convertedArgs[i] = GetData(currentStringValue, currentExpectedType);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        return convertedArgs;
    }

    /// <summary>
    /// 解析原始命令字符串，提取命令名和参数字符串数组。
    /// 格式示例："CommandName" 或 "CommandName:arg1,arg2,(arg3a,arg3b),arg4"
    /// 支持解析包含平衡括号内逗号的参数，如 Vector3(1,1,1)。
    /// 支持尖括号 <>,中括号[],大括号{}, 双引号 "" 和单引号 '' 包裹的参数。
    /// 支持英文冒号 ":" 和中文冒号 "：" 作为命令名和参数分隔符。
    /// </summary>
    /// <param name="rawCommandInput">原始命令字符串，可能包含富文本标签。</param>
    /// <returns>包含命令名和参数字符串数组的元组。DataKeys 总是非 null 的 string[]。</returns>
    public static (string CommandName, string[] DataKeys) ParseRawCommand(string rawCommandInput)
    {
        if (string.IsNullOrWhiteSpace(rawCommandInput))
        {
            Debug.LogWarning("ParseRawCommand received an empty or whitespace string.");
            return (string.Empty, Array.Empty<string>());
        }

        string commandName;
        string rawArgumentsString = null;
        int colonIndex = rawCommandInput.IndexOf(CommandNameDelimiter);

        var cleanedCommand = RemoveRichTextTags(rawCommandInput);
        if (colonIndex == -1)
        {
            commandName = cleanedCommand.Trim();
            return (commandName, Array.Empty<string>());
        }

        commandName = cleanedCommand.Substring(0, colonIndex).Trim();
        if (colonIndex < rawCommandInput.Length - 1)
        {
            rawArgumentsString = rawCommandInput.Substring(colonIndex + 1).Trim();
        }

        string[] dataKeys;
        if (string.IsNullOrEmpty(rawArgumentsString))
        {
            dataKeys = Array.Empty<string>();
        }
        else
        {
            dataKeys = rawArgumentsString.Split(new[] { ArgumentsDelimiter }, StringSplitOptions.None);
        }

        return (commandName, dataKeys);
    }

    /// <summary>
    /// 使用正则表达式移除所有形如 "<...>" 的富文本标签。
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string RemoveRichTextTags(string input)
    {
        return Regex.Replace(input, "<.*?>", string.Empty);
    }

    /// <summary>
    /// 智能分割参数字符串，识别并跳过平衡括号（()、[]、{}、<>、【】、〈〉、｛｝）内部的逗号。
    /// 并支持双引号 (""、“” ) 和单引号 (''、‘’ ) 包裹的字符串参数，引号内的内容不触发分隔或平衡计数。
    /// </summary>
    /// <param name="rawArgumentsString">待分割的原始参数字符串。</param>
    /// <returns>分割后的参数字符串数组，每个参数已去除首尾空格。</returns>
    private static string[] SmartSplitArguments(string rawArgumentsString)
    {
        if (string.IsNullOrEmpty(rawArgumentsString))
        {
            return Array.Empty<string>();
        }

        List<string> arguments = new List<string>();
        StringBuilder currentArgument = new StringBuilder();
        int balanceCount = 0;
        
        bool inEnglishDoubleQuote = false;
        bool inEnglishSingleQuote = false;
        bool inChineseDoubleQuote = false;
        bool inChineseSingleQuote = false;

        for (int i = 0; i < rawArgumentsString.Length; i++)
        {
            char c = rawArgumentsString[i];
            
            if (inEnglishDoubleQuote)
            {
                if (c == '"')
                {
                    inEnglishDoubleQuote = false;
                    continue;
                }
            }
            else if (inEnglishSingleQuote)
            {
                if (c == '\'')
                {
                    inEnglishSingleQuote = false;
                    continue;
                }
            }
            else if (inChineseDoubleQuote)
            {
                if (c == '”')
                {
                    inChineseDoubleQuote = false;
                    continue;
                }
            }
            else if (inChineseSingleQuote)
            {
                if (c == '’')
                {
                    inChineseSingleQuote = false;
                    continue;
                }
            }
            else
            {
                if (c == '"')
                {
                    inEnglishDoubleQuote = true;
                    continue;
                }
                else if (c == '\'')
                {
                    inEnglishSingleQuote = true;
                    continue;
                }
                else if (c == '“')
                {
                    inChineseDoubleQuote = true;
                    continue;
                }
                else if (c == '‘')
                {
                    inChineseSingleQuote = true;
                    continue;
                }
            }
            
            if (inEnglishDoubleQuote || inEnglishSingleQuote || inChineseDoubleQuote || inChineseSingleQuote)
            {
                currentArgument.Append(c);
                continue;
            }
            
            switch (c)
            {
                case '(':
                case '[':
                case '{':
                case '<':
                case '【':
                case '《':
                case '（':
                case '｛':
                case '＜':
                    balanceCount++;
                    currentArgument.Append(c);
                    break;
                case ')':
                case ']':
                case '}':
                case '>':
                case '】':
                case '》':
                case '）':
                case '｝':
                case '＞':
                    balanceCount--;
                    currentArgument.Append(c);
                    break;
                case ',':
                case '，':
                    if (balanceCount == 0)
                    {
                        arguments.Add(currentArgument.ToString().Trim());
                        currentArgument.Clear();
                    }
                    else
                    {
                        currentArgument.Append(c);
                    }

                    break;
                default:
                    currentArgument.Append(c);
                    break;
            }
        }

        if (currentArgument.Length > 0)
        {
            arguments.Add(currentArgument.ToString().Trim());
        }
        
        if (inEnglishDoubleQuote || inEnglishSingleQuote || inChineseDoubleQuote || inChineseSingleQuote)
        {
            Debug.LogWarning("SmartSplitArguments: Unmatched quote found in command arguments.");
        }

        return arguments.ToArray();
    }
}