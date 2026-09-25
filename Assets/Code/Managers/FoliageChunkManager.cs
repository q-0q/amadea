using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class FoliageChunkManager : MonoBehaviour
{
    public static FoliageChunkManager Instance;

    [Header("Culling Settings")]
    public float chunkSize = 20f;
    private float _renderDistance;
    [Tooltip("Distance outside the screen to keep rendering (prevents pop-in)")]
    public float frustumPadding = 5f;

    [Header("LOD Settings")]
    public float lodFallOff = 40f;
    
    private Dictionary<System.ValueTuple<Mesh, Material>, Dictionary<Vector3Int, List<Matrix4x4>>> _masterRegistry = new();
    private Dictionary<System.ValueTuple<Mesh, Material>, Dictionary<Vector3Int, Matrix4x4[][]>> _bakedChunks = new();
    private Dictionary<System.ValueTuple<Mesh, Material>, Dictionary<Transform, List<Matrix4x4>>> _transformRegistry = new();
    public static readonly List<FoliageMaskSpline> MaskSplines = new();
    private static int FoliageLayer;

    private Camera _camera;

    private void Awake()
    {
        Instance = this;
        _camera = Camera.main;
        _renderDistance = ComputeWorldspaceRenderDistance(MetaSaveSystem.LoadCachedMetaSaveData().foliageRenderDistanceLevel);
        FoliageLayer = LayerMask.NameToLayer("Default");
        
        var foliageSceneData = FoliageSerializer.LoadFoliageSceneData(SceneManager.GetActiveScene().name);
        if (foliageSceneData == null) return;
        foreach (var foliageSystemData in foliageSceneData.FoliageSystemDatas)
        {
            print("registering foliage system data from file: " + foliageSystemData.name);
            var foliageSystem = FindFoliageSystemByName(foliageSystemData.name);
            if (foliageSystem.transformBake) RegisterTransformFoliageSystem(foliageSystem.transform, foliageSystem.mesh, foliageSystem.material, foliageSystemData.Matrices);
            else RegisterFoliageSystem(foliageSystem.mesh, foliageSystem.material, foliageSystemData.Matrices);
        }
        
        ChunkRegisteredFoliage();
        

    }

    public void RegisterFoliageSystem(Mesh mesh, Material mat, Matrix4x4[] instances)
    {
        var key = (mesh, mat);
        if (!_masterRegistry.ContainsKey(key)) _masterRegistry[key] = new Dictionary<Vector3Int, List<Matrix4x4>>();

        foreach (var matrix in instances)
        {
            Vector3 pos = matrix.GetColumn(3);
            Vector3Int chunkPos = Vector3Int.FloorToInt(pos / chunkSize);

            if (!_masterRegistry[key].ContainsKey(chunkPos)) _masterRegistry[key][chunkPos] = new List<Matrix4x4>();
            _masterRegistry[key][chunkPos].Add(matrix);
        }
    }
    
    public void RegisterTransformFoliageSystem(Transform t, Mesh mesh, Material mat, Matrix4x4[] instances)
    {
        var key = (mesh, mat);
        if (!_transformRegistry.ContainsKey(key)) 
            _transformRegistry[key] = new Dictionary<Transform, List<Matrix4x4>>();

        if (!_transformRegistry[key].ContainsKey(t)) 
            _transformRegistry[key][t] = new List<Matrix4x4>();

        // An unscaled matrix representing the transform's world position/rotation
        Matrix4x4 unscaledTransformMatrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
        // Invert it so we can convert world space to unscaled local space
        Matrix4x4 inverseUnscaled = unscaledTransformMatrix.inverse;

        foreach (var instanceMatrix in instances)
        {
            Matrix4x4 localMatrix = instanceMatrix; 
            Vector3 worldPos = localMatrix.GetColumn(3);
        
            // Transform the world position into an unscaled local position
            Vector3 unscaledLocalPos = inverseUnscaled.MultiplyPoint3x4(worldPos);
        
            localMatrix.SetColumn(3, new Vector4(unscaledLocalPos.x, unscaledLocalPos.y, unscaledLocalPos.z, 1f));
        
            _transformRegistry[key][t].Add(localMatrix);
        }
    }


    public void ChunkRegisteredFoliage()
    {
        _bakedChunks.Clear();
        foreach (var entry in _masterRegistry)
        {
            var chunkDict = new Dictionary<Vector3Int, Matrix4x4[][]>();
            foreach (var chunk in entry.Value)
            {

                chunkDict[chunk.Key] = BatchMatrices(chunk.Value);
            }
            _bakedChunks[entry.Key] = chunkDict;
        }
        _masterRegistry.Clear();
    }

    private Matrix4x4[][] BatchMatrices(List<Matrix4x4> fullList)
    {
        int count = fullList.Count;
        int batches = Mathf.CeilToInt(count / 1023f);
        Matrix4x4[][] batched = new Matrix4x4[batches][];
        for (int i = 0; i < batches; i++)
        {
            int size = Mathf.Min(1023, count - (i * 1023));
            batched[i] = new Matrix4x4[size];
            fullList.CopyTo(i * 1023, batched[i], 0, size);
        }
        return batched;
    }

    private void OnEnable()
    {
        MetaSaveSystem.OnMetaSaveDataUpdated += data => { _renderDistance = ComputeWorldspaceRenderDistance(data.foliageRenderDistanceLevel); };
    }

    private static float ComputeWorldspaceRenderDistance(int level)
    {
        return level * 12f + 200f;
    }

    void Update()
    {
        
        Vector3 camPos = _camera.transform.position;
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_camera);
        
        for (int i = 0; i < 6; i++) 
        {
            planes[i].distance += frustumPadding;
        }

        foreach (var entry in _bakedChunks)
        {
            Mesh mesh = entry.Key.Item1;
            Material mat = entry.Key.Item2;
            RenderParams rp = new RenderParams(mat);
            rp.layer = FoliageLayer;

            foreach (var chunk in entry.Value)
            {
                Vector3 chunkCenter = new Vector3(
                    chunk.Key.x * chunkSize + chunkSize / 2, 
                    camPos.y, 
                    chunk.Key.z * chunkSize + chunkSize / 2
                );

                float dist = Vector3.Distance(camPos, chunkCenter);
                if (dist > _renderDistance) continue;

                Bounds b = new Bounds(chunkCenter, new Vector3(chunkSize, 50f, chunkSize));
                if (!GeometryUtility.TestPlanesAABB(planes, b)) continue;

                float densityPercent = Mathf.InverseLerp(_renderDistance, _renderDistance - lodFallOff, dist);

                foreach (var batch in chunk.Value)
                {
                    int countToRender = Mathf.CeilToInt(batch.Length * densityPercent);
                    if (countToRender <= 0) continue;

                    Graphics.RenderMeshInstanced(rp, mesh, 0, batch, countToRender);
                }
            }
        }
        
        RenderTransformFoliage();
    }
    
    private void Shuffle(List<Matrix4x4> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Matrix4x4 temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private List<Matrix4x4> _transformRenderBatch = new List<Matrix4x4>(1023);

    private void RenderTransformFoliage()
    {
        foreach (var entry in _transformRegistry)
        {
            Mesh mesh = entry.Key.Item1;
            Material mat = entry.Key.Item2;
        
            RenderParams rp = new RenderParams(mat);
            rp.layer = FoliageLayer;

            _transformRenderBatch.Clear();

            foreach (var keyValuePair in entry.Value)
            {
                Transform t = keyValuePair.Key;
                if (t == null) continue; 

                // Create the world matrix using only position and rotation
                Matrix4x4 unscaledLocalToWorld = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);

                foreach (var localMatrix in keyValuePair.Value)
                {
                    // Both matrices are now completely agnostic of the Transform's scale!
                    Matrix4x4 worldMatrix = unscaledLocalToWorld * localMatrix;
                
                    _transformRenderBatch.Add(worldMatrix);

                    if (_transformRenderBatch.Count >= 1023)
                    {
                        Graphics.RenderMeshInstanced(rp, mesh, 0, _transformRenderBatch, _transformRenderBatch.Count);
                        _transformRenderBatch.Clear();
                    }
                }
            }

            if (_transformRenderBatch.Count > 0)
            {
                Graphics.RenderMeshInstanced(rp, mesh, 0, _transformRenderBatch, _transformRenderBatch.Count);
            }
        }
    }

    private FoliageSystem FindFoliageSystemByName(string name)
    {
        foreach (var foliageSystem in FindObjectsByType<FoliageSystem>())
        {
            if (foliageSystem.name == name) return foliageSystem;
        }

        return null;
    }
}