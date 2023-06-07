using System;
using System.IO;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEditor.Formats.Alembic.Importer
{
    [CustomEditor(typeof(AlembicStreamPlayer)), CanEditMultipleObjects]
    class AlembicStreamPlayerEditor : Editor
    {
        static readonly GUIContent RecreateContent = new GUIContent("Create Missing GameObjects",
            "Re-create the GameObject hierarchy to mirror the node structure in the current Alembic file.");
        static readonly GUIContent DeleteExtraContent = new GUIContent("Remove Unused GameObjects",
            "Deletes all GameObjects that are not in the current Alembic file.");

        bool loadSucceded = true;
        void OnEnable()
        {
            RegisterCallbacks();
            var streamPlayer = target as AlembicStreamPlayer;
            if (streamPlayer.abcStream != null)
            {
                loadSucceded = streamPlayer.abcStream.abcIsValid;
            }
            else
            {
                // We have no stream, open and close just to update the UI status.
                loadSucceded = streamPlayer.LoadStream(false, true);
                streamPlayer.CloseStream();
            }
        }

        void OnDisable()
        {
            UnregisterCallbacks();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var streamPlayer = target as AlembicStreamPlayer;
            var externalSource = streamPlayer.StreamSource == AlembicStreamPlayer.AlembicStreamSource.External;
            var prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(streamPlayer.gameObject);
            var prefabStatus = PrefabUtility.GetPrefabAssetType(streamPlayer.gameObject);

            var canAddGos = prefabStatus == PrefabAssetType.NotAPrefab || prefabInstanceStatus != PrefabInstanceStatus.NotAPrefab;
            using (new EditorGUI.DisabledGroupScope((target.hideFlags & HideFlags.NotEditable) != HideFlags.None))
            {
                var streamDescriptorObj = serializedObject.FindProperty("streamDescriptor");
                var startTime = serializedObject.FindProperty("startTime");
                var endTime = serializedObject.FindProperty("endTime");

                var targetStreamDesc = streamPlayer.StreamDescriptor;
                if (externalSource && !serializedObject.isEditingMultipleObjects)
                {
                    using (new EditorGUI.DisabledGroupScope(!canAddGos))
                    {
                        var initialFilePath = targetStreamDesc != null ? targetStreamDesc.PathToAbc : "";
                        var filePath = initialFilePath;
                        EditorGUILayout.LabelField(new GUIContent("Alembic File"));
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            filePath = EditorGUILayout.DelayedTextField(filePath);
                            if (GUILayout.Button(new GUIContent("..."), GUILayout.MaxWidth(30)))
                            {
                                var dir = "";
                                if (File.Exists(filePath))
                                {
                                    dir = Path.GetDirectoryName(filePath);
                                }

                                var path = EditorUtility.OpenFilePanel("Load Alembic File", dir, "abc");
                                if (!string.IsNullOrWhiteSpace(path))
                                    filePath = path;
                            }
                        }

                        if (filePath != initialFilePath)
                        {
                            Undo.RecordObject(streamPlayer, "Load Alembic File");
                            Undo.RegisterFullObjectHierarchyUndo(streamPlayer.gameObject, "Load Alembic File");
                            loadSucceded = streamPlayer.LoadFromFile(filePath);
                        }

                        if (string.IsNullOrEmpty(filePath))
                        {
                            return;
                        }

                        if (!File.Exists(filePath))
                        {
                            EditorGUILayout.HelpBox(
                                "Alembic file path not found.",
                                MessageType.Error);
                            return;
                        }

                        if (streamPlayer.abcStream != null && streamPlayer.abcStream.IsHDF5())
                        {
                            EditorGUILayout.HelpBox("Unsupported HDF5 file format detected. Please convert to Ogawa.",
                                MessageType.Error);
                            return;
                        }

                        if (!loadSucceded)
                        {
                            EditorGUILayout.HelpBox("File is in an unknown format", MessageType.Error);
                            return;
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("From File", GUILayout.Width(150));

                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(RecreateContent, GUILayout.Width(200)))
                            {
                                streamPlayer.LoadStream(true);
                            }
                        }
                    }

                    using (new EditorGUI.DisabledGroupScope(!canAddGos))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(DeleteExtraContent, GUILayout.Width(200)))
                            {
                                Undo.RegisterFullObjectHierarchyUndo(streamPlayer.gameObject,
                                    "CleanUp GameObject Hierarchy");
                                streamPlayer.RemoveObsoleteGameObjects();
                            }
                        }
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        EditorGUILayout.ObjectField(streamDescriptorObj);
                    }
                }

                if (!externalSource && streamDescriptorObj.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("The stream descriptor could not be found.", MessageType.Error);
                    return;
                }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("currentTime"), new GUIContent("Time"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("vertexMotionScale"));
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(new GUIContent("Time Range"));

                var abcStart = targetStreamDesc.MediaStartTime;
                var abcEnd = targetStreamDesc.MediaEndTime;
                var start = streamPlayer.StartTime;
                var end = streamPlayer.EndTime;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(" ", ref start, ref end, abcStart, abcEnd);
                if (EditorGUI.EndChangeCheck())
                {
                    if (Math.Abs(startTime.doubleValue - start) > 1e-5) // The Mix max slider is "jiggly": changes slightly all numbers even though they were not changed.
                    {
                        startTime.doubleValue = start;
                    }

                    if (Math.Abs(endTime.doubleValue - end) > 1e-5)
                    {
                        endTime.doubleValue = end;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(new GUIContent("seconds"));
                EditorGUI.BeginChangeCheck();
                EditorGUIUtility.labelWidth = 35.0f;
                EditorGUI.showMixedValue = startTime.hasMultipleDifferentValues;
                var newStartTime = EditorGUILayout.FloatField(new GUIContent("from", "Start time"), start,
                    GUILayout.MinWidth(80.0f));
                GUILayout.FlexibleSpace();
                EditorGUIUtility.labelWidth = 20.0f;
                EditorGUI.showMixedValue = endTime.hasMultipleDifferentValues;
                var newEndTime =
                    EditorGUILayout.FloatField(new GUIContent("to", "End time"), end, GUILayout.MinWidth(80.0f));
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    startTime.doubleValue = newStartTime;
                    endTime.doubleValue = newEndTime;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = 0.0f;

                GUIStyle style = new GUIStyle();
                style.alignment = TextAnchor.LowerRight;
                if (!endTime.hasMultipleDifferentValues && !startTime.hasMultipleDifferentValues)
                {
                    EditorGUILayout.LabelField(new GUIContent((end - start).ToString("0.000") + "s"), style);
                }
            }

            if (externalSource && !serializedObject.isEditingMultipleObjects)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    DrawStreamSettings(serializedObject.FindProperty("embeddedStreamDescriptor.settings"));
                    if (check.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        streamPlayer.Settings = streamPlayer.StreamDescriptor.Settings;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        static void DrawStreamSettings(SerializedProperty settings)
        {
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("scaleFactor"),
                    new GUIContent("Scale Factor",
                        "Use this property to resize models relative to their dimensions in the source file."));
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("swapHandedness"),
                    new GUIContent("Swap Handedness", "Swaps the X coordinate direction."));
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("interpolateSamples"),
                    new GUIContent("Interpolate Samples",
                        "Interpolate transforms and vertices (if topology is constant)."));
                EditorGUILayout.Separator();

                EditorGUILayout.PropertyField(settings.FindPropertyRelative("importVisibility"),
                    new GUIContent("Import Visibility", "Import visibility animation."));

                EditorGUILayout.PropertyField(settings.FindPropertyRelative("importCameras"),
                    new GUIContent("Import Cameras", ""));
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("importMeshes"),
                    new GUIContent("Import Meshes", ""));
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("importPoints"),
                    new GUIContent("Import Points", ""));
                var importCurvesProp = settings.FindPropertyRelative("importCurves");
                EditorGUILayout.PropertyField(importCurvesProp, new GUIContent("Import Curves", ""));
                if (importCurvesProp.boolValue == true)
                {
                    var curveRenderers = settings.FindPropertyRelative("createCurveRenderers");
                    EditorGUILayout.PropertyField(curveRenderers,
                        new GUIContent("Add Curve Renderers",
                            "Automatically add 'AlembicCurvesRenderer' components on curve objects.\nThis allows you to get a basic preview of the Alembic curves in the Scene."));
                }

                EditorGUILayout.Separator();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("normals"));
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("tangents"));
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("flipFaces"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Cameras", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("cameraAspectRatio"), new GUIContent("Aspect Ratio", ""));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Separator();
        }

        void RegisterCallbacks()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void UnregisterCallbacks()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        void UndoRedoPerformed()
        {
            if (target == null || (target as AlembicStreamPlayer).StreamSource != AlembicStreamPlayer.AlembicStreamSource.External) // Out of project streaming needs to reload the stream to update the data on undo.
            {
                return;
            }

            loadSucceded = (target as AlembicStreamPlayer).ReloadStream();
        }

        [MenuItem("CONTEXT/AlembicStreamPlayer/Reset"),]
        static void ResetPreventer()
        {
        }

        [MenuItem("CONTEXT/AlembicStreamPlayer/Reset", validate = true),]
        static bool ResetPreventerValidate()
        {
            return false;
        }
    }
}
