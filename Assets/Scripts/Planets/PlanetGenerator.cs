using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PlanetGenerator : MonoBehaviour
{
    public enum PreviewMode { LOD0, LOD1, LOD2, CollisionRes }
    public ResolutionSettings resolutionSettings;
    public PreviewMode previewMode;

    public ShapeSettings shapeSettings;
    public ShadingSettings shadingSettings;

    [HideInInspector]
    public PlanetSettings body;
    bool shapeSettingsUpdated;
    bool shadingSettingsUpdated;
    Mesh previewMesh;
    Mesh collisionMesh;
    Mesh[] lodMeshes;
    MeshFilter terrainMeshFilter;
    static Dictionary<int, SphereMesh> sphereGenerators;
    Vector3 heightMinMax;

    ComputeBuffer vertexBuffer;

    int activeLODIndex = -1;
    void Start()
    {
        body = ScriptableObject.CreateInstance<PlanetSettings>();
        body.shape = this.shapeSettings;
        body.shading = this.shadingSettings;
    }

    void Update()
    {
        if (InEditMode)
        {
            HandleEditModeGeneration();
        }
    }

    // Generates the planet and adds the collider object on top
    void HandleGameModeGeneration()
    {
        if (CanGenerateMesh())
        {
            Dummy();
            lodMeshes = new Mesh[ResolutionSettings.numLODLevels];
            for (int i = 0; i < lodMeshes.Length; i++)
            {
                GenerateTerrainMesh(ref lodMeshes[i], resolutionSettings.GetLODResolution(i));
            }

            GenerateCollisionMesh(resolutionSettings.collider);

            Material terrainMatInstance = new Material(body.shading.terrainMaterial);
            //body.shading.Initialize(body.shape);
            body.shading.SetTerrainProperties(terrainMatInstance, heightMinMax);
            GameObject terrainHolder = MeshToGameObject("Planet", null, terrainMatInstance);
            terrainMeshFilter = terrainHolder.GetComponent<MeshFilter>();
            // Add collider
            MeshCollider meshCollider;
            if (!terrainHolder.TryGetComponent<MeshCollider>(out meshCollider))
            {
                meshCollider = terrainHolder.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = collisionMesh;
            }
        }
    }

    // Takes care of buffers and memory, only updates the shape when a change has been made
    void HandleEditModeGeneration()
    {
        ComputeHelper.shouldReleaseEditModeBuffers -= ReleaseAllBuffers;
        ComputeHelper.shouldReleaseEditModeBuffers += ReleaseAllBuffers;

        if (CanGenerateMesh())
        {
            if (shapeSettingsUpdated)
            {
                shapeSettingsUpdated = false;
                shadingSettingsUpdated = false;
                Dummy();
                GenerateTerrainMesh(ref previewMesh, PickTerrainRes());
                MeshToGameObject("Planet", previewMesh, body.shading.terrainMaterial);
            }
            else if (shadingSettingsUpdated)
            {
                shadingSettingsUpdated = false;
                GenerateShadingMaterial();
            }
        }
    }

    void GenerateShadingMaterial()
    {
        //body.shading.Initialize(body.shape);
        body.shading.SetTerrainProperties(body.shading.terrainMaterial, heightMinMax);
        ComputeHelper.CreateStructuredBuffer<Vector3>(ref vertexBuffer, previewMesh.vertices);
        Vector4[] shadingData = body.shading.GenerateShadingData(vertexBuffer);
        previewMesh.SetUVs(0, shadingData);
    }

    // Generates the main mesh based on the given resolution.
    void GenerateTerrainMesh(ref Mesh mesh, int resolution)
    {
        var (vertices, triangles) = CreateSphereVertsAndTris(resolution);
        ComputeHelper.CreateStructuredBuffer<Vector3>(ref vertexBuffer, vertices);

        float edgeLength = (vertices[triangles[0]] - vertices[triangles[1]]).magnitude;


        float[] heights = body.shape.CalculateHeights(vertexBuffer);

        float minHeight = float.PositiveInfinity;
        float maxHeight = float.NegativeInfinity;
        // Apply heights to vertices
        for (int i = 0; i < heights.Length; i++)
        {
            vertices[i] *= heights[i];
            minHeight = Mathf.Min(minHeight, heights[i]);
            maxHeight = Mathf.Max(maxHeight, heights[i]);
        }

        // Create mesh
        CreateMesh(ref mesh, vertices.Length);
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0, true);
        mesh.RecalculateNormals(); //

        // Create crude tangents (vectors perpendicular to surface normal)
        // This is needed (even though normal mapping is being done with triplanar)
        // because surfaceshader wants normals in tangent space
        var normals = mesh.normals;
        var crudeTangents = new Vector4[mesh.vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 normal = normals[i];
            crudeTangents[i] = new Vector4(-normal.z, 0, normal.x, 1);
        }
        mesh.SetTangents(crudeTangents);

        heightMinMax = new Vector3(minHeight, body.shape.planet.size, maxHeight);
    }

    // Generates the collision mesh based on the main mesh and the resolution.
    void GenerateCollisionMesh(int resolution)
    {
        var (vertices, triangles) = CreateSphereVertsAndTris(resolution);

        // Create mesh
        CreateMesh(ref collisionMesh, vertices.Length);
        collisionMesh.vertices = vertices;
        collisionMesh.triangles = triangles;
    }

    // Calls the basic function from SphereMesh to create a basic sphere.
    void CreateMesh(ref Mesh mesh, int numVertices)
    {
        const int vertexLimit16Bit = 1 << 16 - 1; // 65535
        if (mesh == null)
        {
            mesh = new Mesh();
        }
        else
        {
            mesh.Clear();
        }
        mesh.indexFormat = (numVertices < vertexLimit16Bit) ? UnityEngine.Rendering.IndexFormat.UInt16 : UnityEngine.Rendering.IndexFormat.UInt32;
    }

    GameObject MeshToGameObject(string name, Mesh mesh, Material material)
    {
        // Find/create object
        var child = transform.Find(name);
        if (!child)
        {
            child = new GameObject(name).transform;
            child.parent = transform;
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            child.gameObject.layer = gameObject.layer;
        }

        // Add mesh components
        MeshFilter filter;
        if (!child.TryGetComponent<MeshFilter>(out filter))
        {
            filter = child.gameObject.AddComponent<MeshFilter>();
        }
        filter.sharedMesh = mesh;

        MeshRenderer renderer;
        if (!child.TryGetComponent<MeshRenderer>(out renderer))
        {
            renderer = child.gameObject.AddComponent<MeshRenderer>();
        }
        renderer.sharedMaterial = material;

        return child.gameObject;
    }

    // Generate sphere (or reuse if already generated) and return a copy of the vertices and triangles
    (Vector3[] vertices, int[] triangles) CreateSphereVertsAndTris(int resolution)
    {
        if (sphereGenerators == null)
        {
            sphereGenerators = new Dictionary<int, SphereMesh>();
        }

        if (!sphereGenerators.ContainsKey(resolution))
        {
            sphereGenerators.Add(resolution, new SphereMesh(resolution));
        }

        var generator = sphereGenerators[resolution];

        var vertices = new Vector3[generator.Vertices.Length];
        var triangles = new int[generator.Triangles.Length];
        System.Array.Copy(generator.Vertices, vertices, vertices.Length);
        System.Array.Copy(generator.Triangles, triangles, triangles.Length);

        return (vertices, triangles);
    }

    public void OnValidate()
    {
        if (body)
        {
            if (body.shape)
            {
                body.shape.OnSettingsChanged -= OnShapeSettingsChanged;
                body.shape.OnSettingsChanged += OnShapeSettingsChanged;
            }

            if (body.shading)
            {
                body.shading.OnSettingsChanged -= OnShadingSettingsChanged;
                body.shading.OnSettingsChanged += OnShadingSettingsChanged;
            }
        }

        if (resolutionSettings != null)
        {
            resolutionSettings.ClampResolutions();
        }
        OnShapeSettingsChanged();
    }

    public void OnShapeSettingsChanged()
    {
        shapeSettingsUpdated = true;
    }

    public void OnShadingSettingsChanged()
    {
        shadingSettingsUpdated = true;
    }

    void ReleaseAllBuffers()
    {
        ComputeHelper.Release(vertexBuffer);
        if (body.shape)
        {
            body.shape.ReleaseBuffers();
        }
    }

    public void SetLOD(int lodIndex)
    {
        if (lodIndex != activeLODIndex && terrainMeshFilter)
        {
            activeLODIndex = lodIndex;
            terrainMeshFilter.sharedMesh = lodMeshes[lodIndex];
        }
    }

    public int PickTerrainRes()
    {
        if (InEditMode)
        {
            switch (previewMode)
            {
                case PreviewMode.LOD0:
                    return resolutionSettings.lod0;
                case PreviewMode.LOD1:
                    return resolutionSettings.lod1;
                case PreviewMode.LOD2:
                    return resolutionSettings.lod2;
                case PreviewMode.CollisionRes:
                    return resolutionSettings.collider;
            }
        }

        return 0;
    }

    void OnDestroy()
    {
        ReleaseAllBuffers();
    }

    bool CanGenerateMesh()
    {
        return ComputeHelper.CanRunEditModeCompute && body.shape && body.shape.heightMapCompute;
    }

    void Dummy()
    {
        // Crude fix for a problem I was having where the values in the vertex buffer were *occasionally* all zero at start of game
        // This function runs the compute shader once with single dummy input, after which it seems the problem doesn't occur
        // (Waiting until Time.frameCount > 3 before generating is another gross hack that seems to fix the problem)
        // I don't know why...
        Vector3[] vertices = new Vector3[] { Vector3.zero };
        ComputeHelper.CreateStructuredBuffer<Vector3>(ref vertexBuffer, vertices);
        body.shape.CalculateHeights(vertexBuffer);
    }

    bool InGameMode
    {
        get
        {
            return Application.isPlaying;
        }
    }

    bool InEditMode
    {
        get
        {
            return !Application.isPlaying;
        }
    }


    public class TerrainData
    {
        public float[] heights;
        public Vector4[] uvs;
    }

    [System.Serializable]
    public class ResolutionSettings
    {

        public const int numLODLevels = 3;
        const int maxAllowedResolution = 500;

        public int lod0 = 300;
        public int lod1 = 100;
        public int lod2 = 50;
        public int collider = 100;

        public int GetLODResolution(int lodLevel)
        {
            switch (lodLevel)
            {
                case 0:
                    return lod0;
                case 1:
                    return lod1;
                case 2:
                    return lod2;
            }
            return lod2;
        }

        public void ClampResolutions()
        {
            lod0 = Mathf.Min(maxAllowedResolution, lod0);
            lod1 = Mathf.Min(maxAllowedResolution, lod1);
            lod2 = Mathf.Min(maxAllowedResolution, lod2);
            collider = Mathf.Min(maxAllowedResolution, collider);
        }
    }
}
