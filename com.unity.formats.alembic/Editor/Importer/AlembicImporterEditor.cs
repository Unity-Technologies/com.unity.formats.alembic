#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;
using Object = System.Object;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
using UnityEngine.Formats.Alembic.Importer;

#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityEditor.Formats.Alembic.Importer
{
    [CustomEditor(typeof(AlembicImporter)), CanEditMultipleObjects]
    internal class AlembicImporterEditor : ScriptedImporterEditor
    {
        enum UITab
        {
            Model,
            Material
        };

        enum MaterialSearchLocation
        {
            ProjectWide,
            CurrentFolder
        }

        struct MaterialEntry
        {
            public AlembicCustomData component;
            public string path;
            public int index;
            public Material material;

            public AssetImporter.SourceAssetIdentifier ToSourceAssetIdentifier()
            {
                return new AssetImporter.SourceAssetIdentifier(typeof(Material), path + $":{index}");
            }

            public static MaterialEntry FromSourceAssetIdentifier(AssetImporter.SourceAssetIdentifier identifier, GameObject rootGO)
            {
                var toks = identifier.name.Split(':');
                var go = AlembicImporter.GetGameObjectFromPath(rootGO, toks[0]);
                return new MaterialEntry
                {
                    component = go.GetComponent<AlembicCustomData>(),
                    path = toks[0],
                    index = int.Parse(toks[1])
                };
            }
        }

        UITab uiTab;
        MaterialSearchLocation materialSearchLocation;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var importer = serializedObject.targetObject as AlembicImporter;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                uiTab = (UITab)GUILayout.Toolbar((int)uiTab, new[] {"Model", "Material"});
                GUILayout.FlexibleSpace();
            }

            if (uiTab == UITab.Model)
            {
                DrawModelUI(importer);
            }
            else
            {
                DrawMaterialUI(importer);
            }


            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        void DrawModelUI(AlembicImporter importer)
        {
            const string pathSettings = "streamSettings.";
            if (importer.IsHDF5)
            {
                EditorGUILayout.HelpBox("Unsupported HDF5 file format detected. Please convert to Ogawa.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "scaleFactor"),
                    new GUIContent("Scale Factor", "How much to scale the models compared to what is in the source file."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "swapHandedness"),
                    new GUIContent("Swap Handedness", "Swap X coordinate"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "interpolateSamples"),
                    new GUIContent("Interpolate Samples", "Interpolate transforms and vertices (if topology is constant)."));
                EditorGUILayout.Separator();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importVisibility"),
                    new GUIContent("Import Visibility", "Import visibility animation."));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importXform"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importCameras"),
                    new GUIContent("Import Cameras", ""));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importMeshes"),
                    new GUIContent("Import Meshes", ""));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importPoints"),
                    new GUIContent("Import Points", ""));
                var importCurvesProp = serializedObject.FindProperty(pathSettings + "importCurves");
                EditorGUILayout.PropertyField(importCurvesProp, new GUIContent("Import Curves", ""));
                if (importCurvesProp.boolValue == true)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "createCurveRenderers"),
                        new GUIContent("Add Curve Renderers", "Automatically add 'AlembicCurvesRenderer' components on curve objects.\nThis allows you to get a basic preview of the Alembic curves in the Scene."));
                }
                EditorGUILayout.Separator();

                // time range
                var startTimeProp = serializedObject.FindProperty("startTime");
                var endTimeProp = serializedObject.FindProperty("endTime");

                EditorGUI.BeginDisabledGroup(startTimeProp.hasMultipleDifferentValues || endTimeProp.hasMultipleDifferentValues);
                var startTime = (float)startTimeProp.doubleValue;
                var endTime = (float)endTimeProp.doubleValue;
                var abcStart = (float)importer.AbcStartTime;
                var abcEnd = (float)importer.AbcEndTime;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider("Time Range", ref startTime, ref endTime, abcStart, abcEnd);

                if (EditorGUI.EndChangeCheck())
                {
                    startTimeProp.doubleValue = startTime;
                    endTime = (float)Math.Round(endTime, 5);
                    endTimeProp.doubleValue = endTime;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = startTimeProp.hasMultipleDifferentValues;
                var newStartTime = EditorGUILayout.FloatField(new GUIContent(" ", "Start time"), startTime, GUILayout.MinWidth(90.0f));
                EditorGUI.showMixedValue = endTimeProp.hasMultipleDifferentValues;
                var newEndTime = EditorGUILayout.FloatField(new GUIContent(" ", "End time"), endTime, GUILayout.MinWidth(90.0f));
                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    startTimeProp.doubleValue = newStartTime;
                    endTimeProp.doubleValue = newEndTime;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();


                GUIStyle style = new GUIStyle();
                style.alignment = TextAnchor.LowerRight;
                style.normal.textColor = Color.gray;
                if (!startTimeProp.hasMultipleDifferentValues && !endTimeProp.hasMultipleDifferentValues)
                {
                    EditorGUILayout.LabelField(new GUIContent((endTime - startTime).ToString("0.000") + "s"), style);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                DisplayEnumProperty(serializedObject.FindProperty(pathSettings + "normals"), Enum.GetNames(typeof(NormalsMode)));
                DisplayEnumProperty(serializedObject.FindProperty(pathSettings + "tangents"), Enum.GetNames(typeof(TangentsMode)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "flipFaces"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Cameras", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                DisplayEnumProperty(serializedObject.FindProperty(pathSettings + "cameraAspectRatio"), Enum.GetNames(typeof(AspectRatioMode)),
                    new GUIContent("Aspect Ratio", ""));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Separator();
        }

        void DrawMaterialUI(AlembicImporter importer)
        {
            var mainGO = AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);
            var materials = ConvertToMaterialEntry(importer.GetExternalObjectMap(), mainGO);

            EditorGUILayout.LabelField("Material");

            using (new EditorGUI.IndentLevelScope())
            {
                materialSearchLocation = (MaterialSearchLocation)EditorGUILayout.EnumPopup("Search Location", materialSearchLocation);
                if (GUILayout.Button("Existing Material"))
                {
                    SearchForMaterials(materials, importer);
                }
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Meshes");

                var drawnMeshes = new HashSet<AlembicCustomData>();
                foreach (var o in materials)
                {
                    if (drawnMeshes.Contains(o.component))
                        continue;
                    drawnMeshes.Add(o.component);

                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField(o.component.name);
                        for (var i = 0; i < o.component.FacesetNames.Count; ++i)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                using (var c = new EditorGUI.ChangeCheckScope())
                                {
                                    var assign = EditorGUILayout.ObjectField(o.component.FacesetNames[i], o.material,
                                        typeof(Material), false);
                                    if (c.changed)
                                    {
                                        if (AssetDatabase.GetAssetPath(assign) == importer.assetPath)
                                        {
                                            Debug.LogError(
                                                $"{assign.name} is a sub-asset of {Path.GetFileName(importer.assetPath)} and cannot be used as an external material.");
                                        }
                                        else
                                        {
                                            importer.AddRemap(o.ToSourceAssetIdentifier(), assign);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        List<MaterialEntry> ConvertToMaterialEntry(
            Dictionary<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> remap, GameObject rootGameObject)
        {
            var ret = new List<MaterialEntry>();
            foreach (var o in remap)
            {
                var entry = MaterialEntry.FromSourceAssetIdentifier(o.Key, rootGameObject);
                entry.material = o.Value as Material;
                ret.Add(entry);
            }

            return ret;
        }

        void SearchForMaterials(List<MaterialEntry> materials, AlembicImporter importer)
        {
            const string searchString = "t:Material ";
            var searchPath = new string[1];
            switch (materialSearchLocation)
            {
                case MaterialSearchLocation.ProjectWide:
                    searchPath[0] = "Assets";
                    break;
                case MaterialSearchLocation.CurrentFolder:
                    searchPath[0] = Path.GetDirectoryName(importer.assetPath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            foreach (var entry in materials)
            {
                var facesetName = entry.component.FacesetNames[entry.index];
                var guids = AssetDatabase.FindAssets(searchString + facesetName, searchPath);
                if (guids.Length > 0)
                {
                    importer.AddRemap(entry.ToSourceAssetIdentifier(), LoadAssetFromGuid<Material>(guids[0]));
                }
            }
        }

        T LoadAssetFromGuid<T>(string guid) where T : UnityEngine.Object
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return path == null ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        /*  static void DisplayMaterialUI(SerializedProperty materialAssignment)
          {
              var keysProp = materialAssignment.FindPropertyRelative("keys");
              var valsProp = materialAssignment.FindPropertyRelative("values");
              for (var i = 0; i < keysProp.arraySize; ++i)
              {
                  var matSlotProp = keysProp.GetArrayElementAtIndex(i);
                  var matProp = valsProp.GetArrayElementAtIndex(i);
                  var facesetName = matSlotProp.FindPropertyRelative("facesetName").stringValue;

                  EditorGUILayout.ObjectField(matProp, typeof(Material), new GUIContent(facesetName));
              }

              if (GUILayout.Button("Auto-Assign"))
              {
                  const string searchString = "t:Material ";
                  for (var i = 0; i < keysProp.arraySize; ++i)
                  {
                      var matSlotProp = keysProp.GetArrayElementAtIndex(i);
                      var matProp = valsProp.GetArrayElementAtIndex(i);
                      var facesetName = matSlotProp.FindPropertyRelative("facesetName").stringValue;
                      var guid = AssetDatabase.FindAssets(searchString + facesetName);
                      if (guid.Length > 0)
                      {
                          var path = AssetDatabase.GUIDToAssetPath(guid[0]);
                          var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                          matProp.objectReferenceValue = mat;
                      }
                  }
              }
          }*/

        internal static void DisplayEnumProperty(SerializedProperty prop, string[] displayNames, GUIContent guicontent = null)
        {
            if (guicontent == null)
                guicontent = new GUIContent(prop.displayName);

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, guicontent, prop);
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            var options = new GUIContent[displayNames.Length];
            for (int i = 0; i < options.Length; ++i)
                options[i] = new GUIContent(ObjectNames.NicifyVariableName(displayNames[i]), "");

            var normalsModeNew = EditorGUI.Popup(rect, guicontent, prop.intValue, options);
            if (EditorGUI.EndChangeCheck())
                prop.intValue = normalsModeNew;

            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }
    }
}

#endif
