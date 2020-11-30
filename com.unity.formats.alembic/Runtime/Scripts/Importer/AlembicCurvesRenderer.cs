using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
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
        ProfilerMarker setMeshProperties = new ProfilerMarker("SetMeshProperties");


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
                    GenerateLineMesh(mesh, curves.positionsList.GetArray(), curves.curvePointCount.GetArray());
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

        void GeneratePlaneMesh(Mesh theMesh, List<Vector3> positions, List<int> curveCounts, List<float> widths)
        {
            var curveCount = curveCounts.Count;

            var indexArraySize = (positions.Count - curveCount) * 4;

            using (var vertices = new NativeArray<Vector3>(positions.Count * 2, Allocator.Temp))
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
                            var d = widths[curveStartOffset + j] / 2 * dir; // displacement
                            var p0 = positions[curveStartOffset + j] + d;
                            var p1 = positions[curveStartOffset + j] - d;

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

        void GenerateLineMesh(Mesh theMesh, Vector3[] positionsM, int[] curvesCountsM)
        {
            var curveCount = curvesCountsM.Length;
            var indexArraySize = (positionsM.Length - curvesCountsM.Length) * 2;


            using (var curveCounts = new NativeArray<int>(curvesCountsM.Length, Allocator.TempJob))
            using (var vertices = new NativeArray<Vector3>(positionsM.Length, Allocator.TempJob))
            using (var strideArray = new NativeArray<int>(curvesCountsM.Length, Allocator.TempJob))
            using (var particleTangent = new NativeArray<Vector3>(positionsM.Length, Allocator.TempJob))
            using (var particleUV = new NativeArray<Vector2>(positionsM.Length, Allocator.TempJob))
            using (var indices = new NativeArray<int>(indexArraySize, Allocator.TempJob))
            {
                vertices.CopyFrom(positionsM);
                curveCounts.CopyFrom(curvesCountsM);
                var cnt = 0;
                for (var i = 0; i < strideArray.Length; ++i)
                {
                    var nativeArray = strideArray;
                    nativeArray[i] = cnt;
                    cnt += curveCounts[i];
                }

                var job = new GenerateLinesJob
                {
                    indices = indices,
                    curveCounts = curveCounts,
                    strideArray = strideArray,
                    particleTangent = particleTangent,
                    vertices = vertices,
                    particleUV = particleUV
                };

                job.Schedule(curveCount, 1).Complete();

                using (setMeshProperties.Auto())
                {
                    theMesh.indexFormat = (indexArraySize > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16;
                    theMesh.SetVertices(vertices);
                    theMesh.SetIndices(indices, MeshTopology.Lines, 0);
                    theMesh.SetNormals(particleTangent);
                    theMesh.SetUVs(0, particleUV);

                    if (curves.Velocities.Count > 0)
                    {
                        theMesh.SetUVs(5, curves.Velocities);
                    }
                    theMesh.RecalculateBounds();
                    theMesh.Optimize();
                }
            }
        }

        struct GenerateLinesJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<int> indices;
            [WriteOnly] public NativeArray<Vector3> particleTangent;
            [WriteOnly] public NativeArray<Vector2> particleUV;
            [Unity.Collections.ReadOnly] public NativeArray<int> curveCounts, strideArray;
            [Unity.Collections.ReadOnly] public NativeArray<Vector3> vertices;


            public void Execute(int curveIdx)
            {
                unsafe
                {
                    var indicesPtr = (int*) indices.GetUnsafePtr();
                    var particleTangentPtr = (Vector3*) particleTangent.GetUnsafePtr();
                    var particleUVPtr = (Vector2*) particleUV.GetUnsafePtr();

                    var curvePointCount = curveCounts[curveIdx];
                    var strandParticleBegin = strideArray[curveIdx];
                    var strandParticleEnd = strandParticleBegin + curvePointCount;

                    var wireStrandLineCount = (curvePointCount - 1);
                    // IndexArray
                    for (var j = 0; j < wireStrandLineCount; j++)
                    {
                        var idx = (strideArray[curveIdx] - curveIdx + j) * 2;
                        indicesPtr[idx] = strandParticleBegin + j;
                        indicesPtr[idx + 1] = strandParticleBegin + j + 1;
                    }

                    // Normals
                    for (var j = strandParticleBegin; j < strandParticleEnd - 1; j += 1)
                    {
                        var p0 = vertices[j];
                        var p1 = vertices[j + 1];

                        particleTangentPtr[j] = Vector3.Normalize(p1 - p0);
                    }

                    particleTangentPtr[strandParticleEnd - 1] = particleTangentPtr[strandParticleEnd - 2];
                    
                    for (var j = strandParticleBegin; j < strandParticleEnd; j += 1)
                    {
                        particleUVPtr[j] = new Vector2(j, curveIdx); // particle index + strand index
                    }
                }
            }
        }

        static Material GetDefaultMaterial()
        {
            return GraphicsSettings.renderPipelineAsset != null ? GraphicsSettings.renderPipelineAsset.defaultMaterial : new Material(Shader.Find("Diffuse"));
        }
    }
}
