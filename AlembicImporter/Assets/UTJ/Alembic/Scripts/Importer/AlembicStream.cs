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

        public AlembicTreeNode m_abcTreeRoot;
        private AlembicStreamDescriptor m_streamDesc;
        private aiConfig m_config;
        private aiContext m_context;
        private float m_time;
        private bool m_loaded;
        private bool m_streamInterupted;

        public bool abcIsValid { get { return m_context; } }
        public float abcStartTime { get { return m_context.startTime; } }
        public float abcEndTime { get { return m_context.endTime; } }
        public int abcFrameCount { get { return m_context.frameCount; } }

        public aiConfig config { get { return m_config; } }
        public float vertexMotionScale
        {
            get { return m_config.vertexMotionScale; }
            set { m_config.vertexMotionScale = value; }
        }

        public AlembicStream(GameObject rootGo, AlembicStreamDescriptor streamDesc)
        {
            m_config.SetDefaults();
            m_abcTreeRoot = new AlembicTreeNode() { streamDescriptor = streamDesc, linkedGameObj = rootGo };
            m_streamDesc = streamDesc;
        }

        void AbcBeforeUpdateSamples(AlembicTreeNode node)
        {
            foreach (var obj in node.alembicObjects)
                obj.Value.AbcBeforeUpdateSamples();
            foreach (var child in node.children)
                AbcBeforeUpdateSamples(child);
        }

        void AbcAfterUpdateSamples(AlembicTreeNode node)
        {
            foreach (var obj in node.alembicObjects)
                obj.Value.AbcUpdate();
            foreach (var child in node.children)
                AbcAfterUpdateSamples(child);
        }

        // returns false if the context needs to be recovered.
        public bool AbcUpdate(float time)
        {
            if (m_streamInterupted) return true;
            if (!abcIsValid || !m_loaded) return false;

            m_time = time;
            m_context.SetConfig(ref m_config);
            AbcBeforeUpdateSamples(m_abcTreeRoot);

            m_context.UpdateSamples(m_time);
            AbcAfterUpdateSamples(m_abcTreeRoot);
            return true;
        }

   
        public void AbcLoad()
        {
            m_time = 0.0f;
            m_context = aiContext.Create(m_abcTreeRoot.linkedGameObj.GetInstanceID());

            var settings = m_streamDesc.settings;
            m_config.swapHandedness = settings.swapHandedness;
            m_config.swapFaceWinding = settings.swapFaceWinding;
            m_config.aspectRatio = GetAspectRatio(settings.cameraAspectRatio);
            m_config.normalsMode = settings.normals;
            m_config.tangentsMode = settings.tangents;
            m_config.turnQuadEdges = settings.turnQuadEdges;
            m_config.interpolateSamples = settings.interpolateSamples;
#if UNITY_2017_3_OR_NEWER
            m_config.splitUnit = 0x7fffffff;
#else
            m_config.splitUnit = 65000;
#endif
            m_context.SetConfig(ref m_config);
            m_loaded = m_context.Load(Application.streamingAssetsPath + m_streamDesc.pathToAbc);

            if (m_loaded)
            {
                UpdateAbcTree(m_context, m_abcTreeRoot, m_time);
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
        void UpdateAbcTree(aiContext ctx, AlembicTreeNode node, float time, bool createMissingNodes = true)
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

                var childTransf = treeNode.linkedGameObj == null ? null : treeNode.linkedGameObj.transform.Find(childName);
                if (childTransf == null)
                {
                    if (!ic.createMissingNodes)
                    {
                        obj.enabled = false;
                        return;
                    }

                    childGO = new GameObject { name = childName };
                    var trans = childGO.GetComponent<Transform>();
                    trans.parent = treeNode.linkedGameObj.transform;
                    trans.localPosition = Vector3.zero;
                    trans.localEulerAngles = Vector3.zero;
                    trans.localScale = Vector3.one;
                }
                else
                    childGO = childTransf.gameObject;

                childTreeNode = new AlembicTreeNode() { linkedGameObj = childGO, streamDescriptor = treeNode.streamDescriptor };
                treeNode.children.Add(childTreeNode);

                // Update
                AlembicElement elem = null;

                if (obj.AsXform())
                    elem = childTreeNode.GetOrAddAlembicObj<AlembicXform>();
                else if (obj.AsPolyMesh())
                    elem = childTreeNode.GetOrAddAlembicObj<AlembicMesh>();
                else if (obj.AsCamera())
                    elem = childTreeNode.GetOrAddAlembicObj<AlembicCamera>();
                else if (obj.AsPoints())
                    elem = childTreeNode.GetOrAddAlembicObj<AlembicPoints>();

                if (elem != null)
                {
                    elem.AbcSetup(obj, schema);
                    elem.AbcBeforeUpdateSamples();
                    schema.UpdateSample(ref ic.ss);
                    elem.AbcUpdate();
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
