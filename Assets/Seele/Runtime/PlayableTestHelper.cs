using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayableTestHelper
{
    public static async Task<object> RunMoveSlice(string target, string axisName = "Horizontal", float axisValue = 1f, int holdFrames = 8, int settleFrames = 2, float minDistance = 0.01f)
    {
        try
        {
            var go = FindTarget(target);
            if (go == null)
            {
                return Failure("move_slice", target, $"GameObject not found: {target}");
            }

            var before = go.transform.position;
            await InputManager.Instance.SimulateAxis(axisName, axisValue, Mathf.Max(holdFrames, 1));
            await WaitFrames(Mathf.Max(settleFrames, 1));
            var after = go.transform.position;
            var assertion = RuntimeAssert.AssertPositionChanged(target, before, after, minDistance);

            return Success("move_slice", target, new Dictionary<string, object>
            {
                ["axisName"] = axisName,
                ["axisValue"] = axisValue,
                ["holdFrames"] = holdFrames,
                ["settleFrames"] = settleFrames,
                ["before"] = ToVector3(before),
                ["after"] = ToVector3(after),
                ["assertion"] = assertion,
            });
        }
        catch (Exception ex)
        {
            return Failure("move_slice", target, ex.Message);
        }
    }

    public static async Task<object> RunJumpSlice(string target, KeyCode key = KeyCode.Space, int pressFrames = 2, int observeFrames = 12, float minHeightGain = 0.05f)
    {
        try
        {
            var go = FindTarget(target);
            if (go == null)
            {
                return Failure("jump_slice", target, $"GameObject not found: {target}");
            }

            var before = go.transform.position;
            var peakY = before.y;

            await InputManager.Instance.SimulateKey(key, Mathf.Max(pressFrames, 1));
            for (var i = 0; i < Mathf.Max(observeFrames, 1); i++)
            {
                await Task.Yield();
                peakY = Mathf.Max(peakY, go.transform.position.y);
            }

            var after = go.transform.position;
            var heightGain = peakY - before.y;
            var assertion = RuntimeAssert.AssertTrue(
                "jump_height_gain",
                target,
                heightGain >= minHeightGain,
                new Dictionary<string, object>
                {
                    ["beforeY"] = before.y,
                    ["peakY"] = peakY,
                    ["afterY"] = after.y,
                    ["heightGain"] = heightGain,
                    ["minHeightGain"] = minHeightGain,
                },
                $"Expected height gain >= {minHeightGain}, got {heightGain}"
            );

            return Success("jump_slice", target, new Dictionary<string, object>
            {
                ["key"] = key.ToString(),
                ["pressFrames"] = pressFrames,
                ["observeFrames"] = observeFrames,
                ["before"] = ToVector3(before),
                ["after"] = ToVector3(after),
                ["peakY"] = peakY,
                ["heightGain"] = heightGain,
                ["assertion"] = assertion,
            });
        }
        catch (Exception ex)
        {
            return Failure("jump_slice", target, ex.Message);
        }
    }

    public static async Task<object> RunButtonSlice(string buttonName, int holdFrames = 2)
    {
        try
        {
            var before = InputManager.GetButton(buttonName);
            var task = InputManager.Instance.SimulateButton(buttonName, Mathf.Max(holdFrames, 1));
            await Task.Yield();
            var during = InputManager.GetButton(buttonName);
            await task;
            var after = InputManager.GetButton(buttonName);
            var assertion = RuntimeAssert.AssertTrue(
                "button_pressed",
                buttonName,
                !before && during,
                new Dictionary<string, object>
                {
                    ["before"] = before,
                    ["during"] = during,
                    ["after"] = after,
                },
                $"Expected button '{buttonName}' to become true during simulation."
            );

            return Success("button_slice", buttonName, new Dictionary<string, object>
            {
                ["buttonName"] = buttonName,
                ["holdFrames"] = holdFrames,
                ["before"] = before,
                ["during"] = during,
                ["after"] = after,
                ["assertion"] = assertion,
            });
        }
        catch (Exception ex)
        {
            return Failure("button_slice", buttonName, ex.Message);
        }
    }

    public static async Task<object> RunAttackSlice(string actionName, string buttonName, string targetObject, string targetComponent, string fieldName, float expectedDelta, int holdFrames = 2, int observeFrames = 12)
    {
        try
        {
            var beforeResult = RuntimeProbe.QueryComponentField(targetObject, targetComponent, fieldName) as Dictionary<string, object>;
            var before = GetNumericFieldValue(beforeResult);
            if (!before.HasValue)
            {
                return Failure("attack_slice", actionName, $"Cannot read numeric field {targetComponent}.{fieldName} on {targetObject}");
            }

            await InputManager.Instance.SimulateButton(buttonName, Mathf.Max(holdFrames, 1));
            await WaitFrames(Mathf.Max(observeFrames, 1));

            var afterResult = RuntimeProbe.QueryComponentField(targetObject, targetComponent, fieldName) as Dictionary<string, object>;
            var after = GetNumericFieldValue(afterResult);
            if (!after.HasValue)
            {
                return Failure("attack_slice", actionName, $"Cannot read numeric field after action: {targetComponent}.{fieldName} on {targetObject}");
            }

            var delta = before.Value - after.Value;
            var assertion = RuntimeAssert.AssertTrue(
                "attack_effect",
                actionName,
                delta >= expectedDelta,
                new Dictionary<string, object>
                {
                    ["targetObject"] = targetObject,
                    ["component"] = targetComponent,
                    ["field"] = fieldName,
                    ["before"] = before.Value,
                    ["after"] = after.Value,
                    ["delta"] = delta,
                    ["expectedDelta"] = expectedDelta,
                },
                $"Expected attack effect delta >= {expectedDelta}, got {delta}"
            );

            return Success("attack_slice", actionName, new Dictionary<string, object>
            {
                ["buttonName"] = buttonName,
                ["targetObject"] = targetObject,
                ["component"] = targetComponent,
                ["field"] = fieldName,
                ["before"] = before.Value,
                ["after"] = after.Value,
                ["delta"] = delta,
                ["assertion"] = assertion,
            });
        }
        catch (Exception ex)
        {
            return Failure("attack_slice", actionName, ex.Message);
        }
    }

    public static async Task<object> RunInteractSlice(string actionName, string buttonName, string targetObject, string targetComponent, string fieldName, object expectedAfter, int holdFrames = 2, int observeFrames = 12)
    {
        try
        {
            var beforeResult = RuntimeProbe.QueryComponentField(targetObject, targetComponent, fieldName) as Dictionary<string, object>;
            var before = GetFieldValue(beforeResult);

            await InputManager.Instance.SimulateButton(buttonName, Mathf.Max(holdFrames, 1));
            await WaitFrames(Mathf.Max(observeFrames, 1));

            var afterResult = RuntimeProbe.QueryComponentField(targetObject, targetComponent, fieldName) as Dictionary<string, object>;
            var after = GetFieldValue(afterResult);
            var assertion = RuntimeAssert.AssertTrue(
                "interact_effect",
                actionName,
                ValuesEqual(after, expectedAfter),
                new Dictionary<string, object>
                {
                    ["targetObject"] = targetObject,
                    ["component"] = targetComponent,
                    ["field"] = fieldName,
                    ["before"] = before,
                    ["after"] = after,
                    ["expectedAfter"] = expectedAfter,
                },
                $"Expected interaction result {fieldName} == {expectedAfter}, got {after}"
            );

            return Success("interact_slice", actionName, new Dictionary<string, object>
            {
                ["buttonName"] = buttonName,
                ["targetObject"] = targetObject,
                ["component"] = targetComponent,
                ["field"] = fieldName,
                ["before"] = before,
                ["after"] = after,
                ["expectedAfter"] = expectedAfter,
                ["assertion"] = assertion,
            });
        }
        catch (Exception ex)
        {
            return Failure("interact_slice", actionName, ex.Message);
        }
    }

    public static async Task<object> RunUiFeedbackSlice(string actionName, string buttonName, string uiTarget, string expectedSubstring, int holdFrames = 2, int observeFrames = 12)
    {
        try
        {
            var beforeState = RuntimeProbe.QueryUIState(uiTarget) as Dictionary<string, object>;
            var beforeText = ExtractUiText(beforeState);

            if (!string.IsNullOrWhiteSpace(buttonName))
            {
                await InputManager.Instance.SimulateButton(buttonName, Mathf.Max(holdFrames, 1));
            }
            await WaitFrames(Mathf.Max(observeFrames, 1));

            var afterState = RuntimeProbe.QueryUIState(uiTarget) as Dictionary<string, object>;
            var afterText = ExtractUiText(afterState);
            var assertion = RuntimeAssert.AssertUiTextContains(uiTarget, expectedSubstring);

            return Success("ui_feedback_slice", actionName, new Dictionary<string, object>
            {
                ["buttonName"] = buttonName,
                ["uiTarget"] = uiTarget,
                ["beforeText"] = beforeText,
                ["afterText"] = afterText,
                ["expectedSubstring"] = expectedSubstring,
                ["assertion"] = assertion,
            });
        }
        catch (Exception ex)
        {
            return Failure("ui_feedback_slice", actionName, ex.Message);
        }
    }

    private static async Task WaitFrames(int frameCount)
    {
        for (var i = 0; i < frameCount; i++)
        {
            await Task.Yield();
        }
    }

    private static object GetFieldValue(Dictionary<string, object> queryResult)
    {
        if (queryResult == null || !queryResult.ContainsKey("data"))
        {
            return null;
        }

        var data = queryResult["data"] as Dictionary<string, object>;
        if (data == null || !data.ContainsKey("value"))
        {
            return null;
        }

        return data["value"];
    }

    private static double? GetNumericFieldValue(Dictionary<string, object> queryResult)
    {
        var value = GetFieldValue(queryResult);
        if (value == null)
        {
            return null;
        }

        try
        {
            return Convert.ToDouble(value);
        }
        catch
        {
            return null;
        }
    }

    private static bool ValuesEqual(object left, object right)
    {
        if (left == null && right == null)
        {
            return true;
        }

        if (left == null || right == null)
        {
            return false;
        }

        if (left is IConvertible && right is IConvertible)
        {
            try
            {
                return Math.Abs(Convert.ToDouble(left) - Convert.ToDouble(right)) < 0.0001d;
            }
            catch
            {
                // Fall through.
            }
        }

        return Equals(left, right);
    }

    private static string ExtractUiText(Dictionary<string, object> queryResult)
    {
        if (queryResult == null || !queryResult.ContainsKey("data"))
        {
            return null;
        }

        var data = queryResult["data"] as Dictionary<string, object>;
        if (data == null)
        {
            return null;
        }

        var text = ExtractUiTextFromBlock(data, "text");
        if (!string.IsNullOrEmpty(text))
        {
            return text;
        }

        return ExtractUiTextFromBlock(data, "tmpText");
    }

    private static string ExtractUiTextFromBlock(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key) || data[key] == null)
        {
            return null;
        }

        var block = data[key] as Dictionary<string, object>;
        if (block == null || !block.ContainsKey("value") || block["value"] == null)
        {
            return null;
        }

        return block["value"].ToString();
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

    private static Dictionary<string, object> Success(string sliceType, string target, Dictionary<string, object> data)
    {
        return new Dictionary<string, object>
        {
            ["ok"] = true,
            ["slice_type"] = sliceType,
            ["target"] = target,
            ["data"] = data,
            ["error"] = null,
        };
    }

    private static Dictionary<string, object> Failure(string sliceType, string target, string error)
    {
        return new Dictionary<string, object>
        {
            ["ok"] = false,
            ["slice_type"] = sliceType,
            ["target"] = target,
            ["data"] = null,
            ["error"] = error,
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
}
