#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Importer;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityEditor.Formats.Alembic.Importer
{
    [CustomEditor(typeof(AlembicImporter)), CanEditMultipleObjects]
    class AlembicImporterEditor : ScriptedImporterEditor
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


        bool materialRootFold = true;
        List<bool> materialFold = new List<bool>();
        SavedInt uiTab;
        MaterialSearchLocation materialSearchLocation;

        public override void OnEnable()
        {
            base.OnEnable();
            uiTab = new SavedInt("AlembicImporterUITab", (int)UITab.Model);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var importer = serializedObject.targetObject as AlembicImporter;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                uiTab.value = GUILayout.Toolbar(uiTab, new[] {"Model", "Materials"});
                GUILayout.FlexibleSpace();
            }

            if (uiTab == (int)UITab.Model)
            {
                DrawModelUI(importer, serializedObject.isEditingMultipleObjects);
            }
            else
            {
                DrawMaterialUI(importer, serializedObject.isEditingMultipleObjects);
            }


            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        protected override void Apply()
        {
            var importer = serializedObject.targetObject as AlembicImporter;
            var remaps = importer.GetExternalObjectMap();
            foreach (var remap in remaps.Where(remap => remap.Value == null))
            {
                importer.RemoveRemap(remap.Key);
            }
            base.Apply();
        }

        void DrawModelUI(AlembicImporter importer, bool isMultiEdit)
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

                if (!isMultiEdit)
                {
                    // time range
                    var startTimeProp = serializedObject.FindProperty("startTime");
                    var endTimeProp = serializedObject.FindProperty("endTime");

                    EditorGUI.BeginDisabledGroup(startTimeProp.hasMultipleDifferentValues ||
                        endTimeProp.hasMultipleDifferentValues);
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
                    var newStartTime = EditorGUILayout.FloatField(new GUIContent(" ", "Start time"), startTime,
                        GUILayout.MinWidth(90.0f));
                    EditorGUI.showMixedValue = endTimeProp.hasMultipleDifferentValues;
                    var newEndTime = EditorGUILayout.FloatField(new GUIContent(" ", "End time"), endTime,
                        GUILayout.MinWidth(90.0f));
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
                        EditorGUILayout.LabelField(new GUIContent((endTime - startTime).ToString("0.000") + "s"),
                            style);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "normals"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "tangents"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "flipFaces"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Cameras", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "cameraAspectRatio"), new GUIContent("Aspect Ratio", ""));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Separator();
        }

        static List<AlembicImporter.MaterialEntry> GenMaterialSlotsPresetUI(AlembicImporter importer)
        {
            var ret = new List<AlembicImporter.MaterialEntry>();
            foreach (var r in importer.GetExternalObjectMap())
            {
                var toks = r.Key.name.Split(':');
                var entry = new AlembicImporter.MaterialEntry
                {
                    path = toks[0], index = int.Parse(toks[1]), facesetName = toks[2], material = r.Value as Material
                };
                ret.Add(entry);
            }

            return ret;
        }

        void DrawMaterialUI(AlembicImporter importer, bool multiEdit)
        {
            if (multiEdit)
            {
                EditorGUILayout.HelpBox("Material Editing is not supported on multiple selection.", MessageType.Info);
                return;
            }

            var mainGO = AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);
            List<AlembicImporter.MaterialEntry> materials = new List<AlembicImporter.MaterialEntry>();
            if (mainGO != null) // Null in case of a Preset for the importer
            {
                materials = AlembicImporter.GenMaterialSlots(importer, mainGO);

                if (!materials.Any())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("No facesets present in file.");
                        GUILayout.FlexibleSpace();
                    }

                    return;
                }
            }
            else
            {
                materials = GenMaterialSlotsPresetUI(importer);
            }

            var strideArray = new List<int>(); // list that "breaks" the materials when the gameObject changes.
            if (materials.Count > 0)
            {
                strideArray.Add(0);
            }

            for (var m = 0; m < materials.Count - 1; ++m)
            {
                var currentGO = materials[m].path;
                for (var i = m + 1; i < materials.Count; ++i)
                {
                    var o = materials[i];
                    if (o.path != currentGO)
                    {
                        m = i - 1;
                        strideArray.Add(i);
                        break;
                    }
                }
            }

            strideArray.Add(materials.Count);


            if (materialFold.Count != strideArray.Count)
            {
                materialFold = Enumerable.Repeat(true, strideArray.Count).ToList();
            }

            EditorGUILayout.LabelField("Material Search");

            using (new EditorGUI.IndentLevelScope())
            {
                materialSearchLocation =
                    (MaterialSearchLocation)EditorGUILayout.EnumPopup("Location", materialSearchLocation);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Search and Remap"))
                    {
                        SearchForMaterials(materials, importer);
                    }
                }
            }

            var newRootFoldout = EditorGUILayout.Foldout(materialRootFold, "Meshes / Face Sets", true);
            if (materialRootFold != newRootFoldout && Event.current != null && Event.current.alt)
            {
                materialFold = Enumerable.Repeat(newRootFoldout, materialFold.Count).ToList();
            }
            using (new EditorGUI.IndentLevelScope())
            {
                materialRootFold = newRootFoldout;
                if (materialRootFold)
                {
                    for (var s = 0; s < strideArray.Count - 1; ++s)
                    {
                        var path = materials[strideArray[s]].path.Split('/');
                        var label = new GUIContent(path[path.Length - 1]);
                        if (mainGO != null)
                        {
                            string tooltip = "";
                            tooltip += path.Aggregate(tooltip, (current, p) => current + p + " > ");
                            label.tooltip = mainGO.name + " > " + tooltip.Remove(tooltip.Length - 3, 3);
                        }

                        materialFold[s] =
                            EditorGUILayout.Foldout(materialFold[s], label, true);
                        if (materialFold[s])
                        {
                            for (var i = strideArray[s]; i < strideArray[s + 1]; ++i)
                            {
                                var o = materials[i];
                                var fsName = materials[i].facesetName;
                                using (new EditorGUI.IndentLevelScope())
                                {
                                    using (var c = new EditorGUI.ChangeCheckScope())
                                    {
                                        var bakStyle = EditorStyles.objectField.fontStyle;
                                        var emptyName = string.IsNullOrEmpty(fsName);
                                        if (emptyName)
                                        {
                                            EditorStyles.label.fontStyle = FontStyle.BoldAndItalic;
                                        }

                                        var assign = EditorGUILayout.ObjectField(!emptyName ? fsName : "Empty",
                                            o.material,
                                            typeof(Material), false);

                                        EditorStyles.label.fontStyle = bakStyle;
                                        if (c.changed)
                                        {
                                            if (mainGO != null && assign == null)
                                            {
                                                importer.RemoveRemap(o.ToSourceAssetIdentifier());
                                            }
                                            else if (AssetDatabase.GetAssetPath(assign).ToLower().EndsWith("abc"))
                                            {
                                                Debug.LogError("Materials cannot be remapped to materials bundled with Alembic files.");
                                            }
                                            else
                                            {
                                                Undo.RegisterCompleteObjectUndo(importer, "Alembic Material");
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
        }

        void SearchForMaterials(List<AlembicImporter.MaterialEntry> materials, AlembicImporter importer)
        {
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
                var mat = LoadMaterial(entry.facesetName, searchPath);
                if (mat != null)
                {
                    Undo.RegisterCompleteObjectUndo(importer, "Alembic Material");
                    importer.AddRemap(entry.ToSourceAssetIdentifier(), mat);
                }
            }
        }

        static Material LoadMaterial(string assetName, string[] searchPath)
        {
            const string searchString = "t:Material ";
            var path = AssetDatabase.FindAssets(searchString + assetName, searchPath)
                .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(str =>
                    string.Equals(Path.GetFileNameWithoutExtension(str), assetName,
                        StringComparison.CurrentCultureIgnoreCase));

            if (path == null || path.ToLower().EndsWith("abc")) // we don't want to assign materials from other alembics since these gets recreated on every re-import.
            {
                return null;
            }


            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }

    class SavedInt
    {
        int m_Value;
        string m_Name;
        bool m_Loaded;
        public SavedInt(string name, int value)
        {
            m_Name = name;
            m_Loaded = false;
            m_Value = value;
        }

        void Load()
        {
            if (m_Loaded)
                return;
            m_Loaded = true;
            m_Value = EditorPrefs.GetInt(m_Name, m_Value);
        }

        public int value
        {
            get { Load(); return m_Value; }
            set
            {
                Load();
                if (m_Value == value)
                    return;
                m_Value = value;
                EditorPrefs.SetInt(m_Name, value);
            }
        }
        public static implicit operator int(SavedInt s)
        {
            return s.value;
        }
    }
}

#endif
