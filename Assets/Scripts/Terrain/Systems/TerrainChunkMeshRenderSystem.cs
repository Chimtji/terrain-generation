using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAfter(typeof(TerrainChunkMeshGenerationSystem))]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class TerrainChunkMeshRenderSystem : SystemBase
{
    private EntityQuery _terrainRootQuery;
    private EntityQuery _chunksQuery;
    private List<Mesh> _meshPool;
    private const int POOL_SIZE = 256;

    protected override void OnCreate()
    {
        _meshPool = new List<Mesh>(POOL_SIZE);

        _chunksQuery = SystemAPI
            .QueryBuilder()
            .WithAll<TerrainChunkData, TerrainChunkNeedsMeshRendering>()
            .Build();

        _terrainRootQuery = SystemAPI
            .QueryBuilder()
            .WithAll<TerrainData, TerrainRenderData>()
            .Build();

        RequireForUpdate(_terrainRootQuery);
        RequireForUpdate(_chunksQuery);
    }

    protected override void OnUpdate()
    {
        Entity terrainRoot = _terrainRootQuery.GetSingletonEntity();
        TerrainRenderData renderData = EntityManager.GetComponentObject<TerrainRenderData>(
            terrainRoot
        );

        NativeArray<Entity> chunkEntities = _chunksQuery.ToEntityArray(Allocator.Temp);

        foreach (Entity chunkEntity in chunkEntities)
        {
            RenderChunkMesh(chunkEntity, renderData);
            EntityManager.SetComponentEnabled<TerrainChunkNeedsMeshRendering>(chunkEntity, false);
            EntityManager.SetComponentEnabled<TerrainChunkReady>(chunkEntity, true);
        }

        chunkEntities.Dispose();
    }

    private void RenderChunkMesh(Entity entity, TerrainRenderData renderData)
    {
        DynamicBuffer<TerrainVertexBufferData> vertexBuffer =
            SystemAPI.GetBuffer<TerrainVertexBufferData>(entity);
        DynamicBuffer<TerrainTriangleBufferData> triangleBuffer =
            SystemAPI.GetBuffer<TerrainTriangleBufferData>(entity);
        DynamicBuffer<TerrainUVBufferData> uvBuffer = SystemAPI.GetBuffer<TerrainUVBufferData>(
            entity
        );

        Mesh mesh = AcquireMesh();
        mesh.Clear();

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        meshData.SetVertexBufferParams(vertexBuffer.Length, VertexLayout);
        meshData.SetIndexBufferParams(triangleBuffer.Length, IndexFormat.UInt32);

        NativeArray<TerrainMeshDataVertex> meshVertices =
            meshData.GetVertexData<TerrainMeshDataVertex>();
        NativeArray<int> meshIndices = meshData.GetIndexData<int>();

        for (int i = 0; i < vertexBuffer.Length; i++)
        {
            meshVertices[i] = new TerrainMeshDataVertex
            {
                vertex = vertexBuffer[i].vertex,
                uv = uvBuffer[i].uv,
            };
        }

        for (int i = 0; i < triangleBuffer.Length; i++)
        {
            meshIndices[i] = triangleBuffer[i].triangle;
        }

        meshData.subMeshCount = 1;

        meshData.SetSubMesh(
            0,
            new SubMeshDescriptor(indexStart: 0, indexCount: triangleBuffer.Length)
        );

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        RenderMeshArray meshArray = new(new[] { renderData.Material }, new[] { mesh });
        RenderMeshDescription renderMeshDescription = new(
            shadowCastingMode: ShadowCastingMode.On,
            receiveShadows: true
        );

        RenderMeshUtility.AddComponents(
            entity,
            EntityManager,
            renderMeshDescription,
            meshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
        );
    }

    public void OnDestroy(ref SystemState state)
    {
        foreach (Mesh mesh in _meshPool)
        {
            if (mesh != null)
                Object.Destroy(mesh);
        }
        _meshPool.Clear();
    }

    private Mesh AcquireMesh()
    {
        if (_meshPool.Count > 0)
        {
            Mesh mesh = _meshPool[_meshPool.Count - 1];
            _meshPool.RemoveAt(_meshPool.Count - 1);
            return mesh;
        }
        return new Mesh { name = "TerrainChunk" };
    }

    private static readonly VertexAttributeDescriptor[] VertexLayout =
    {
        new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
    };

    private struct TerrainMeshDataVertex
    {
        public float3 vertex;
        public float2 uv;
    }
}
