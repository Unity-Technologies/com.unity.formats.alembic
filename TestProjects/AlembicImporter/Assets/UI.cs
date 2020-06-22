using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

namespace System.Runtime.InteropServices
{
    public static class UI
    {
        [MenuItem("Window/AutoAssign")]
        static void Menu()
        {
            var go = Selection.activeObject as GameObject;
            var player = go.GetComponent<AlembicStreamPlayer>();
            if (!go || !player)
                return;
            var materials = player.GetMaterialNames();
            foreach (var material in materials)
            {
                var renderer = material.Key.GetComponent<MeshRenderer>();
                var mats = new Material[renderer.sharedMaterials.Length];
                for (var i = 0; i < renderer.sharedMaterials.Length; ++i)
                {
                    mats[i] = FindFirstMaterialByName(material.Value[i]);
                }

                renderer.sharedMaterials = mats;
            }
        }

        static Material FindFirstMaterialByName(string name)
        {
            var paths = AssetDatabase.FindAssets(name).Select(AssetDatabase.GUIDToAssetPath);
            return paths.Select(AssetDatabase.LoadAssetAtPath<Material>).FirstOrDefault(x => x != null);
        }
    }
}
