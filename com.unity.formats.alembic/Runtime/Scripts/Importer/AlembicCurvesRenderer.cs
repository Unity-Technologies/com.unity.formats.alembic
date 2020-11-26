using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Rendering;

namespace Scripts.Importer
{
    [RequireComponent(typeof(AlembicCurves))]
    [ExecuteInEditMode]
    public class AlembicCurvesRenderer : MonoBehaviour
    {
        AlembicCurves curves;
        Mesh mesh;

        [SerializeField] Material material;

        void OnEnable()
        {
            curves = GetComponent<AlembicCurves>();
            mesh = new Mesh();
        }

        void Update()
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
            var curvePointCount = 16;// FIXME
            var curveCount = curves.PositionsOffsetBuffer.Count;
            var wireStrandLineCount = curvePointCount - 1;
            var wireStrandPointCount = wireStrandLineCount * 2;
            using (var particleTangent = new NativeArray<Vector3>(curveCount * curvePointCount, Allocator.Temp))
            using (var indices = new NativeArray<int>(curveCount * wireStrandPointCount, Allocator.Temp))
            {
                unsafe
                {
                    var indicesPtr = (int*)indices.GetUnsafePtr();
                    var particleTangentPtr = (Vector3*)particleTangent.GetUnsafePtr();
                    for (var i = 0; i < curveCount; i++)
                    {
                        DeclareStrandIterator(i, curveCount, curvePointCount, out var strandParticleBegin,
                            out var strandParticleStride, out _);

                        for (var j = 0; j < wireStrandLineCount; j++)
                        {
                            *(indicesPtr++) = strandParticleBegin + strandParticleStride * j;
                            *(indicesPtr++) = strandParticleBegin + strandParticleStride * (j + 1);
                        }
                    }

                    for (var i = 0; i < curveCount; i++)
                    {
                        DeclareStrandIterator(i, curveCount, curvePointCount, out var strandParticleBegin,
                            out var strandParticleStride, out var strandParticleEnd);
                        for (var j = strandParticleBegin; j < strandParticleEnd - strandParticleStride; j += strandParticleStride)
                        {
                            var p0 = curves.Positions[j];
                            var p1 = curves.Positions[j + strandParticleStride];

                            particleTangentPtr[j] = Vector3.Normalize(p1 - p0);
                        }

                        particleTangentPtr[strandParticleEnd - strandParticleStride] = particleTangentPtr[strandParticleEnd - 2 * strandParticleStride];
                    }
                }

                mesh.indexFormat = (curveCount * curvePointCount > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(curves.Positions);
                mesh.SetIndices(indices, MeshTopology.Lines, 0);
                mesh.SetNormals(particleTangent);
                mesh.RecalculateBounds();
            }
            Graphics.DrawMesh(mesh, transform.position, transform.rotation, material, 0);
        }

        void OnDisable()
        {
            DestroyImmediate(mesh);
        }

        public static void DeclareStrandIterator(int strandIndex, int strandCount, int strandParticleCount,
            out int strandParticleBegin,
            out int strandParticleStride,
            out int strandParticleEnd)
        {
            strandParticleBegin = strandIndex * strandParticleCount;
            strandParticleStride = 1;
            strandParticleEnd = strandParticleBegin + strandParticleStride * strandParticleCount;
        }
    }
}
