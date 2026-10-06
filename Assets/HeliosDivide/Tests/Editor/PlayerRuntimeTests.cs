using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using HeliosDivide.Player;

namespace HeliosDivide.Tests.Editor
{
    public sealed class PlayerRuntimeTests
    {
        [UnityTest]
        public IEnumerator LocomotionAndMultiDeviceContract()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/HeliosDivide/World/FoundationTest.unity");
            yield return new EnterPlayMode();
            var result = RunInPlayMode();
            Debug.Log("[HeliosDivide tests] " + result);
            yield return new ExitPlayMode();
        }

        public static string RunInPlayMode()
        {
            Assert.IsTrue(Application.isPlaying);
            var motor = UnityEngine.Object.FindFirstObjectByType<HDPlayerLocomotion>();
            Assert.IsNotNull(motor);
            var look = motor.GetComponent<HDDevelopmentLook>();
            var input = motor.GetComponent<HDPlayerInput>();
            var controller = motor.GetComponent<CharacterController>();
            var originalPosition = motor.transform.position;
            var originalRotation = motor.transform.rotation;
            var motorEnabled = motor.enabled;
            var lookEnabled = look.enabled;
            var originalSettings = InputSystem.settings;
            var testSettings = UnityEngine.Object.Instantiate(originalSettings);
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = testSettings;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var results = new List<object>();
            motor.enabled = false;
            look.enabled = false;
            Action neutral = () => {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                InputSystem.Update();
            };
            Action<Vector3, float> reset = (position, yaw) => {
                neutral();
                controller.enabled = false;
                motor.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
                controller.enabled = true;
                Physics.SyncTransforms();
                for (var i = 0; i < 40; i++) motor.Tick(0.02f);
            };
            Action<KeyboardState> keys = state => { InputSystem.QueueStateEvent(keyboard, state); InputSystem.Update(); };
            Action<GamepadState> pad = state => { InputSystem.QueueStateEvent(gamepad, state); InputSystem.Update(); };
            Action<int> advance = frames => { for (var i = 0; i < frames; i++) motor.Tick(0.02f); };
            try
            {
                reset(new Vector3(-12f, 0.23f, -10f), 0f);
                Assert.IsTrue(motor.IsGrounded);
                Assert.GreaterOrEqual(motor.transform.position.y, -0.05f);
                var settledY = motor.transform.position.y;
                advance(250);
                Assert.AreEqual(settledY, motor.transform.position.y, 0.03f);
                Assert.GreaterOrEqual(motor.VerticalSpeed, -2.5f);
                results.Add(new { test = "spawn/grounded stability", feetY = motor.transform.position.y, pass = true });

                keys(new KeyboardState(Key.W)); advance(30);
                Assert.AreEqual(4.5f, motor.HorizontalVelocity.magnitude, 0.05f);
                var walk = motor.HorizontalVelocity.magnitude;
                keys(new KeyboardState(Key.W, Key.D)); advance(20);
                Assert.AreEqual(walk, motor.HorizontalVelocity.magnitude, 0.05f);
                Assert.AreEqual(HDInputDeviceCategory.KeyboardMouse, input.ActiveDevice.Category);
                results.Add(new { test = "WASD/diagonal cap", speed = motor.HorizontalVelocity.magnitude, pass = true });

                keys(new KeyboardState(Key.W, Key.LeftShift)); advance(25);
                Assert.IsTrue(motor.IsSprinting); Assert.AreEqual(7f, motor.HorizontalVelocity.magnitude, 0.05f);
                keys(new KeyboardState(Key.W)); advance(25);
                Assert.IsFalse(motor.IsSprinting); Assert.AreEqual(4.5f, motor.HorizontalVelocity.magnitude, 0.05f);
                keys(new KeyboardState(Key.LeftShift)); advance(30);
                Assert.IsFalse(motor.IsSprinting); Assert.Less(motor.HorizontalVelocity.magnitude, 0.01f);
                results.Add(new { test = "hold sprint/release/no idle sprint", pass = true });

                reset(new Vector3(-12f, 0.23f, -10f), 0f);
                pad(new GamepadState { leftStick = new Vector2(0f, 0.55f) }); advance(30);
                Assert.AreEqual(0.5f, input.Move.y, 0.02f);
                Assert.AreEqual(2.25f, motor.HorizontalVelocity.magnitude, 0.1f);
                Assert.AreEqual(HDInputDeviceCategory.Gamepad, input.ActiveDevice.Category);
                pad(new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.LeftStick)); advance(25);
                Assert.AreEqual(7f, motor.HorizontalVelocity.magnitude, 0.05f);
                pad(new GamepadState { leftStick = Vector2.up }); advance(25);
                Assert.IsFalse(motor.IsSprinting);
                neutral(); keys(new KeyboardState(Key.W)); advance(20);
                Assert.AreEqual(HDInputDeviceCategory.KeyboardMouse, input.ActiveDevice.Category);
                results.Add(new { test = "analog magnitude/gamepad sprint/live switching", pass = true });

                reset(new Vector3(-12f, 0.23f, -10f), 0f);
                pad(new GamepadState { leftStick = new Vector2(0.05f, 0f), rightStick = new Vector2(0.08f, 0f) });
                Assert.AreEqual(Vector2.zero, input.Move); Assert.AreEqual(Vector2.zero, input.Look);
                advance(30); Assert.Less(motor.HorizontalVelocity.magnitude, 0.01f);
                results.Add(new { test = "deadzone/no movement or look drift", pass = true });

                reset(new Vector3(-12f, 0.23f, -10f), 0f);
                var jumpStart = motor.transform.position.y;
                keys(new KeyboardState(Key.Space)); motor.Tick(0.02f);
                Assert.Greater(motor.VerticalSpeed, 0f); Assert.IsFalse(motor.IsGrounded);
                var firstSpeed = motor.VerticalSpeed;
                keys(new KeyboardState()); keys(new KeyboardState(Key.Space)); motor.Tick(0.02f);
                Assert.Less(motor.VerticalSpeed, firstSpeed, "Airborne jump must not restart the impulse.");
                var highest = motor.transform.position.y;
                for (var i = 0; i < 80; i++) { motor.Tick(0.02f); highest = Mathf.Max(highest, motor.transform.position.y); }
                Assert.AreEqual(1.2f, highest - jumpStart, 0.15f); Assert.IsTrue(motor.IsGrounded);
                neutral(); pad(new GamepadState().WithButton(GamepadButton.South)); motor.Tick(0.02f);
                Assert.Greater(motor.VerticalSpeed, 0f);
                neutral(); advance(90);
                results.Add(new { test = "grounded jump/no double jump/south button/landing", apexMetres = highest - jumpStart, pass = true });

                reset(new Vector3(-12f, 0.23f, -10f), 0f);
                var feet = motor.transform.position.y;
                keys(new KeyboardState(Key.LeftCtrl, Key.W)); advance(25);
                Assert.IsTrue(motor.IsCrouching); Assert.AreEqual(1.1f, controller.height, 0.001f);
                Assert.AreEqual(2.5f, motor.HorizontalVelocity.magnitude, 0.05f);
                neutral(); advance(20); Assert.IsFalse(motor.IsCrouching);
                keys(new KeyboardState(Key.C)); advance(1); Assert.IsTrue(motor.IsCrouching);
                neutral(); advance(1);
                pad(new GamepadState().WithButton(GamepadButton.East)); advance(1); Assert.IsTrue(motor.IsCrouching);
                var ceiling = new GameObject("TemporaryStandingObstruction");
                var ceilingCollider = ceiling.AddComponent<BoxCollider>();
                ceiling.transform.position = motor.transform.position + Vector3.up * 1.48f;
                ceilingCollider.size = new Vector3(2f, 0.2f, 2f);
                Physics.SyncTransforms();
                try
                {
                    neutral(); advance(15);
                    Assert.IsTrue(motor.IsCrouching); Assert.IsFalse(motor.CanStand());
                    Assert.AreEqual(1.1f, controller.height, 0.001f);
                }
                finally { UnityEngine.Object.DestroyImmediate(ceiling); Physics.SyncTransforms(); }
                advance(15); Assert.IsFalse(motor.IsCrouching); Assert.AreEqual(1.8f, controller.height, 0.001f);
                Assert.AreEqual(feet, motor.transform.position.y, 0.03f);
                results.Add(new { test = "Ctrl/C/east button/crouch speed/blocked standing/feet preservation", pass = true });

                neutral();
                motor.transform.rotation = Quaternion.identity;
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(100f, 0f) }); InputSystem.Update();
                Assert.IsFalse(input.LookIsRate); look.Tick(0.02f); var mouseYaw = motor.transform.eulerAngles.y;
                motor.transform.rotation = Quaternion.identity; look.Tick(0.1f);
                Assert.AreEqual(mouseYaw, motor.transform.eulerAngles.y, 0.01f); Assert.AreEqual(12f, mouseYaw, 0.1f);
                neutral(); pad(new GamepadState { rightStick = Vector2.right });
                motor.transform.rotation = Quaternion.identity; Assert.IsTrue(input.LookIsRate); look.Tick(0.02f);
                Assert.AreEqual(3f, motor.transform.eulerAngles.y, 0.05f);
                motor.transform.rotation = Quaternion.identity; look.Tick(0.1f);
                Assert.AreEqual(15f, motor.transform.eulerAngles.y, 0.05f);
                pad(new GamepadState { rightStick = Vector2.up }); for (var i = 0; i < 100; i++) look.Tick(0.02f);
                Assert.AreEqual(-85f, look.Pitch, 0.01f);
                neutral(); InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(0f, -2000f) }); InputSystem.Update(); look.Tick(0.02f);
                Assert.AreEqual(HDInputDeviceCategory.KeyboardMouse, input.ActiveDevice.Category);
                Assert.AreEqual(85f, look.Pitch, 0.01f);
                results.Add(new { test = "mouse delta vs stick rate/pitch clamp/mouse-stick-mouse switching", pass = true });

                reset(new Vector3(0f, 0.23f, -12f), 0f);
                keys(new KeyboardState(Key.W)); advance(340);
                Assert.Less(motor.transform.position.z, 13.85f);
                Assert.Greater(motor.transform.position.z, 13f);
                results.Add(new { test = "perimeter wall collision", z = motor.transform.position.z, pass = true });
                reset(new Vector3(-7f, 0.23f, -7f), 0f);
                keys(new KeyboardState(Key.W)); advance(65);
                Assert.Less(motor.transform.position.z, -4.9f);
                results.Add(new { test = "obstacle collision", z = motor.transform.position.z, pass = true });

                reset(new Vector3(-2f, 0.23f, 7.3f), 90f);
                keys(new KeyboardState(Key.W)); advance(130);
                Assert.Greater(motor.transform.position.x, 7f);
                Assert.Greater(motor.transform.position.y, 1.9f); Assert.IsTrue(motor.IsGrounded);
                results.Add(new { test = "ramp ascent and platform", x = motor.transform.position.x, y = motor.transform.position.y, pass = true });
                keys(new KeyboardState(Key.S)); advance(140);
                Assert.Less(motor.transform.position.x, 0f); Assert.Less(motor.transform.position.y, 0.15f);
                results.Add(new { test = "ramp descent", pass = true });

                reset(new Vector3(10f, 0.23f, -1f), 0f);
                keys(new KeyboardState(Key.W)); advance(90);
                Assert.Greater(motor.transform.position.z, 5.8f);
                Assert.Greater(motor.transform.position.y, 1.9f); Assert.IsTrue(motor.IsGrounded);
                results.Add(new { test = "four stairs ascent", z = motor.transform.position.z, y = motor.transform.position.y, pass = true });
                keys(new KeyboardState(Key.S)); advance(115);
                neutral(); advance(40);
                Assert.Less(motor.transform.position.z, 0f, "Descend beyond the first stair.");
                Assert.Less(motor.transform.position.y, 0.15f, "Land on the floor after descending the stairs.");
                results.Add(new { test = "stairs descent and landing", pass = true });
                return "pass=True groupsPassed=" + results.Count + " evidence=" + string.Join("; ", results);
            }
            finally
            {
                neutral();
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(gamepad);
                InputSystem.settings = originalSettings;
                UnityEngine.Object.DestroyImmediate(testSettings);
                controller.enabled = false; motor.transform.SetPositionAndRotation(originalPosition, originalRotation); controller.enabled = true;
                Physics.SyncTransforms();
                motor.enabled = motorEnabled; look.enabled = lookEnabled;
            }
        }
    }
}
