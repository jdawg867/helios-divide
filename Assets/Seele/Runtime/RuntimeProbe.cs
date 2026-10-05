using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RuntimeProbe
{
    public static object QueryObjectExists(string target)
    {
        try
        {
            var go = FindTarget(target);
            return Success("object_exists", target, new Dictionary<string, object>
            {
                ["exists"] = go != null,
                ["name"] = go != null ? go.name : target,
                ["path"] = go != null ? GetGameObjectPath(go) : null,
                ["activeSelf"] = go != null && go.activeSelf,
                ["activeInHierarchy"] = go != null && go.activeInHierarchy,
                ["scene"] = go != null ? go.scene.name : null,
                ["position"] = go != null ? ToVector3(go.transform.position) : null,
                ["rotation"] = go != null ? ToVector3(go.transform.eulerAngles) : null,
                ["childCount"] = go != null ? go.transform.childCount : 0,
            });
        }
        catch (Exception ex)
        {
            return Error("object_exists", target, ex.Message);
        }
    }

    public static object QueryComponentExists(string target, string componentName)
    {
        try
        {
            var go = FindTarget(target);
            if (go == null)
            {
                return Error("component_exists", target, $"GameObject not found: {target}");
            }

            var component = FindComponent(go, componentName);
            return Success("component_exists", target, new Dictionary<string, object>
            {
                ["exists"] = component != null,
                ["component"] = componentName,
                ["componentType"] = component != null ? component.GetType().FullName : null,
                ["gameObject"] = go.name,
                ["path"] = GetGameObjectPath(go),
            });
        }
        catch (Exception ex)
        {
            return Error("component_exists", target, ex.Message, componentName);
        }
    }

    public static object QueryComponentField(string target, string componentName, string fieldName)
    {
        try
        {
            var go = FindTarget(target);
            if (go == null)
            {
                return Error("component_field", target, $"GameObject not found: {target}", componentName, fieldName);
            }

            var component = FindComponent(go, componentName);
            if (component == null)
            {
                return Error("component_field", target, $"Component not found: {componentName}", componentName, fieldName);
            }

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = component.GetType();
            var field = type.GetField(fieldName, flags);
            if (field != null)
            {
                return Success("component_field", target, new Dictionary<string, object>
                {
                    ["component"] = componentName,
                    ["field"] = fieldName,
                    ["value"] = NormalizeValue(field.GetValue(component)),
                    ["valueType"] = field.FieldType.FullName,
                    ["memberKind"] = "field",
                    ["gameObject"] = go.name,
                    ["path"] = GetGameObjectPath(go),
                });
            }

            var prop = type.GetProperty(fieldName, flags);
            if (prop != null && prop.GetIndexParameters().Length == 0)
            {
                return Success("component_field", target, new Dictionary<string, object>
                {
                    ["component"] = componentName,
                    ["field"] = fieldName,
                    ["value"] = NormalizeValue(prop.GetValue(component)),
                    ["valueType"] = prop.PropertyType.FullName,
                    ["memberKind"] = "property",
                    ["gameObject"] = go.name,
                    ["path"] = GetGameObjectPath(go),
                });
            }

            return Error("component_field", target, $"Field or property not found: {fieldName}", componentName, fieldName);
        }
        catch (Exception ex)
        {
            return Error("component_field", target, ex.Message, componentName, fieldName);
        }
    }

    public static object QueryUIState(string target)
    {
        try
        {
            var go = FindTarget(target);
            if (go == null)
            {
                return Error("ui_state", target, $"GameObject not found: {target}");
            }

            var data = new Dictionary<string, object>
            {
                ["gameObject"] = go.name,
                ["path"] = GetGameObjectPath(go),
                ["activeSelf"] = go.activeSelf,
                ["activeInHierarchy"] = go.activeInHierarchy,
                ["scene"] = go.scene.name,
            };

            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                data["canvasGroup"] = new Dictionary<string, object>
                {
                    ["alpha"] = canvasGroup.alpha,
                    ["interactable"] = canvasGroup.interactable,
                    ["blocksRaycasts"] = canvasGroup.blocksRaycasts,
                    ["ignoreParentGroups"] = canvasGroup.ignoreParentGroups,
                };
            }

            var text = go.GetComponent<Text>();
            if (text != null)
            {
                data["text"] = new Dictionary<string, object>
                {
                    ["value"] = text.text,
                    ["enabled"] = text.enabled,
                    ["color"] = ToColor(text.color),
                };
            }

            var tmpText = go.GetComponent<TMP_Text>();
            if (tmpText != null)
            {
                data["tmpText"] = new Dictionary<string, object>
                {
                    ["value"] = tmpText.text,
                    ["enabled"] = tmpText.enabled,
                    ["color"] = ToColor(tmpText.color),
                };
            }

            var slider = go.GetComponent<Slider>();
            if (slider != null)
            {
                data["slider"] = new Dictionary<string, object>
                {
                    ["value"] = slider.value,
                    ["minValue"] = slider.minValue,
                    ["maxValue"] = slider.maxValue,
                    ["normalizedValue"] = slider.normalizedValue,
                    ["interactable"] = slider.interactable,
                };
            }

            var toggle = go.GetComponent<Toggle>();
            if (toggle != null)
            {
                data["toggle"] = new Dictionary<string, object>
                {
                    ["isOn"] = toggle.isOn,
                    ["interactable"] = toggle.interactable,
                };
            }

            return Success("ui_state", target, data);
        }
        catch (Exception ex)
        {
            return Error("ui_state", target, ex.Message);
        }
    }

    private static GameObject FindTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return null;
        }

        var direct = GameObject.Find(target);
        if (direct != null)
        {
            return direct;
        }

        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            return null;
        }

        foreach (var root in activeScene.GetRootGameObjects())
        {
            var found = FindInChildren(root.transform, target);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindInChildren(Transform root, string target)
    {
        if (root.name == target || GetGameObjectPath(root.gameObject) == target)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            var found = FindInChildren(child, target);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Component FindComponent(GameObject go, string componentName)
    {
        if (go == null || string.IsNullOrWhiteSpace(componentName))
        {
            return null;
        }

        var exact = go.GetComponent(componentName);
        if (exact != null)
        {
            return exact;
        }

        return go.GetComponents<Component>()
            .FirstOrDefault(c => c != null &&
                (string.Equals(c.GetType().Name, componentName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(c.GetType().FullName, componentName, StringComparison.OrdinalIgnoreCase)));
    }

    private static Dictionary<string, object> Success(string queryType, string target, Dictionary<string, object> data)
    {
        return new Dictionary<string, object>
        {
            ["ok"] = true,
            ["query_type"] = queryType,
            ["target"] = target,
            ["data"] = data,
            ["error"] = null,
        };
    }

    private static Dictionary<string, object> Error(string queryType, string target, string error, string component = null, string field = null)
    {
        var result = new Dictionary<string, object>
        {
            ["ok"] = false,
            ["query_type"] = queryType,
            ["target"] = target,
            ["data"] = null,
            ["error"] = error,
        };

        if (!string.IsNullOrWhiteSpace(component))
        {
            result["component"] = component;
        }

        if (!string.IsNullOrWhiteSpace(field))
        {
            result["field"] = field;
        }

        return result;
    }

    private static string GetGameObjectPath(GameObject go)
    {
        if (go == null)
        {
            return null;
        }

        var parts = new Stack<string>();
        var current = go.transform;
        while (current != null)
        {
            parts.Push(current.name);
            current = current.parent;
        }
        return string.Join("/", parts);
    }

    private static object NormalizeValue(object value)
    {
        if (value == null)
        {
            return null;
        }

        switch (value)
        {
            case string s:
                return s;
            case bool b:
                return b;
            case int i:
                return i;
            case long l:
                return l;
            case float f:
                return f;
            case double d:
                return d;
            case decimal m:
                return Convert.ToDouble(m, CultureInfo.InvariantCulture);
            case Enum e:
                return e.ToString();
            case Vector2 v2:
                return ToVector2(v2);
            case Vector3 v3:
                return ToVector3(v3);
            case Quaternion q:
                return new Dictionary<string, object>
                {
                    ["x"] = q.x,
                    ["y"] = q.y,
                    ["z"] = q.z,
                    ["w"] = q.w,
                };
            case Color c:
                return ToColor(c);
            case GameObject go:
                return new Dictionary<string, object>
                {
                    ["name"] = go.name,
                    ["path"] = GetGameObjectPath(go),
                    ["activeSelf"] = go.activeSelf,
                    ["activeInHierarchy"] = go.activeInHierarchy,
                };
            case Component component:
                return new Dictionary<string, object>
                {
                    ["type"] = component.GetType().FullName,
                    ["gameObject"] = component.gameObject.name,
                    ["path"] = GetGameObjectPath(component.gameObject),
                };
            case ICollection collection:
                var list = new List<object>();
                foreach (var item in collection)
                {
                    list.Add(NormalizeValue(item));
                }
                return list;
            default:
                var type = value.GetType();
                if (type.IsPrimitive)
                {
                    return value;
                }
                return value.ToString();
        }
    }

    private static Dictionary<string, object> ToVector2(Vector2 value)
    {
        return new Dictionary<string, object>
        {
            ["x"] = value.x,
            ["y"] = value.y,
        };
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

    private static Dictionary<string, object> ToColor(Color value)
    {
        return new Dictionary<string, object>
        {
            ["r"] = value.r,
            ["g"] = value.g,
            ["b"] = value.b,
            ["a"] = value.a,
        };
    }
}
