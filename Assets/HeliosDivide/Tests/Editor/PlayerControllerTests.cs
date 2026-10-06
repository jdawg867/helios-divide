using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using HeliosDivide.Player;

namespace HeliosDivide.Tests.Editor
{
    public sealed class PlayerControllerTests
    {
        const string PrefabPath = "Assets/HeliosDivide/Player/HDPlayer.prefab";
        const string ScenePath = "Assets/HeliosDivide/World/FoundationTest.unity";
        const string InputPath = "Assets/HeliosDivide/Core/HeliosDivideInput.inputactions";

        [Test]
        public void PrefabHasRequiredReusableComponents()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<CharacterController>());
            Assert.IsNotNull(prefab.GetComponent<HDPlayerInput>());
            Assert.IsNotNull(prefab.GetComponent<HDPlayerLocomotion>());
            Assert.IsNotNull(prefab.GetComponent<HDGroundedState>());
            Assert.IsNotNull(prefab.GetComponent<HDActiveDevice>());
            Assert.IsNotNull(prefab.GetComponent<HDDevelopmentLook>().CameraPivot);
            Assert.AreEqual(1, prefab.GetComponentsInChildren<Camera>().Length);
            Assert.AreEqual(InputPath, AssetDatabase.GetAssetPath(prefab.GetComponent<HDPlayerInput>().SourceAsset));
            Assert.AreEqual(1.8f, prefab.GetComponent<CharacterController>().height, 0.001f);
        }

        [Test]
        public void ActionsAndPhysicalControllerBindingsArePreserved()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            Assert.IsNotNull(asset);
            var names = new[] { "Move", "Look", "Jump", "Sprint", "Crouch", "Interact", "Fire", "Aim", "Reload", "NextWeapon", "PreviousWeapon", "TogglePerspective", "Inventory", "Pause" };
            foreach (var name in names) Assert.IsNotNull(asset.FindAction(name, true));
            var bindings = new[] {
                ("Move", "<Gamepad>/leftStick"), ("Look", "<Gamepad>/rightStick"),
                ("Jump", "<Gamepad>/buttonSouth"), ("Crouch", "<Gamepad>/buttonEast"),
                ("Sprint", "<Gamepad>/leftStickPress"), ("Interact", "<Gamepad>/buttonWest"),
                ("Reload", "<Gamepad>/buttonNorth"), ("Aim", "<Gamepad>/leftTrigger"),
                ("Fire", "<Gamepad>/rightTrigger"), ("PreviousWeapon", "<Gamepad>/leftShoulder"),
                ("NextWeapon", "<Gamepad>/rightShoulder"), ("TogglePerspective", "<Gamepad>/dpad/down"),
                ("Inventory", "<Gamepad>/dpad/up"), ("Pause", "<Gamepad>/start") };
            foreach (var binding in bindings)
                Assert.IsTrue(asset.FindAction(binding.Item1, true).bindings.Any(b => b.path == binding.Item2), binding.Item1);
            InputActionAsset bound;
            Assert.IsTrue(EditorBuildSettings.TryGetConfigObject("com.unity.input.settings.actions", out bound));
            Assert.AreSame(asset, bound);
        }

        [Test]
        public void DeadzonesPersistWithoutDoubleProcessing()
        {
            InputSettings settings;
            Assert.IsTrue(EditorBuildSettings.TryGetConfigObject("com.unity.input.settings", out settings));
            Assert.AreEqual(0.15f, settings.defaultDeadzoneMin, 0.001f);
            Assert.AreEqual(0.95f, settings.defaultDeadzoneMax, 0.001f);
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            foreach (var binding in asset.bindings.Where(b => b.path.EndsWith("Stick")))
                Assert.IsEmpty(binding.processors, "Stick controls already have stickDeadzone; do not apply it twice.");
        }

        [Test]
        public void FoundationReferencesOneLinkedPlayerAndWalkableSpawn()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var players = transforms.Select(t => t.GetComponent<HDPlayerLocomotion>()).Where(p => p != null).ToArray();
            Assert.AreEqual(1, players.Length);
            Assert.AreEqual(PrefabPath, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(players[0].gameObject));
            var spawn = transforms.Single(t => t.name == "PlayerSpawn");
            Assert.AreEqual(spawn.position.x, players[0].transform.position.x, 0.001f);
            Assert.AreEqual(spawn.position.z, players[0].transform.position.z, 0.001f);
            RaycastHit hit;
            Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(players[0].transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore));
            Assert.AreEqual("Floor", hit.collider.name);
            Assert.AreEqual(0, transforms.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
            Assert.AreEqual(1, transforms.Select(t => t.GetComponent<Camera>()).Count(c => c != null && c.isActiveAndEnabled));
        }
    }
}
