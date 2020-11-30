using System;
using System.Collections.Generic;
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
        [SerializeField] RenderMethod renderMethod;
        [NonSerialized] RenderMethod prevRenderMethod;

        enum RenderMethod
        {
            Line,
            Strip
        }

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
            if (prevRenderMethod != renderMethod)
            {
                mesh.Clear();
            }

            prevRenderMethod = renderMethod;

            switch (renderMethod)
            {
                case RenderMethod.Line:
                    GenerateLineMesh(mesh, curves.Positions, curves.CurvePointCount);
                    break;
                case RenderMethod.Strip:
                    //GeneratePlaneMesh(mesh, new[] {new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 2, 0)}, new[] {3}, new[] {0.1f, 0.75f, 0.1f});
                    GeneratePlaneMesh(mesh, curves.Positions, curves.CurvePointCount, curves.Widths);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void OnDisable()
        {
            DestroyImmediate(mesh);
        }

        static void  GeneratePlaneMesh(Mesh theMesh, List<Vector3> positions, List<int> curveCounts, List<float> widths)
        {
            var curveCount = curveCounts.Count;

            var indexArraySize = (positions.Count - curveCount) * 4;

            using (var vertices = new NativeArray<Vector3>(positions.Count * 2 , Allocator.Temp))
            using (var indices = new NativeArray<int>(indexArraySize, Allocator.Temp))
            {
                unsafe
                {
                    var vertexPtr = (Vector3*)vertices.GetUnsafePtr();
                    var indicesPtr = (int*)indices.GetUnsafePtr();

                    var strandParticleBegin = 0;

                    var dir = new Vector3(1, 0, 0);

                    var curveStartOffset = 0;
                    for (var i = 0; i < curveCount; i++)
                    {
                        var curvePointCount = curveCounts[i];
                        for (var j = 0; j < curvePointCount; ++j)
                        {
                            var width = widths[curveStartOffset + j] / 2;
                            var p0 = positions[curveStartOffset + j] + width * dir;
                            var p1 = positions[curveStartOffset + j] - width * dir;

                            *(vertexPtr++) = p0;
                            *(vertexPtr++) = p1;
                        }

                        curveStartOffset += curvePointCount;


                        var wireStrandLineCount = (curvePointCount - 1);
                        var firstIdx = 0;
                        // IndexArray
                        for (var j = 0; j < wireStrandLineCount; j++)
                        {
                            *(indicesPtr++) = firstIdx + strandParticleBegin + j;
                            *(indicesPtr++) = firstIdx + strandParticleBegin + (j + 1);
                            *(indicesPtr++) = firstIdx + strandParticleBegin + (j + 3);
                            *(indicesPtr++) = firstIdx + strandParticleBegin + (j + 2);
                            firstIdx += 1;
                        }

                        strandParticleBegin += curvePointCount;
                    }
                }

                theMesh.indexFormat = (indexArraySize > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16;
                theMesh.SetVertices(vertices);
                theMesh.SetIndices(indices, MeshTopology.Quads, 0);


                theMesh.RecalculateBounds();
                theMesh.RecalculateNormals();
            }
        }

        void GenerateLineMesh(Mesh theMesh, List<Vector3> positions, List<int> curveCounts)
        {
            var curveCount = curveCounts.Count;

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
