using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UTJ.Alembic
{
    [System.ComponentModel.DisplayName("Alembic Recorder Clip")]
    public class AlembicRecorderClip : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] AlembicRecorderSettings m_settings = new AlembicRecorderSettings();
        [SerializeField] bool m_ignoreFirstFrame = true;
        [SerializeField] string m_targetBranchPath = "";

        public AlembicRecorderSettings settings { get { return m_settings; } }
        public GameObject targetBranch
        {
            get { return FindObjectByPath(m_targetBranchPath); }
            set { m_targetBranchPath = GetPath(value); }
        }

        public ClipCaps clipCaps { get { return ClipCaps.None; } }

        public static GameObject FindObjectByPath(string path)
        {
            var names = path.Split('/');
            Transform ret = null;
            foreach (var name in names)
            {
                if (name.Length == 0) { continue; }
                ret = FindObjectByName(ret, name);
                if (ret == null) { break; }
            }
            return ret != null ? ret.gameObject : null;
        }
        public static Transform FindObjectByName(Transform parent, string name)
        {
            Transform ret = null;
            if (parent == null)
            {
                var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var go in roots)
                {
                    if (go.name == name)
                    {
                        ret = go.GetComponent<Transform>();
                        break;
                    }
                }
            }
            else
            {
                ret = parent.Find(name);
            }
            return ret;
        }

        public static string GetPath(Transform trans)
        {
            if (trans == null)
                return "";
            string ret = "/" + trans.name;
            if (trans.parent != null)
                ret = GetPath(trans.parent) + ret;
            return ret;
        }
        public static string GetPath(GameObject go)
        {
            return go == null ? "" : GetPath(go.transform);
        }



        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_settings.targetBranch = targetBranch;

            var ret = ScriptPlayable<AlembicRecorderBehaviour>.Create(graph);
            var behaviour = ret.GetBehaviour();
            behaviour.settings = m_settings;
            behaviour.ignoreFirstFrame = m_ignoreFirstFrame;
            return ret;
        }

        public virtual void OnDestroy()
        {
        }
    }
}
