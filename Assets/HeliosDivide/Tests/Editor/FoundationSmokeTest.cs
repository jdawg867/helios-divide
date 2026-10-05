using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace HeliosDivide.Tests.Editor
{
    public sealed class FoundationSmokeTest
    {
        const string FoundationScenePath = "Assets/HeliosDivide/World/FoundationTest.unity";
        const string InputAssetPath = "Assets/HeliosDivide/Core/HeliosDivideInput.inputactions";

        [Test]
        public void FoundationTestSceneExists()
        {
            Assert.IsTrue(File.Exists(FoundationScenePath), "FoundationTest scene is missing.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(FoundationScenePath));
        }

        [Test]
        public void InputActionAssetExists()
        {
            Assert.IsTrue(File.Exists(InputAssetPath), "HeliosDivideInput asset is missing.");
        }
    }
}
