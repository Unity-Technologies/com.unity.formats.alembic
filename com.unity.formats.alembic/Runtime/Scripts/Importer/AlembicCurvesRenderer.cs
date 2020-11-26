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
        }

        void Update()
        {
            GenerateLineMesh(mesh);
        }

        void OnDisable()
        {
            DestroyImmediate(mesh);
        }

        void GenerateLineMesh(Mesh theMesh)
        {
            var curvePointCount = curves.PositionsOffsetBuffer[1];// FIXME
            var curveCount = curves.PositionsOffsetBuffer.Count;
            var wireStrandLineCount = curvePointCount - 1;
            var wireStrandPointCount = wireStrandLineCount * 2;
            using (var particleTangent = new NativeArray<Vector3>(curveCount * curvePointCount, Allocator.Temp))
            using (var particleUV = new NativeArray<Vector2>(curveCount * curvePointCount, Allocator.Temp))
            using (var indices = new NativeArray<int>(curveCount * wireStrandPointCount, Allocator.Temp))
            {
                unsafe
                {
                    var indicesPtr = (int*)indices.GetUnsafePtr();
                    var particleTangentPtr = (Vector3*)particleTangent.GetUnsafePtr();
                    var particleUVPtr = (Vector2*)particleUV.GetUnsafePtr();

                    for (var i = 0; i < curveCount; i++)
                    {
                        DeclareStrandIterator(i, curvePointCount, out var strandParticleBegin,
                            out var strandParticleStride, out var strandParticleEnd);

                        // IndexArray
                        for (var j = 0; j < wireStrandLineCount; j++)
                        {
                            *(indicesPtr++) = strandParticleBegin + strandParticleStride * j;
                            *(indicesPtr++) = strandParticleBegin + strandParticleStride * (j + 1);
                        }

                        // Normals
                        for (var j = strandParticleBegin; j < strandParticleEnd - strandParticleStride; j += strandParticleStride)
                        {
                            var p0 = curves.Positions[j];
                            var p1 = curves.Positions[j + strandParticleStride];

                            particleTangentPtr[j] = Vector3.Normalize(p1 - p0);
                        }

                        particleTangentPtr[strandParticleEnd - strandParticleStride] = particleTangentPtr[strandParticleEnd - 2 * strandParticleStride];

                        for (var j = strandParticleBegin; j < strandParticleEnd; j += strandParticleStride)
                        {
                            particleUVPtr[j] = new Vector2(j, i);// particle index + strand index
                        }
                    }
                }

                theMesh.indexFormat = (curveCount * curvePointCount > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16;
                theMesh.SetVertices(curves.Positions);
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

        static void DeclareStrandIterator(int strandIndex, int strandParticleCount,
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
