#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace UTJ.Alembic
{
    public class DebugCommands
    {

        [MenuItem("Debug/Gen Tangents")]
        public static void GenTangents()
        {
            Mesh target;

            target = Selection.activeObject as Mesh;
            if (target == null)
            {
                var go = Selection.activeGameObject;
                if (go != null)
                {
                    {
                        var mf = go.GetComponent<MeshFilter>();
                        if (mf != null)
                            target = mf.sharedMesh;
                    }

                    if (target == null)
                    {
                        var smr = go.GetComponent<SkinnedMeshRenderer>();
                        if (smr != null)
                            target = smr.sharedMesh;
                    }
                }
            }

            if (target != null)
            {
                target.RecalculateTangents();
                Debug.Log("GenTangents: done");
            }
            else
            {
                Debug.Log("GenTangents: failed (Mesh not found)");
            }
        }
    }
}
#endif
