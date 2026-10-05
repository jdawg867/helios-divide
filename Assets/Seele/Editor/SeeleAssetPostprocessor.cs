using UnityEditor;
using System.IO;

namespace Seele.Editor
{
    public class SeeleAssetPostprocessor : UnityEditor.AssetPostprocessor {
        public void OnPreprocessModel()
        {
            ModelImporter mi = assetImporter as ModelImporter;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            string texture_Extract_path = $"{Path.GetDirectoryName(assetPath)}";
            mi.ExtractTextures(texture_Extract_path);
        }
    }
}