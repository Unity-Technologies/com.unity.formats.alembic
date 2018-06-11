using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UTJ.Alembic
{
    public class AlembicStream : IDisposable
    {
        static List<AlembicStream> s_streams = new List<AlembicStream>();

        public static void DisconnectStreamsWithPath(string path)
        {
            var fullPath = Application.streamingAssetsPath + path;
            aiContext.DestroyByPath(fullPath);
            s_streams.ForEach(s => {
                if (s.m_streamDesc.pathToAbc == path)
                {
                    s.m_streamInterupted = true;
                    s.m_context = default(aiContext);
                    s.m_loaded = false;
                }
            });
        }

        public static void RemapStreamsWithPath(string oldPath, string newPath)
        {
            s_streams.ForEach(s =>
            {
                if (s.m_streamDesc.pathToAbc == oldPath)
                {
                    s.m_streamInterupted = true;
                    s.m_streamDesc.pathToAbc = newPath;
                }
            });
        }

        public static void ReconnectStreamsWithPath(string path)
        {
            s_streams.ForEach(s =>
            {
                if (s.m_streamDesc.pathToAbc == path)
                {
                    s.m_streamInterupted = false;
                }
            });
        }

        AlembicStreamDescriptor m_streamDesc;
        AlembicTreeNode m_abcTreeRoot;
        aiConfig m_config;
        aiContext m_context;
        double m_time;
        bool m_ignoreVisibility;
        bool m_loaded;
        bool m_streamInterupted;

        public AlembicStreamDescriptor streamDescriptor { get { return m_streamDesc; } }
        public AlembicTreeNode abcTreeRoot { get { return m_abcTreeRoot; } }
        public aiContext abcContext { get { return m_context; } }
        public bool abcIsValid { get { return m_context; } }
        public aiConfig config { get { return m_config; } }
        public float vertexMotionScale { set { m_config.vertexMotionScale = value; } }
        public bool asyncLoad { set { m_config.asyncLoad = value; } }
        public bool ignoreVisibility { get { return m_ignoreVisibility; } set { m_ignoreVisibility = value; } }

        public void GetTimeRange(ref double begin, ref double end) { m_context.GetTimeRange(ref begin, ref end); }


        public AlembicStream(GameObject rootGo, AlembicStreamDescriptor streamDesc)
        {
            m_config.SetDefaults();
            m_abcTreeRoot = new AlembicTreeNode() { stream = this, gameObject = rootGo };
            m_streamDesc = streamDesc;
        }

        void AbcBeforeUpdateSamples(AlembicTreeNode node)
        {
            if (node.abcObject != null && node.gameObject != null)
                node.abcObject.AbcPrepareSample();
            foreach (var child in node.children)
                AbcBeforeUpdateSamples(child);
        }

        void AbcBeginSyncData(AlembicTreeNode node)
        {
            if (node.abcObject != null && node.gameObject != null)
                node.abcObject.AbcSyncDataBegin();
            foreach (var child in node.children)
                AbcBeginSyncData(child);
        }

        void AbcEndSyncData(AlembicTreeNode node)
        {
            if (node.abcObject != null && node.gameObject != null)
                node.abcObject.AbcSyncDataEnd();
            foreach (var child in node.children)
                AbcEndSyncData(child);
        }

        // returns false if the context needs to be recovered.
        public bool AbcUpdateBegin(double time)
        {
            if (m_streamInterupted) return true;
            if (!abcIsValid || !m_loaded) return false;

            m_time = time;
            m_context.SetConfig(ref m_config);
            AbcBeforeUpdateSamples(m_abcTreeRoot);
            m_context.UpdateSamples(m_time);
            return true;
        }

        // returns false if the context needs to be recovered.
        public void AbcUpdateEnd()
        {
            AbcBeginSyncData(m_abcTreeRoot);
            AbcEndSyncData(m_abcTreeRoot);
        }

        public void AbcLoad(bool createMissingNodes)
        {
            m_time = 0.0f;
            m_context = aiContext.Create(m_abcTreeRoot.gameObject.GetInstanceID());

            var settings = m_streamDesc.settings;
            m_config.swapHandedness = settings.swapHandedness;
            m_config.swapFaceWinding = settings.swapFaceWinding;
            m_config.aspectRatio = GetAspectRatio(settings.cameraAspectRatio);
            m_config.scaleFactor = settings.scaleFactor;
            m_config.normalsMode = settings.normals;
            m_config.tangentsMode = settings.tangents;
            m_config.turnQuadEdges = settings.turnQuadEdges;
            m_config.interpolateSamples = settings.interpolateSamples;
            m_config.importPointPolygon = settings.importPointPolygon;
            m_config.importLinePolygon = settings.importLinePolygon;
            m_config.importTrianglePolygon = settings.importTrianglePolygon;

            m_context.SetConfig(ref m_config);
            m_loaded = m_context.Load(Application.streamingAssetsPath + m_streamDesc.pathToAbc);

            if (m_loaded)
            {
                UpdateAbcTree(m_context, m_abcTreeRoot, m_time, createMissingNodes);
                AlembicStream.s_streams.Add(this);
            }
            else
            {
                Debug.LogError("failed to load alembic at " + Application.streamingAssetsPath + m_streamDesc.pathToAbc);
            }
        }

        public void Dispose()
        {
            AlembicStream.s_streams.Remove(this);
            if (m_abcTreeRoot != null)
            {
                m_abcTreeRoot.Dispose();
                m_abcTreeRoot = null;
            }

            if (abcIsValid)
            {
                m_context.Destroy();
            }
        }



        class ImportContext
        {
            public AlembicTreeNode alembicTreeNode;
            public aiSampleSelector ss;
            public bool createMissingNodes;
        }

        ImportContext m_importContext;
        void UpdateAbcTree(aiContext ctx, AlembicTreeNode node, double time, bool createMissingNodes)
        {
            var top = ctx.topObject;
            if (!top)
                return;

            m_importContext = new ImportContext
            {
                alembicTreeNode = node,
                ss = AbcAPI.aiTimeToSampleSelector(time),
                createMissingNodes = createMissingNodes,
            };
            top.EachChild(ImportCallback);
            m_importContext = null;
        }

        void ImportCallback(aiObject obj)
        {
            var ic = m_importContext;
            AlembicTreeNode treeNode = ic.alembicTreeNode;
            AlembicTreeNode childTreeNode = null;

            aiSchema schema = obj.AsXform();
            if (!schema) schema = obj.AsPolyMesh();
            if (!schema) schema = obj.AsCamera();
            if (!schema) schema = obj.AsPoints();

            if (schema)
            {
                // Get child. create if needed and allowed.
                string childName = obj.name;

                // Find targetted child GameObj
                GameObject childGO = null;

                var childTransf = treeNode.gameObject == null ? null : treeNode.gameObject.transform.Find(childName);
                if (childTransf == null)
                {
                    if (!ic.createMissingNodes)
                    {
                        obj.enabled = false;
                        return;
                    }
                    else
                    {
                        obj.enabled = true;
                    }

                    childGO = new GameObject { name = childName };
                    childGO.GetComponent<Transform>().SetParent(treeNode.gameObject.transform, false);
                }
                else
                    childGO = childTransf.gameObject;

                childTreeNode = new AlembicTreeNode() { stream = this, gameObject = childGO };
                treeNode.children.Add(childTreeNode);

                // Update
                AlembicElement elem = null;

                if (obj.AsXform() && m_streamDesc.settings.importXform)
                    elem = childTreeNode.GetOrAddAlembicObj<AlembicXform>();
                else if (obj.AsCamera() && m_streamDesc.settings.importCamera)
                    elem = childTreeNode.GetOrAddAlembicObj<AlembicCamera>();
                else if (obj.AsPolyMesh() && m_streamDesc.settings.importPolyMesh)
                    elem = childTreeNode.GetOrAddAlembicObj<AlembicMesh>();
                else if (obj.AsPoints() && m_streamDesc.settings.importPoints)
                    elem = childTreeNode.GetOrAddAlembicObj<AlembicPoints>();

                if (elem != null)
                {
                    elem.AbcSetup(obj, schema);
                    elem.AbcPrepareSample();
                    schema.UpdateSample(ref ic.ss);
                    elem.AbcSyncDataBegin();
                    elem.AbcSyncDataEnd();
                }
            }
            else
            {
                obj.enabled = false;
            }

            ic.alembicTreeNode = childTreeNode;
            obj.EachChild(ImportCallback);
            ic.alembicTreeNode = treeNode;
        }

        public static float GetAspectRatio(aiAspectRatioMode mode)
        {
            if (mode == aiAspectRatioMode.CameraAperture)
            {
                return 0.0f;
            }
            else if (mode == aiAspectRatioMode.CurrentResolution)
            {
                return (float)Screen.width / (float)Screen.height;
            }
            else
            {
#if UNITY_EDITOR
                return (float)PlayerSettings.defaultScreenWidth / (float)PlayerSettings.defaultScreenHeight;
#else
                // fallback on current resoltution
                return (float) Screen.width / (float) Screen.height;
#endif
            }
        }
    }
}
