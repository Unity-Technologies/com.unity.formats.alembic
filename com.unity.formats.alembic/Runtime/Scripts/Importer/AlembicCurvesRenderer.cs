using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Rendering;

namespace Scripts.Importer
{
    [RequireComponent(typeof(AlembicCurves))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public class AlembicCurvesRenderer : MonoBehaviour
    {
        AlembicCurves curves;
        Mesh mesh;

        void OnEnable()
        {
            curves = GetComponent<AlembicCurves>();
            mesh = new Mesh {hideFlags = HideFlags.DontSave};
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer.sharedMaterial == null)
            {
                meshRenderer.sharedMaterial = GetDefaultMaterial();
            }
        }

        void LateUpdate()
        {
            GenerateLineMesh(mesh);
        }

        void OnDisable()
        {
            DestroyImmediate(mesh);
        }

        void GenerateLineMesh(Mesh theMesh)
        {
            /* for (var i = 1; i < curves.PositionsOffsetBuffer.Count; ++i)
      {
          var idx = curves.PositionsOffsetBuffer[i - 1];
          var p0 = curves.Positions[idx];
          do
          {
              idx++;
              var p1 = curves.Positions[idx];
              Debug.DrawLine(p0, p1);
          }
          while (idx < curves.positionOffsetBuffer[i]);
      }*/


            var positions = curves.Positions;

            var curveCount = curves.CurvePointCount.Count;

            var indexArraySize = (positions.Count - curves.CurvePointCount.Count) * 2;
            using (var particleTangent = new NativeArray<Vector3>(positions.Count, Allocator.Temp))
            using (var particleUV = new NativeArray<Vector2>(positions.Count, Allocator.Temp))
            using (var indices = new NativeArray<int>(indexArraySize, Allocator.Temp))
            {
                unsafe
                {
                    var indicesPtr = (int*)indices.GetUnsafePtr();
                    var particleTangentPtr = (Vector3*)particleTangent.GetUnsafePtr();
                    var particleUVPtr = (Vector2*)particleUV.GetUnsafePtr();

                    var strandParticleBegin = 0;
                    const int strandParticleStride = 1;
                    for (var i = 0; i < curveCount; i++)
                    {
                        var nativeArray = indices;
                        var index = nativeArray[i];
                        nativeArray[i] = index;

                        var curvePointCount = curves.CurvePointCount[i];
                        var strandParticleEnd = strandParticleBegin + curvePointCount;

                        var wireStrandLineCount = (curvePointCount - 1);
                        // IndexArray
                        for (var j = 0; j < wireStrandLineCount; j++)
                        {
                            *(indicesPtr++) = strandParticleBegin + strandParticleStride * j;
                            *(indicesPtr++) = strandParticleBegin + strandParticleStride * (j + 1);
                        }

                        // Normals
                        for (var j = strandParticleBegin; j < strandParticleEnd - strandParticleStride; j += strandParticleStride)
                        {
                            var p0 = positions[j];
                            var p1 = positions[j + strandParticleStride];

                            particleTangentPtr[j] = Vector3.Normalize(p1 - p0);
                        }

                        particleTangentPtr[strandParticleEnd - strandParticleStride] = particleTangentPtr[strandParticleEnd - 2 * strandParticleStride];

                        for (var j = strandParticleBegin; j < strandParticleEnd; j += strandParticleStride)
                        {
                            particleUVPtr[j] = new Vector2(j, i);// particle index + strand index
                        }

                        strandParticleBegin += curvePointCount;
                    }
                }

                theMesh.indexFormat = (indexArraySize > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16;
                theMesh.SetVertices(positions);
                theMesh.SetIndices(indices, MeshTopology.Lines, 0);
                theMesh.SetNormals(particleTangent);
                theMesh.SetUVs(0, particleUV);

                if (curves.Velocities.Count > 0)
                {
                    theMesh.SetUVs(5, curves.Velocities);
                }

                theMesh.RecalculateBounds();
            }
        }

        static Material GetDefaultMaterial()
        {
            return GraphicsSettings.renderPipelineAsset != null ? GraphicsSettings.renderPipelineAsset.defaultMaterial : new Material(Shader.Find("Diffuse"));
        }
    }
}
