using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

[ExecuteInEditMode]
public class AlembicMeshCustomDataMapper : MonoBehaviour
{
    AlembicCustomData customData;

    Mesh mesh;
    // Start is called before the first frame update

    // Update is called once per frame
    void LateUpdate()
    {
        customData = GetComponent<AlembicCustomData>();
        var mf = GetComponent<MeshFilter>();
        if (mf != null)
        {
            mesh = mf.sharedMesh;
        }

        if (customData == null || mesh == null)
        {
            return;
        }

        // Make sure the Alembic is up to date;
        var asp = GetComponentInParent<AlembicStreamPlayer>();
        asp.UpdateImmediately(asp.CurrentTime);

        foreach (var attribute in customData.VertexAttributes)
        {

            if (attribute.Name == "furUVs")
            {
                mesh.SetUVs(1, attribute.Data);
            }
            else if (attribute.Name == "rigUVs")
            {
                mesh.SetUVs(3, attribute.Data);
            }
            else if (attribute.Name == "transferUVs")
            {
                mesh.SetUVs(2, attribute.Data);
            }
            else
            {
                Debug.LogWarning($"Unhandled Attribute: {attribute.Name.Value}");
            }


        }
    }
}
