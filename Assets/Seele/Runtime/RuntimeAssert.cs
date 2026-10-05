using System;
using System.Collections.Generic;
using UnityEngine;

public static class RuntimeAssert
{
    public static object AssertTrue(string assertionType, string target, bool condition, object evidence = null, string failureMessage = null)
    {
        return BuildResult(assertionType, target, condition, evidence, condition ? null : failureMessage ?? $"Assertion failed: {assertionType}");
    }

    public static object AssertObjectExists(string target)
    {
        var query = RuntimeProbe.QueryObjectExists(target) as Dictionary<string, object>;
        var data = GetNestedDictionary(query, "data");
        var exists = GetBool(data, "exists");
        return BuildResult("object_exists", target, exists, query, exists ? null : $"GameObject not found: {target}");
    }

    public static object AssertComponentExists(string target, string componentName)
    {
        var query = RuntimeProbe.QueryComponentExists(target, componentName) as Dictionary<string, object>;
        var data = GetNestedDictionary(query, "data");
        var exists = GetBool(data, "exists");
        return BuildResult("component_exists", target, exists, query, exists ? null : $"Component not found: {componentName}");
    }

    public static object AssertFieldEquals(string target, string componentName, string fieldName, object expected)
    {
        var query = RuntimeProbe.QueryComponentField(target, componentName, fieldName) as Dictionary<string, object>;
        var data = GetNestedDictionary(query, "data");
        var actual = data != null && data.ContainsKey("value") ? data["value"] : null;
        var passed = ValuesEqual(actual, expected);
        return BuildResult(
            "field_equals",
            target,
            passed,
            new Dictionary<string, object>
            {
                ["query"] = query,
                ["expected"] = expected,
                ["actual"] = actual,
            },
            passed ? null : $"Expected {fieldName} to equal {expected}, got {actual}"
        );
    }

    public static object AssertPositionChanged(string target, Vector3 before, Vector3 after, float minDistance = 0.01f)
    {
        var distance = Vector3.Distance(before, after);
        var passed = distance >= minDistance;
        return BuildResult(
            "position_changed",
            target,
            passed,
            new Dictionary<string, object>
            {
                ["before"] = ToVector3(before),
                ["after"] = ToVector3(after),
                ["distance"] = distance,
                ["minDistance"] = minDistance,
            },
            passed ? null : $"Expected position change >= {minDistance}, got {distance}"
        );
    }

    public static object AssertValueChanged(string target, object before, object after)
    {
        var passed = !ValuesEqual(before, after);
        return BuildResult(
            "value_changed",
            target,
            passed,
            new Dictionary<string, object>
            {
                ["before"] = before,
                ["after"] = after,
            },
            passed ? null : "Expected value to change, but it did not."
        );
    }

    public static object AssertUiTextContains(string target, string expectedSubstring)
    {
        var query = RuntimeProbe.QueryUIState(target) as Dictionary<string, object>;
        var data = GetNestedDictionary(query, "data");
        var textBlock = GetNestedDictionary(data, "text");
        var tmpTextBlock = GetNestedDictionary(data, "tmpText");
        var textValue = GetString(textBlock, "value") ?? GetString(tmpTextBlock, "value") ?? string.Empty;
        var passed = textValue.IndexOf(expectedSubstring ?? string.Empty, StringComparison.Ordinal) >= 0;
        return BuildResult(
            "ui_text_contains",
            target,
            passed,
            new Dictionary<string, object>
            {
                ["query"] = query,
                ["expectedSubstring"] = expectedSubstring,
                ["actualText"] = textValue,
            },
            passed ? null : $"Expected UI text to contain '{expectedSubstring}', got '{textValue}'"
        );
    }

    private static Dictionary<string, object> BuildResult(string assertionType, string target, bool passed, object evidence, string error)
    {
        return new Dictionary<string, object>
        {
            ["ok"] = passed,
            ["assertion_type"] = assertionType,
            ["target"] = target,
            ["passed"] = passed,
            ["evidence"] = evidence,
            ["error"] = error,
        };
    }

    private static bool ValuesEqual(object a, object b)
    {
        if (a == null && b == null)
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        if (a is IConvertible && b is IConvertible)
        {
            try
            {
                var da = Convert.ToDouble(a);
                var db = Convert.ToDouble(b);
                return Math.Abs(da - db) < 0.0001d;
            }
            catch
            {
                // Fall through to default equality.
            }
        }

        return Equals(a, b);
    }

    private static Dictionary<string, object> GetNestedDictionary(Dictionary<string, object> dict, string key)
    {
        if (dict == null || string.IsNullOrWhiteSpace(key) || !dict.ContainsKey(key))
        {
            return null;
        }

        return dict[key] as Dictionary<string, object>;
    }

    private static bool GetBool(Dictionary<string, object> dict, string key)
    {
        if (dict == null || !dict.ContainsKey(key) || dict[key] == null)
        {
            return false;
        }

        return Convert.ToBoolean(dict[key]);
    }

    private static string GetString(Dictionary<string, object> dict, string key)
    {
        if (dict == null || !dict.ContainsKey(key) || dict[key] == null)
        {
            return null;
        }

        return dict[key].ToString();
    }

    private static Dictionary<string, object> ToVector3(Vector3 value)
    {
        return new Dictionary<string, object>
        {
            ["x"] = value.x,
            ["y"] = value.y,
            ["z"] = value.z,
        };
    }
}
