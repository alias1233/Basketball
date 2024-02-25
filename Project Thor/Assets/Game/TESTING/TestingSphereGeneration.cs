using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class TestingSphereGeneration : MonoBehaviour
{
    public int TerrainResolution = 50;

    private MeshFilter IslandFilter;
    private MeshCollider IslandCollider;

    private Mesh mesh;
    private SphereMesh Island;

    private bool bEditor;

    private void Awake()
    {
        bEditor = Application.isEditor;
    }

    // Start is called before the first frame update
    void Start()
    {
        IslandFilter = GetComponent<MeshFilter>();
        IslandCollider = GetComponent<MeshCollider>();

        mesh = new Mesh();
        Island = new SphereMesh(TerrainResolution);

        Vector3[] Verts = Island.Vertices;

        for (int i = 0; i < Verts.Length; i++)
        {
            Vector3 vertice = Verts[i];

            Verts[i] = new Vector3(vertice.x, vertice.y * noise.cellular2x2(vertice.x + transform.position.x).x * noise.cellular2x2(vertice.z + transform.position.z).y, vertice.z);
        }

        mesh.vertices = Verts;

        mesh.triangles = Island.Triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        IslandFilter.sharedMesh = mesh;
        IslandCollider.sharedMesh = mesh;

        transform.localScale = new Vector3(50, 50, 50);
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isPlaying)
        {
            return;
        }

        mesh = new Mesh();
        Island = new SphereMesh(TerrainResolution);

        Vector3[] Verts = Island.Vertices;

        for (int i = 0; i < Verts.Length; i++)
        {
            Vector3 vertice = Verts[i];

            Verts[i] = new Vector3(vertice.x, vertice.y * noise.cellular2x2(vertice.x + transform.position.x).x * noise.cellular2x2(vertice.z + transform.position.z).y, vertice.z);
        }

        mesh.vertices = Verts;

        mesh.triangles = Island.Triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        IslandFilter.sharedMesh = mesh;
        IslandCollider.sharedMesh = mesh;

        transform.localScale = new Vector3(50, 50, 50);
    }
}
