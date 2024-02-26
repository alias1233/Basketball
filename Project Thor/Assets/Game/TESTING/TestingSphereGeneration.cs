using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class TestingSphereGeneration : MonoBehaviour
{
    public int TerrainResolution = 50;
    public float NoiseFactor;
    public float BottomHeightFactor;
    public float TopHeightFactor;
    public float BottomVarience;

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

        GenerateIsland();

        transform.localScale = new Vector3(50, 50, 50);
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isPlaying)
        {
            return;
        }

        GenerateIsland();
    }

    private void GenerateIsland()
    {
        mesh = new Mesh();
        Island = new SphereMesh(TerrainResolution);

        Vector3[] Verts = Island.Vertices;

        for (int i = 0; i < Verts.Length; i++)
        {
            Vector3 vertice = Verts[i];
            float Height = 0;

            if(vertice.y < 0)
            {
                float XZPos = vertice.z * vertice.z + vertice.x * vertice.x;

                if (XZPos == 0)
                {
                    XZPos = 0.001f;
                }

                Height = BottomHeightFactor * vertice.y * (1 + NoiseFactor * noise.cellular2x2(vertice.x + transform.position.x).x) * (1 - XZPos * BottomVarience);
            }

            else
            {
                Height = TopHeightFactor * vertice.y * (1 + NoiseFactor * noise.cellular2x2(vertice.x + transform.position.x).x);
            }

            Verts[i] = new Vector3(vertice.x * (1 + NoiseFactor * noise.cellular2x2(vertice.z + transform.position.z).x), Height, vertice.z * (1 + NoiseFactor * noise.cellular2x2(vertice.y + transform.position.y).x));
        }

        mesh.vertices = Verts;

        mesh.triangles = Island.Triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        IslandFilter.sharedMesh = mesh;
        IslandCollider.sharedMesh = mesh;
    }
}
