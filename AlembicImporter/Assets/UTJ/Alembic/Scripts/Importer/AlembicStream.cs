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

        public AlembicTreeNode m_alembicTreeRoot;
        private AlembicStreamDescriptor m_streamDesc;
        private AbcAPI.aiConfig m_config;
        private AbcAPI.aiContext m_context;
        private float m_time;
        private bool m_loaded;
        private bool m_streamInterupted;

        public AlembicStream(GameObject rootGo, AlembicStreamDescriptor streamDesc)
        {
            m_config.SetDefaults();
            m_alembicTreeRoot = new AlembicTreeNode() { streamDescriptor = streamDesc, linkedGameObj = rootGo };
            m_streamDesc = streamDesc;
        }

        public bool AbcIsValid()
        {
            return (m_context.ptr != (IntPtr)0);
        }

        public void AbcUpdateConfigElements(AlembicTreeNode node = null)
        {
            if (node == null)
                node = m_alembicTreeRoot;
            using (var o = node.alembicObjects.GetEnumerator())
            {
                while (o.MoveNext())
                {
                    o.Current.Value.AbcUpdateConfig();
                }
            }
            using (var c = node.children.GetEnumerator())
            {
                while (c.MoveNext())
                {
                    AbcUpdateConfigElements(c.Current);
                }
            }
        }

        public void AbcUpdateElements( AlembicTreeNode node = null )
        {
            if (node == null)
                node = m_alembicTreeRoot;
            using (var o = node.alembicObjects.GetEnumerator())
            {
                while (o.MoveNext())
                {
                    o.Current.Value.AbcUpdate();
                }
            }
            using (var c = node.children.GetEnumerator())
            {
                while (c.MoveNext())
                {
                    AbcUpdateElements(c.Current);
                }
            }
        }

        public float abcStartTime
        {
            get {
                return AbcIsValid() ? AbcAPI.aiGetStartTime(m_context) : 0;
            }
        }
        public int abcFrameCount
        {
            get {
                return AbcIsValid() ? AbcAPI.aiGetFrameCount(m_context) : 0;
            }
        }

        public float abcEndTime
        {
            get
            {
                return AbcIsValid() ? AbcAPI.aiGetEndTime(m_context) : 0;
            }
        }

        // returns false if the context needs to be recovered.
        public bool AbcUpdate(float time, float motionScale, bool interpolateSamples)
        {
            if (m_streamInterupted) return true;
            if (!AbcIsValid() || !m_loaded) return false;

            m_time = time;
            m_config.interpolateSamples = interpolateSamples;
            m_config.vertexMotionScale = motionScale;
            AbcAPI.aiSetConfig(m_context, ref m_config);
            AbcUpdateConfigElements();

            AbcAPI.aiUpdateSamples(m_context, m_time);
            AbcUpdateElements();

            return true;
        }
   
        public void AbcLoad()
        {
            m_time = 0.0f;
            m_context = AbcAPI.aiCreateContext(m_alembicTreeRoot.linkedGameObj.GetInstanceID());

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
                AbcAPI.UpdateAbcTree(m_context, m_alembicTreeRoot, m_time);
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
            if (m_alembicTreeRoot != null)
            {
                m_alembicTreeRoot.Dispose();
                m_alembicTreeRoot = null;
            }

            if (AbcIsValid())
            {
                AbcAPI.aiDestroyContext(m_context);
                m_context = default(AbcAPI.aiContext);
            }
        }
    }
}
