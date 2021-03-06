﻿using UnityEngine;

namespace GameScripts
{
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class CubeSphere : MonoBehaviour {

		private int gridSize = 104;

		private float radius = 1f;

		private Mesh mesh;
		//private MeshFilter mf;
		private Vector3[] vertices;
		private Vector3[] normals;
		private Vector2[] uvs;

		public Mesh Generate (int i) {
			mesh = new Mesh();
			mesh.name = "Procedural_Sphere_" + i;
			CreateVertices();
			CreateTriangles();
			//CreateColliders();

			return mesh;
		}

		private void CreateVertices () {
			int cornerVertices = 8;
			int edgeVertices = (gridSize + gridSize + gridSize - 3) * 4;
			int faceVertices = (
				                   (gridSize - 1) * (gridSize - 1) +
				                   (gridSize - 1) * (gridSize - 1) +
				                   (gridSize - 1) * (gridSize - 1)) * 2;
			vertices = new Vector3[cornerVertices + edgeVertices + faceVertices];
			normals = new Vector3[vertices.Length];
			uvs = new Vector2[vertices.Length];

			int v = 0;
			for (int y = 0; y <= gridSize; y++) {
				for (int x = 0; x <= gridSize; x++) {
					SetVertex(v++, x, y, 0);
				}
				for (int z = 1; z <= gridSize; z++) {
					SetVertex(v++, gridSize, y, z);
				}
				for (int x = gridSize - 1; x >= 0; x--) {
					SetVertex(v++, x, y, gridSize);
				}
				for (int z = gridSize - 1; z > 0; z--) {
					SetVertex(v++, 0, y, z);
				}
			}
			for (int z = 1; z < gridSize; z++) {
				for (int x = 1; x < gridSize; x++) {
					SetVertex(v++, x, gridSize, z);
				}
			}
			for (int z = 1; z < gridSize; z++) {
				for (int x = 1; x < gridSize; x++) {
					SetVertex(v++, x, 0, z);
				}
			}

			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.uv = uvs;
		}

		private void SetVertex (int i, int x, int y, int z) {
			Vector3 v = new Vector3(x, y, z) * 2f / gridSize - Vector3.one;
			float x2 = v.x * v.x;
			float y2 = v.y * v.y;
			float z2 = v.z * v.z;
			Vector3 s;
			s.x = v.x * Mathf.Sqrt(1f - y2 / 2f - z2 / 2f + y2 * z2 / 3f);
			s.y = v.y * Mathf.Sqrt(1f - x2 / 2f - z2 / 2f + x2 * z2 / 3f);
			s.z = v.z * Mathf.Sqrt(1f - x2 / 2f - y2 / 2f + x2 * y2 / 3f);
			normals[i] = s;
			vertices[i] = normals[i] * radius;
			var r = Mathf.Sqrt(vertices[i].x * vertices[i].x + vertices[i].y * vertices[i].y + vertices[i].z * vertices[i].z);        
			uvs[i] = new Vector2(Mathf.Atan2(vertices[i].z, vertices[i].x) / Mathf.PI / 2, Mathf.Acos(vertices[i].y / r) / Mathf.PI);
       
		}

		private void CreateTriangles () {
			int[] trianglesZ = new int[(gridSize * gridSize) * 12];
			int[] trianglesX = new int[(gridSize * gridSize) * 12];
			int[] trianglesY = new int[(gridSize * gridSize) * 12];
			int ring = (gridSize + gridSize) * 2;
			int tZ = 0, tX = 0, tY = 0, v = 0;

			for (int y = 0; y < gridSize; y++, v++) {
				for (int q = 0; q < gridSize; q++, v++) {
					tZ = SetQuad(trianglesZ, tZ, v, v + 1, v + ring, v + ring + 1);
				}
				for (int q = 0; q < gridSize; q++, v++) {
					tX = SetQuad(trianglesX, tX, v, v + 1, v + ring, v + ring + 1);
				}
				for (int q = 0; q < gridSize; q++, v++) {
					tZ = SetQuad(trianglesZ, tZ, v, v + 1, v + ring, v + ring + 1);
				}
				for (int q = 0; q < gridSize - 1; q++, v++) {
					tX = SetQuad(trianglesX, tX, v, v + 1, v + ring, v + ring + 1);
				}
				tX = SetQuad(trianglesX, tX, v, v - ring + 1, v + ring, v + 1);
			}

			tY = CreateTopFace(trianglesY, tY, ring);
			tY = CreateBottomFace(trianglesY, tY, ring);

			mesh.subMeshCount = 3;
			mesh.SetTriangles(trianglesZ, 0);
			mesh.SetTriangles(trianglesX, 1);
			mesh.SetTriangles(trianglesY, 2);

			mesh.SetTriangles(mesh.triangles, 0);
			mesh.subMeshCount = 1;
		}

		private int CreateTopFace (int[] triangles, int t, int ring) {
			int v = ring * gridSize;
			for (int x = 0; x < gridSize - 1; x++, v++) {
				t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + ring);
			}
			t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + 2);

			int vMin = ring * (gridSize + 1) - 1;
			int vMid = vMin + 1;
			int vMax = v + 2;

			for (int z = 1; z < gridSize - 1; z++, vMin--, vMid++, vMax++) {
				t = SetQuad(triangles, t, vMin, vMid, vMin - 1, vMid + gridSize - 1);
				for (int x = 1; x < gridSize - 1; x++, vMid++) {
					t = SetQuad(
						triangles, t,
						vMid, vMid + 1, vMid + gridSize - 1, vMid + gridSize);
				}
				t = SetQuad(triangles, t, vMid, vMax, vMid + gridSize - 1, vMax + 1);
			}

			int vTop = vMin - 2;
			t = SetQuad(triangles, t, vMin, vMid, vTop + 1, vTop);
			for (int x = 1; x < gridSize - 1; x++, vTop--, vMid++) {
				t = SetQuad(triangles, t, vMid, vMid + 1, vTop, vTop - 1);
			}
			t = SetQuad(triangles, t, vMid, vTop - 2, vTop, vTop - 1);

			return t;
		}

		private int CreateBottomFace (int[] triangles, int t, int ring) {
			int v = 1;
			int vMid = vertices.Length - (gridSize - 1) * (gridSize - 1);
			t = SetQuad(triangles, t, ring - 1, vMid, 0, 1);
			for (int x = 1; x < gridSize - 1; x++, v++, vMid++) {
				t = SetQuad(triangles, t, vMid, vMid + 1, v, v + 1);
			}
			t = SetQuad(triangles, t, vMid, v + 2, v, v + 1);

			int vMin = ring - 2;
			vMid -= gridSize - 2;
			int vMax = v + 2;

			for (int z = 1; z < gridSize - 1; z++, vMin--, vMid++, vMax++) {
				t = SetQuad(triangles, t, vMin, vMid + gridSize - 1, vMin + 1, vMid);
				for (int x = 1; x < gridSize - 1; x++, vMid++) {
					t = SetQuad(
						triangles, t,
						vMid + gridSize - 1, vMid + gridSize, vMid, vMid + 1);
				}
				t = SetQuad(triangles, t, vMid + gridSize - 1, vMax + 1, vMid, vMax);
			}

			int vTop = vMin - 1;
			t = SetQuad(triangles, t, vTop + 1, vTop, vTop + 2, vMid);
			for (int x = 1; x < gridSize - 1; x++, vTop--, vMid++) {
				t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vMid + 1);
			}
			t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vTop - 2);

			return t;
		}

		private static int
			SetQuad (int[] triangles, int i, int v00, int v10, int v01, int v11) {
			triangles[i] = v00;
			triangles[i + 1] = triangles[i + 4] = v01;
			triangles[i + 2] = triangles[i + 3] = v10;
			triangles[i + 5] = v11;
			return i + 6;
		}

		/*private void CreateColliders () {
		gameObject.AddComponent<SphereCollider>();
	}

    public void SavePrefabKeyDown()
    {
        if (Input.GetKeyDown("p"))
        {

            GameObject obj = Selection.activeGameObject;
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            MeshCollider mc = obj.GetComponent<MeshCollider>();
            int i = 0;
            string fileNumber;
            string prefabPath = "Assets/Prefabs/Planet_Prefab " + i + ".prefab";
            string meshPath = "Assets/Prefabs/PlanetMesh " + i + ".asset";

            while (System.IO.File.Exists(meshPath))
            {
                fileNumber = Regex.Match(meshPath, @"\d+").Value;
                i = Int32.Parse(fileNumber);
                i++;

                meshPath = "Assets/Prefabs/PlanetMesh " + i + ".asset";
                prefabPath = "Assets/Prefabs/Planet_Prefab " + i + ".prefab";
            }
           
            mf.mesh = mesh;           
            mf.mesh.RecalculateBounds();
            mf.mesh.RecalculateNormals();
            mf.mesh.RecalculateTangents();          
            mc.sharedMesh = mf.mesh;
            AssetDatabase.CreateAsset(mf.mesh, meshPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
           
            var emptyPrefab = PrefabUtility.CreateEmptyPrefab(prefabPath);

            PrefabUtility.ReplacePrefab(obj, emptyPrefab);
        }
    }

    //	private void OnDrawGizmos () {
    //		if (vertices == null) {
    //			return;
    //		}
    //		for (int i = 0; i < vertices.Length; i++) {
    //			Gizmos.color = Color.black;
    //			Gizmos.DrawSphere(vertices[i], 0.1f);
    //			Gizmos.color = Color.yellow;
    //			Gizmos.DrawRay(vertices[i], normals[i]);
    //		}
    //	}*/
	}
}