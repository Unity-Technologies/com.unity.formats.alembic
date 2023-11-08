using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

namespace UnityEditor.Formats.Alembic.Importer.MeshSchema
{
    public class MeshSchemaTests
    {
        static readonly Dictionary<string, List<Color>> k_VertexRgbScopeTestData = new Dictionary<string, List<Color>>()
        {
            { "cube_face", new List<Color>
            {
                new Color(0.136873f, 0.570807f, 0.034585f, 1.0f),
                new Color(0.136873f, 0.570807f, 0.034585f, 1.0f),
                new Color(0.136873f, 0.570807f, 0.034585f, 1.0f),
                new Color(0.136873f, 0.570807f, 0.034585f, 1.0f),
                new Color(0.807873f, 0.292345f, 0.0282118f, 1.0f),
                new Color(0.807873f, 0.292345f, 0.0282118f, 1.0f),
                new Color(0.807873f, 0.292345f, 0.0282118f, 1.0f),
                new Color(0.807873f, 0.292345f, 0.0282118f, 1.0f),
                new Color(0.638699f, 0.320457f, 0.676918f, 1.0f),
                new Color(0.638699f, 0.320457f, 0.676918f, 1.0f),
                new Color(0.638699f, 0.320457f, 0.676918f, 1.0f),
                new Color(0.638699f, 0.320457f, 0.676918f, 1.0f),
                new Color(0.849678f, 0.0295019f, 0.185701f, 1.0f),
                new Color(0.849678f, 0.0295019f, 0.185701f, 1.0f),
                new Color(0.849678f, 0.0295019f, 0.185701f, 1.0f),
                new Color(0.849678f, 0.0295019f, 0.185701f, 1.0f),
                new Color(0.410384f, 0.133891f, 0.293169f, 1.0f),
                new Color(0.410384f, 0.133891f, 0.293169f, 1.0f),
                new Color(0.410384f, 0.133891f, 0.293169f, 1.0f),
                new Color(0.410384f, 0.133891f, 0.293169f, 1.0f),
                new Color(0.0560673f, 0.955198f, 0.474951f, 1.0f),
                new Color(0.0560673f, 0.955198f, 0.474951f, 1.0f),
                new Color(0.0560673f, 0.955198f, 0.474951f, 1.0f),
                new Color(0.0560673f, 0.955198f, 0.474951f, 1.0f)
            }},
            { "cube_point", new List<Color>
            {
                new Color(0.807873f, 0.292345f, 0.0282118f, 1.0f),
                new Color(0.0560673f, 0.955198f, 0.474951f, 1.0f),
                new Color(0.410384f, 0.133891f, 0.293169f, 1.0f),
                new Color(0.136873f, 0.570807f, 0.034585f, 1.0f),
                new Color(0.638699f, 0.320457f, 0.676918f, 1.0f),
                new Color(0.262381f, 0.579069f, 0.83773f, 1.0f),
                new Color(0.849678f, 0.0295019f, 0.185701f, 1.0f),
                new Color( 0.901355f, 0.815292f, 0.613544f, 1.0f)
            }},
            { "cube_vertex", new List<Color>
            {
                new Color(0.136873f, 0.570807f, 0.034585f, 1.0f),
                new Color(0.807873f, 0.292345f, 0.0282118f, 1.0f),
                new Color(0.638699f, 0.320457f, 0.676918f, 1.0f),
                new Color(0.849678f, 0.0295019f, 0.185701f, 1.0f),
                new Color(0.410384f, 0.133891f, 0.293169f, 1.0f),
                new Color(0.0560673f, 0.955198f, 0.474951f, 1.0f),
                new Color(0.262381f, 0.579069f, 0.83773f, 1.0f),
                new Color(0.901355f, 0.815292f, 0.613544f, 1.0f),
                new Color(0.745016f, 0.696294f, 0.140825f, 1.0f),
                new Color(0.689646f, 0.156375f, 0.77343f, 1.0f),
                new Color(0.931242f, 0.517226f, 0.498477f, 1.0f),
                new Color(0.055582f, 0.523521f, 0.286915f, 1.0f),
                new Color(0.449092f, 0.111865f, 0.48572f, 1.0f),
                new Color(0.901333f, 0.178653f, 0.730107f, 1.0f),
                new Color(0.054878f, 0.691018f, 0.0120219f, 1.0f),
                new Color(0.396657f, 0.233593f, 0.736785f, 1.0f),
                new Color(0.591222f, 0.90485f, 0.211925f, 1.0f),
                new Color(0.839599f, 0.116205f, 0.662651f, 1.0f),
                new Color(0.861743f, 0.252549f, 0.578756f, 1.0f),
                new Color(0.786967f, 0.518664f, 0.646041f, 1.0f),
                new Color(0.417679f, 0.909779f, 0.213722f, 1.0f),
                new Color(0.206854f, 0.819972f, 0.330694f, 1.0f),
                new Color(0.733233f, 0.0665461f, 0.0174288f, 1.0f),
                new Color(0.242886f, 0.790562f, 0.235412f, 1.0f)
            }}
        };

        static readonly Dictionary<string, List<Color>> k_VertexRgbaScopeTestData = new Dictionary<string, List<Color>>()
        {
            { "face_grid", new List<Color>
            {
                new Color(0.123443f, 0.316637f, 0.231753f, 0.522791f),
                new Color(0.123443f, 0.316637f, 0.231753f, 0.522791f),
                new Color(0.123443f, 0.316637f, 0.231753f, 0.522791f),
                new Color(0.123443f, 0.316637f, 0.231753f, 0.522791f),
                new Color(0.845502f, 0.209966f, 0.239808f, 0.466761f),
                new Color(0.845502f, 0.209966f, 0.239808f, 0.466761f),
                new Color(0.845502f, 0.209966f, 0.239808f, 0.466761f),
                new Color(0.845502f, 0.209966f, 0.239808f, 0.466761f),
            }},
            { "point_grid", new List<Color>
            {
                new Color(0.839161f, 0.152978f, 0.577921f, 0.444734f),
                new Color(0.639696f, 0.252602f, 0.571499f, 0.656393f),
                new Color(0.822681f, 0.982065f, 0.645432f, 0.907562f),
                new Color(0.645021f, 0.330725f, 0.396846f, 0.165953f),
                new Color(0.979039f, 0.845975f, 0.0268883f, 0.969148f),
                new Color(0.171811f, 0.407435f, 0.896118f, 0.831778f)
            }},
            { "vertex_grid", new List<Color>
            {
                new Color(0.699117f, 0.283595f, 0.844144f, 0.944193f),
                new Color(0.954809f, 0.0829239f, 0.5548f, 0.315697f),
                new Color(0.1405f, 0.60224f, 0.242197f, 0.642945f),
                new Color(0.461596f, 0.0950942f, 0.56585f, 0.176854f),
                new Color(0.119768f, 0.956445f, 0.342202f, 0.80795f),
                new Color(0.655877f, 0.981544f, 0.658537f, 0.57736f),
                new Color(0.0532411f, 0.201442f, 0.183963f, 0.996724f),
                new Color(0.970232f, 0.646445f, 0.934467f, 0.718449f)
            }}
        };

        static readonly Dictionary<string, List<Vector2>> k_VertexUVScopeTestData = new Dictionary<string, List<Vector2>>()
        {
            { "face_uv_grid", new List<Vector2>
            {
                new Vector2(0.150535f, 0.70265f),
                new Vector2(0.150535f, 0.70265f),
                new Vector2(0.150535f, 0.70265f),
                new Vector2(0.150535f, 0.70265f),
                new Vector2(0.532512f, 0.736863f),
                new Vector2(0.532512f, 0.736863f),
                new Vector2(0.532512f, 0.736863f),
                new Vector2(0.532512f, 0.736863f)
            }},
            { "point_uv_grid", new List<Vector2>
            {
                new Vector2(0.839161f, 0.152978f),
                new Vector2(0.639696f, 0.252602f),
                new Vector2(0.645021f, 0.330725f),
                new Vector2(0.979039f, 0.845975f),
                new Vector2(0.171811f, 0.407435f),
                new Vector2(0.822681f, 0.982065f)

            }},
            { "vertex_uv_grid", new List<Vector2>
            {
                new Vector2(0.868606f, 0.457556f),
                new Vector2(0.515431f, 0.508874f),
                new Vector2(0.0333328f, 0.798657f),
                new Vector2(0.0986392f, 0.53505f),
                new Vector2(0.737957f, 0.512172f),
                new Vector2(0.892787f, 0.0975446f),
                new Vector2(0.68879f, 0.285097f),
                new Vector2(0.800312f, 0.687608f)
            }}
        };

static readonly Dictionary<string, List<Vector3>> k_VertexNormalScopeTestData = new Dictionary<string, List<Vector3>>()
        {
            { "face_normal_grid", new List<Vector3>
            {
                new Vector3(0,0,1),
                new Vector3(0,0,1),
                new Vector3(0,0,1),
                new Vector3(0,0,1),
                new Vector3(0, 0, -1),
                new Vector3(0, 0, -1),
                new Vector3(0, 0, -1),
                new Vector3(0, 0, -1),
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, -1, 0),
                new Vector3(0, -1, 0),
                new Vector3(0, -1, 0),
                new Vector3(0, -1, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(-1, 0, 0),
                new Vector3(-1, 0, 0),
                new Vector3(-1, 0, 0),
                new Vector3(-1, 0, 0)
            }},
            { "point_normal_grid", new List<Vector3>
            {
                new Vector3(0.57735f, -0.57735f, 0.57735f),
                new Vector3( -0.57735f, -0.57735f, 0.57735f),
                new Vector3(  -0.57735f, 0.57735f, 0.57735f),
                new Vector3( 0.57735f, 0.57735f, 0.57735f),
                new Vector3(-0.57735f, -0.57735f, -0.57735f),
                new Vector3(0.57735f, -0.57735f, -0.57735f),
                new Vector3(0.57735f, 0.57735f, -0.57735f),
                new Vector3(-0.57735f, 0.57735f, -0.57735f)
            }},
            { "vertex_normal_grid", new List<Vector3>
            {
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, -1),
                new Vector3(0, 0, -1),
                new Vector3(0, 0, -1),
                new Vector3(0, 0, -1),
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, -1, 0),
                new Vector3(0, -1, 0),
                new Vector3(0, -1, 0),
                new Vector3(0, -1, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(-1, 0, 0),
                new Vector3(-1, 0, 0),
                new Vector3(-1, 0, 0),
                new Vector3(-1, 0, 0)
            }}
        };

        [Test]
        [TestCase("cube_face")]
        [TestCase("cube_point")]
        [TestCase("cube_vertex")]
        public void VertexRgb_AreProcessedCorrectlyForScope(string scope)
        {
            string guid = "dd3554fc098614b9e99b49873fe18cd6"; // cubes_coloured.abc
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject.Instantiate(meshPrefab); // needed to add the mesh filter component

            var meshFilter = GameObject.Find(scope).GetComponentInChildren<MeshFilter>();

            var expectedColors = k_VertexRgbScopeTestData[scope];

            for (int i = 0; i < expectedColors.Count; i++)
            {
                var meshColor = meshFilter.sharedMesh.colors[i];

                Assert.IsTrue(meshColor == expectedColors[i],
                        $"Scope: {scope}, Expected: {expectedColors[i]}, But was: {meshColor}");
            }
        }

        [Test]
        [TestCase("face_grid")]
        [TestCase("point_grid")]
        [TestCase("vertex_grid")]
        public void VertexRgba_AreProcessedCorrectlyForScope(string scope)
        {
            string guid = "8e71ed6608e0b984b8b90d6ea71b11eb"; // rgba_grid.abc
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject.Instantiate(meshPrefab); // needed to add the mesh filter component

            var meshFilter = GameObject.Find(scope).GetComponentInChildren<MeshFilter>();

            var expectedColors = k_VertexRgbaScopeTestData[scope];

            for (int i = 0; i < expectedColors.Count; i++)
            {
                var meshColor = meshFilter.sharedMesh.colors[i];

                Assert.IsTrue(meshColor == expectedColors[i],
                        $"Scope: {scope}, Expected: {expectedColors[i]}, But was: {meshColor}");
            }
        }

        [Test]
        [TestCase("face_uv_grid")]
        [TestCase("point_uv_grid")]
        [TestCase("vertex_uv_grid")]
        public void VertexUV_AreProcessedCorrectlyForScope(string scope)
        {
            string guid = "eba4f4e4413e7ea48a63bd27ddf3a329"; // uv_grid.abc
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject.Instantiate(meshPrefab); // needed to add the mesh filter component

            var meshFilter = GameObject.Find(scope).GetComponentInChildren<MeshFilter>();

            var expectedUV = k_VertexUVScopeTestData[scope];

            for (int i = 0; i < expectedUV.Count; i++)
            {
                var meshUV = meshFilter.sharedMesh.uv[i];

                Assert.IsTrue(meshUV == expectedUV[i],
                    $"Scope: {scope}, Expected: {expectedUV[i]}, But was: {meshUV}");
            }
        }

        [Test]
        [TestCase("face_normal_grid")]
        [TestCase("point_normal_grid")]
        [TestCase("vertex_normal_grid")]
        public void VertexNormals_AreProcessedCorrectlyForScope(string scope)
        {
            string guid = "39e35f70a7d718e4aa4cafea6714d5e3"; // cubes_normals.abc

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var abc = meshPrefab.GetComponent<AlembicStreamPlayer>();

            if(abc.Settings.SwapHandedness)
               abc.Settings.SwapHandedness = false;

            GameObject.Instantiate(meshPrefab); // needed to add the mesh filter component
            var meshFilter = GameObject.Find(scope).GetComponentInChildren<MeshFilter>();

            var expectedNormal = k_VertexNormalScopeTestData[scope];

            for (int i = 0; i < expectedNormal.Count; i++)
            {
                var meshNormal = meshFilter.sharedMesh.normals[i];

                Assert.IsTrue(meshNormal == expectedNormal[i],
                    $"Scope: {scope}, Expected: {expectedNormal[i]}, But was: {meshNormal}");
            }
        }
    }
}

