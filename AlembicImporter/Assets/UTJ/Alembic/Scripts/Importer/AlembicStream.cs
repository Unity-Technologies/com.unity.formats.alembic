using System;
using System.Collections.Generic;
using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicStream : IDisposable
    {
        static List<AlembicStream> s_streams = new List<AlembicStream>();

        public static void DisconnectStreamsWithPath(string path)
        {
            var fullPath = Application.streamingAssetsPath + path;
            AbcAPI.aiClearContextsWithPath(fullPath);
            s_streams.ForEach(s => {
                if (s.m_streamDesc.pathToAbc == path)
                {
                    s.m_streamInterupted = true;
                    s.m_context = default(AbcAPI.aiContext);
                    s.m_loaded = false;   
                }
            });
        } 

        public static void RemapStreamsWithPath(string oldPath , string newPath)
        {
            s_streams.ForEach(s =>
            {
                if (s.m_streamDesc.pathToAbc == oldPath)
                {
                    s.m_streamInterupted = true;
                    s.m_streamDesc.pathToAbc = newPath;
                }
            } );
        } 

        public static void ReconnectStreamsWithPath(string path)
        {
            s_streams.ForEach(s =>
            {
                if (s.m_streamDesc.pathToAbc == path)
                {
                    s.m_streamInterupted = false;
                }
                    
            } );
        }

        public AlembicTreeNode m_abcTreeRoot;
        private AlembicStreamDescriptor m_streamDesc;
        private AbcAPI.aiConfig m_config;
        private AbcAPI.aiContext m_context;
        private float m_time;
        private bool m_loaded;
        private bool m_streamInterupted;

        public bool abcIsValid { get { return m_context; } }
        public float abcStartTime { get { return AbcAPI.aiGetStartTime(m_context); } }
        public int abcFrameCount { get { return AbcAPI.aiGetFrameCount(m_context); } }
        public float abcEndTime { get { return AbcAPI.aiGetEndTime(m_context); } }

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
        public bool AbcUpdate(float time, float motionScale, bool interpolateSamples)
        {
            if (m_streamInterupted) return true;
            if (!abcIsValid || !m_loaded) return false;

            m_time = time;
            m_config.interpolateSamples = interpolateSamples;
            m_config.vertexMotionScale = motionScale;
            AbcAPI.aiSetConfig(m_context, ref m_config);
            AbcBeforeUpdateSamples(m_abcTreeRoot);

            AbcAPI.aiUpdateSamples(m_context, m_time);
            AbcAfterUpdateSamples(m_abcTreeRoot);
            return true;
        }

   
        public void AbcLoad()
        {
            m_time = 0.0f;
            m_context = AbcAPI.aiCreateContext(m_abcTreeRoot.linkedGameObj.GetInstanceID());

            var settings = m_streamDesc.settings;
            m_config.swapHandedness = settings.swapHandedness;
            m_config.swapFaceWinding = settings.swapFaceWinding;
            m_config.normalsMode = settings.normals;
            m_config.tangentsMode = settings.tangents;
            m_config.turnQuadEdges = settings.turnQuadEdges;
            m_config.aspectRatio = AbcAPI.GetAspectRatio(settings.cameraAspectRatio);
#if UNITY_2017_3_OR_NEWER
            m_config.splitUnit = 0x7fffffff;
#else
            m_config.splitUnit = 65000;
#endif
            AbcAPI.aiSetConfig(m_context, ref m_config);

            m_loaded = AbcAPI.aiLoad(m_context,Application.streamingAssetsPath + m_streamDesc.pathToAbc);

            if (m_loaded)
            {
                AbcAPI.UpdateAbcTree(m_context, m_abcTreeRoot, m_time);
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
                AbcAPI.aiDestroyContext(m_context);
                m_context = default(AbcAPI.aiContext);
            }
        }
    }
}
