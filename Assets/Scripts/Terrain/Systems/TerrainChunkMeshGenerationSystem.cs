using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[CreateAfter(typeof(TerrainGenerationSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TerrainNoiseSettingsChangeDetectionSystem))]
[BurstCompile]
public partial struct TerrainChunkMeshGenerationSystem : ISystem
{
    private EntityQuery _chunksQuery;
    private EntityQuery _noiseSettingsQuery;

    public void OnCreate(ref SystemState state)
    {
        _chunksQuery = SystemAPI.QueryBuilder().WithAll<TerrainChunkNeedsGeneration>().Build();

        _noiseSettingsQuery = SystemAPI.QueryBuilder().WithAll<TerrainNoiseSettingsData>().Build();

        state.RequireForUpdate(_noiseSettingsQuery);
        state.RequireForUpdate(_chunksQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Entity noiseSettingsEntity = _noiseSettingsQuery.GetSingletonEntity();

        state.Dependency = new GenerateChunkJob
        {
            NeedsGenerationLookup = SystemAPI.GetComponentLookup<TerrainChunkNeedsGeneration>(
                false
            ),
            NeedsMeshRenderingLookup = SystemAPI.GetComponentLookup<TerrainChunkNeedsMeshRendering>(
                false
            ),
            NoiseSettingsLookup = SystemAPI.GetComponentLookup<TerrainNoiseSettingsData>(true),
            NoiseSettingsEntity = noiseSettingsEntity,
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    private static void AddHeightToVertices(
        ref NativeArray<float3> vertices,
        in TerrainNoiseSettingsData settings,
        in float3 worldPosition
    )
    {
        // ⭐ Now uses the dynamic noise settings instead of hardcoded values
        for (int i = 0; i < vertices.Length; i++)
        {
            float3 vertex = vertices[i];

            float2 noisePosition = (
                new float2(worldPosition.x + vertex.x, worldPosition.z + vertex.z)
                * settings.frequency
            );

            // ⭐ Use settings.amplitude and other parameters
            float noiseValue = PerlinNoise.Noise(noisePosition);
            float heightAmount = (noiseValue - 0.5f) * settings.amplitude;

            // ⭐ Apply offset
            heightAmount += settings.offset;

            vertex.y += heightAmount;
            vertices[i] = vertex;
        }
    }

    [WithAll(typeof(TerrainChunkNeedsGeneration))]
    [BurstCompile]
    public partial struct GenerateChunkJob : IJobEntity
    {
        public Entity NoiseSettingsEntity;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<TerrainChunkNeedsGeneration> NeedsGenerationLookup;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<TerrainChunkNeedsMeshRendering> NeedsMeshRenderingLookup;

        [ReadOnly]
        public ComponentLookup<TerrainNoiseSettingsData> NoiseSettingsLookup;

        private void Execute(
            Entity entity,
            in TerrainChunkData chunkData,
            ref DynamicBuffer<TerrainVertexBufferData> vertexBuffer,
            ref DynamicBuffer<TerrainTriangleBufferData> triangleBuffer,
            ref DynamicBuffer<TerrainUVBufferData> uvBuffer,
            ref TerrainChunkNoiseSettingsVersionData versionData
        )
        {
            // We create all the mesh data in native formats
            MeshData meshData = MeshBuilderNative.CreateSharedVertexMesh(
                chunkData.width,
                chunkData.height,
                chunkData.scale,
                Allocator.Temp
            );

            // Create a copy of vertices to apply height to
            NativeArray<float3> heightMappedVertices = new(
                meshData.vertices.Length,
                Allocator.Temp
            );
            NativeArray<float3>.Copy(meshData.vertices, heightMappedVertices);

            TerrainNoiseSettingsData settings = NoiseSettingsLookup
                .GetRefRO(NoiseSettingsEntity)
                .ValueRO;

            AddHeightToVertices(ref heightMappedVertices, in settings, in chunkData.worldPosition);

            // The pre-allocated buffers are resized to fit the new data
            vertexBuffer.ResizeUninitialized(heightMappedVertices.Length);
            triangleBuffer.ResizeUninitialized(meshData.triangles.Length);
            uvBuffer.ResizeUninitialized(meshData.uvs.Length);

            // We populate all the buffers with the mesh data
            for (int i = 0; i < heightMappedVertices.Length; i++)
            {
                vertexBuffer[i] = new TerrainVertexBufferData { vertex = heightMappedVertices[i] };
            }

            for (int i = 0; i < meshData.triangles.Length; i++)
            {
                triangleBuffer[i] = new TerrainTriangleBufferData
                {
                    triangle = meshData.triangles[i],
                };
            }

            for (int i = 0; i < meshData.uvs.Length; i++)
            {
                uvBuffer[i] = new TerrainUVBufferData { uv = meshData.uvs[i] };
            }

            meshData.vertices.Dispose();
            meshData.triangles.Dispose();
            meshData.uvs.Dispose();
            heightMappedVertices.Dispose();

            NeedsGenerationLookup.SetComponentEnabled(entity, false);
            NeedsMeshRenderingLookup.SetComponentEnabled(entity, true);

            versionData.lastSeenSettingsVersion = settings.version;
        }
    }
}
