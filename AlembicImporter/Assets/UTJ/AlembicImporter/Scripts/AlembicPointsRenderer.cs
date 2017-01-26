using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UTJ.Alembic
{
	[ExecuteInEditMode]
	[AddComponentMenu("UTJ/Alembic/Points Renderer")]
	[RequireComponent(typeof(AlembicPointsCloud))]
	public class AlembicPointsRenderer : MonoBehaviour
	{
		const int TextureWidth = 2048;

		public bool m_makeChildRenderers = true;
		public Mesh m_mesh;
		public Material[] m_materials;
		public bool m_cast_shadow = false;
		public bool m_receive_shadow = false;
		public float m_count_rate = 1.0f;
		public Vector3 m_model_scale = Vector3.one;
		public Vector3 m_trans_scale = Vector3.one;
#if UNITY_EDITOR
		public bool m_show_bounds = false;
#endif

		int m_instances_par_batch;
		int m_layer;
		Mesh m_expanded_mesh;
		Bounds m_bounds;
		List<List<Material>> m_actual_materials;
		[HideInInspector][SerializeField]

		RenderTexture m_texPositions;
		RenderTexture m_texVelocities;
		RenderTexture m_texIDs;

		#region static

		public static int ceildiv(int v, int d)
		{
			return v / d + (v % d == 0 ? 0 : 1);
		}

		public static Vector3 mul(Vector3 a, Vector3 b)
		{
			return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
		}

		static RenderTexture CreateDataTexture(int w, int h, RenderTextureFormat f)
		{
			RenderTexture r = new RenderTexture(w, h, 0, f);
			r.filterMode = FilterMode.Point;
			r.useMipMap = false;
			r.autoGenerateMips = false;
			r.Create();
			return r;
		}

		public const int MaxVertices = 65000; // Mesh's limitation

		public static Mesh CreateExpandedMesh(Mesh mesh, int required_instances, out int instances_par_batch)
		{
			Vector3[] vertices_base = mesh.vertices;
			Vector3[] normals_base = (mesh.normals == null || mesh.normals.Length == 0) ? null : mesh.normals;
			Vector4[] tangents_base = (mesh.tangents == null || mesh.tangents.Length == 0) ? null : mesh.tangents;
			Vector2[] uv_base = (mesh.uv == null || mesh.uv.Length == 0) ? null : mesh.uv;
			Color[] colors_base = (mesh.colors == null || mesh.colors.Length == 0) ? null : mesh.colors;
			int[] indices_base = (mesh.triangles == null || mesh.triangles.Length == 0) ? null : mesh.triangles;
			instances_par_batch = Mathf.Min(MaxVertices / mesh.vertexCount, required_instances);

			Vector3[] vertices = new Vector3[vertices_base.Length * instances_par_batch];
			Vector2[] idata = new Vector2[vertices_base.Length * instances_par_batch];
			Vector3[] normals = normals_base == null ? null : new Vector3[normals_base.Length * instances_par_batch];
			Vector4[] tangents = tangents_base == null ? null : new Vector4[tangents_base.Length * instances_par_batch];
			Vector2[] uv = uv_base == null ? null : new Vector2[uv_base.Length * instances_par_batch];
			Color[] colors = colors_base == null ? null : new Color[colors_base.Length * instances_par_batch];
			int[] indices = indices_base == null ? null : new int[indices_base.Length * instances_par_batch];

			for (int ii = 0; ii < instances_par_batch; ++ii)
			{
				for (int vi = 0; vi < vertices_base.Length; ++vi)
				{
					int i = ii * vertices_base.Length + vi;
					vertices[i] = vertices_base[vi];
					idata[i] = new Vector2((float)ii, (float)vi);
				}
				if (normals != null)
				{
					for (int vi = 0; vi < normals_base.Length; ++vi)
					{
						int i = ii * normals_base.Length + vi;
						normals[i] = normals_base[vi];
					}
				}
				if (tangents != null)
				{
					for (int vi = 0; vi < tangents_base.Length; ++vi)
					{
						int i = ii * tangents_base.Length + vi;
						tangents[i] = tangents_base[vi];
					}
				}
				if (uv != null)
				{
					for (int vi = 0; vi < uv_base.Length; ++vi)
					{
						int i = ii * uv_base.Length + vi;
						uv[i] = uv_base[vi];
					}
				}
				if (colors != null)
				{
					for (int vi = 0; vi < colors_base.Length; ++vi)
					{
						int i = ii * colors_base.Length + vi;
						colors[i] = colors_base[vi];
					}
				}
				if (indices != null)
				{
					for (int vi = 0; vi < indices_base.Length; ++vi)
					{
						int i = ii * indices_base.Length + vi;
						indices[i] = ii * vertices_base.Length + indices_base[vi];
					}
				}

			}
			Mesh ret = new Mesh();
			ret.vertices = vertices;
			ret.normals = normals;
			ret.tangents = tangents;
			ret.uv = uv;
			ret.colors = colors;
			ret.uv2 = idata;
			ret.triangles = indices;
			return ret;
		}

		#endregion

		Material CloneMaterial(Material src, int nth)
		{
			Material m = new Material(src);
			m.SetInt("_BatchBegin", nth * m_instances_par_batch);
			m.SetTexture("_PositionBuffer", m_texPositions);
			m.SetTexture("_VelocityBuffer", m_texVelocities);
			m.SetTexture("_IDBuffer", m_texIDs);

			// fix rendering order for transparent objects
			if (m.renderQueue >= 3000)
			{
				m.renderQueue = m.renderQueue + (nth + 1);
			}
			return m;
		}

		public void RefleshMaterials()
		{
			m_actual_materials = null;
			Flush();
		}

		public void Flush()
		{
			if (m_mesh == null)
			{
				Debug.LogWarning("AlembicPointsRenderer: mesh is not assigned");
				return;
			}
			if (m_materials == null || m_materials.Length == 0 || (m_materials.Length == 1 && m_materials[0] == null))
			{
				Debug.LogWarning("AlembicPointsRenderer: material is not assigned");
				return;
			}

			var pointsCloud = GetComponent<AlembicPointsCloud>();
			int max_instances = pointsCloud.m_peakVertexCount;
			int instance_count = pointsCloud.m_count;
			m_bounds.center = mul(pointsCloud.m_boundsCenter, m_trans_scale);
			m_bounds.extents = mul(pointsCloud.m_boundsExtents, m_trans_scale);

			if (instance_count == 0) { return; } // nothing to draw

			// update data texture
			if (m_texPositions == null || !m_texPositions.IsCreated())
			{
				int height = ceildiv(max_instances, TextureWidth);
				m_texPositions = CreateDataTexture(TextureWidth, height, RenderTextureFormat.ARGBFloat);
				m_texVelocities = CreateDataTexture(TextureWidth, height, RenderTextureFormat.ARGBFloat);
				m_texIDs = CreateDataTexture(TextureWidth, height, RenderTextureFormat.RFloat);
			}

			var ps = Marshal.UnsafeAddrOfPinnedArrayElement(pointsCloud.m_abcPositions, 0);
			var ids = Marshal.UnsafeAddrOfPinnedArrayElement(pointsCloud.m_abcIDs, 0);

			TextureWriter.Write(m_texPositions, ps, pointsCloud.m_count, TextureWriter.tDataFormat.Float3);
			TextureWriter.Write(m_texIDs, ids, pointsCloud.m_count, TextureWriter.tDataFormat.LInt);

			// update expanded mesh
			if (m_expanded_mesh != null)
			{
				// destroy existing expanded mesh if mesh is replaced
				if (m_expanded_mesh.name != m_mesh.name + "_expanded")
				{
					m_expanded_mesh = null;
				}
			}
			if (m_expanded_mesh == null)
			{
				m_expanded_mesh = CreateExpandedMesh(m_mesh, max_instances, out m_instances_par_batch);
				m_expanded_mesh.UploadMeshData(true);
				m_expanded_mesh.name = m_mesh.name + "_expanded";
			}

			if (m_actual_materials == null)
			{
				m_actual_materials = new List<List<Material>>();
				while (m_actual_materials.Count < m_materials.Length)
				{
					m_actual_materials.Add(new List<Material>());
				}
			}

			var trans = GetComponent<Transform>();
			m_expanded_mesh.bounds = m_bounds;
			m_count_rate = Mathf.Max(m_count_rate, 0.0f);
			instance_count = Mathf.Min((int)(instance_count * m_count_rate), (int)(max_instances * m_count_rate));
			int batch_count = ceildiv(instance_count, m_instances_par_batch);

			// clone materials if needed
			for (int i = 0; i < m_actual_materials.Count; ++i)
			{
				var materials = m_actual_materials[i];
				while (materials.Count < batch_count)
				{
					Material m = CloneMaterial(m_materials[i], materials.Count);
					materials.Add(m);
				}
			}

			// update materials
			var worldToLocalMatrix = trans.localToWorldMatrix;
			for (int i = 0; i < m_actual_materials.Count; ++i)
			{
				var materials = m_actual_materials[i];
				for (int j = 0; j < materials.Count; ++j)
				{
					var m = materials[j];
					m.SetInt("_BatchBegin", j * m_instances_par_batch);
					m.SetTexture("_PositionBuffer", m_texPositions);
					m.SetTexture("_VelocityBuffer", m_texVelocities);
					m.SetTexture("_IDBuffer", m_texIDs);

					m.SetInt("_NumInstances", instance_count);
					m.SetVector("_CountRate", new Vector4(m_count_rate, 1.0f / m_count_rate, 0.0f, 0.0f));
					m.SetVector("_ModelScale", m_model_scale);
					m.SetVector("_TransScale", m_trans_scale);
					m.SetMatrix("_Transform", worldToLocalMatrix);
				}
			}


			var childRenderers = transform.GetComponentsInChildren<MeshRenderer>(); 

			if (m_makeChildRenderers)
			{
				// create child renderers if needed
				int missing = m_materials.Length;
				if (childRenderers != null)
					missing -= childRenderers.Length;
				if (missing > 0)
				{
					for( int i =0;  i < missing; i++)
						MakeChildRenderer(childRenderers.Length + i);
					childRenderers = transform.GetComponentsInChildren<MeshRenderer>();
				}

				// assign materials to child renderers
				for (int i = 0; i < m_actual_materials.Count; ++i)
				{
					var materials = m_actual_materials[i];
					materials.RemoveRange(batch_count, materials.Count - batch_count);
					childRenderers[i].sharedMaterials = materials.ToArray();
				}

				// update mesh in child renderers if m_mesh is replaced
				for (int i = 0; i < childRenderers.Length; ++i)
				{
					var mesh_filter = childRenderers[i].GetComponent<MeshFilter>();
					if (mesh_filter.sharedMesh != m_expanded_mesh)
					{
						mesh_filter.sharedMesh = m_expanded_mesh;
					}
				}
			}
			else
			{
				// clear child renderers
				if (childRenderers != null)
				{
					foreach (var child in childRenderers)
					{
						if (child != null)
						{
							DestroyImmediate(child.gameObject);
						}
					}
					childRenderers = null;
				}

				// issue draw calls
				int layer = gameObject.layer;
				Matrix4x4 matrix = Matrix4x4.identity;
				m_actual_materials.ForEach(a =>
				{
					for (int i = 0; i < batch_count; ++i)
					{
						Graphics.DrawMesh(m_expanded_mesh, matrix, a[i], layer, null, 0, null, m_cast_shadow, m_receive_shadow);
					}
				});
			}
		}

		MeshRenderer MakeChildRenderer(int i)
		{
			var child = new GameObject();
			var transform = child.GetComponent<Transform>();
			var filter = child.AddComponent<MeshFilter>();
			var renderer = child.AddComponent<MeshRenderer>();

			child.name = "MeshRenderer[" + i + "]";
			transform.SetParent(GetComponent<Transform>());
			filter.sharedMesh = m_expanded_mesh;
			return renderer;
		}

		void ReleaseGPUResoureces()
		{
			if (m_actual_materials != null)
			{
				m_actual_materials.ForEach(a => { a.Clear(); });
			}
			if (m_texPositions != null)
			{
				m_texPositions.Release();
				m_texPositions = null;
			}
			if (m_texVelocities != null)
			{
				m_texVelocities.Release();
				m_texVelocities = null;
			}
			if (m_texIDs != null)
			{
				m_texIDs.Release();
				m_texIDs = null;
			}
			m_bounds = new Bounds();
		}

#if UNITY_EDITOR
		void Reset()
		{
			// IcoSphere.asset
			m_mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath("b63f02850eb90a641b0e2db0da7e9e74"));
			m_materials = new Material[1] {
                // AlembicPointsDefault.mat
                AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("5dfafd6734133bb4b9012fba0eadd4af"))
			};
			ReleaseGPUResoureces();
		}

		void OnValidate()
		{
			ReleaseGPUResoureces();
		}
#endif

		void OnDisable()
		{
			ReleaseGPUResoureces();
		}

		void LateUpdate()
		{
			Flush();
		}

#if UNITY_EDITOR
		void OnDrawGizmos()
		{
			if (m_show_bounds)
			{
				Gizmos.matrix = GetComponent<Transform>().localToWorldMatrix;
				Gizmos.color = Color.yellow;
				Gizmos.DrawWireCube(m_bounds.center, m_bounds.extents);
			}
		}
#endif
	}
}
