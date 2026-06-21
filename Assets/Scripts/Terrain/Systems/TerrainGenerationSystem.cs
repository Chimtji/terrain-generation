using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// This system is responsible for generating the terrain chunks based on the TerrainData component.
/// Its only focus is to create the grid of chunks with the right positions and give the foundation for other systems to fill them with data.
///
/// Only one system runs at a time in DOTS, so we can be sure that all chunks are created before any other system tries to access them.
/// We set UpdateInGroup to SimulationSystemGroup to ensure that this system runs before any system that might need the chunks to exist.
///
/// This system is not Burst Compatible (by design) because it uses EntityManager.
/// That is also why this system only focuses on creating the entities and nothing more.
/// Then the burstable systems can handle the heavy lifting.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct TerrainGenerationSystem : ISystem
{
    private EntityQuery _terrainQuery;

    public void OnCreate(ref SystemState state)
    {
        // This caches the query, so we don't have to create it every frame in OnUpdate.
        // Also by checking on TerrainGenerated we ensure that this system only runs once, when the terrain is first generated.
        _terrainQuery = SystemAPI
            .QueryBuilder()
            .WithAll<TerrainData>()
            .WithNone<TerrainGenerated>()
            .Build();

        state.RequireForUpdate(_terrainQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity terrainEntity = _terrainQuery.GetSingletonEntity();
        TerrainData terrainData = SystemAPI.GetComponent<TerrainData>(terrainEntity);

        CreateChunks(ref state, terrainData, terrainEntity);

        state.EntityManager.AddComponent<TerrainGenerated>(terrainEntity);
    }

    private void CreateChunks(ref SystemState state, TerrainData terrainData, Entity terrainEntity)
    {
        int gridSize = terrainData.size;

        // By creating an archetype we minimize drastically the structural
        // changes needed to create the chunk entities
        EntityArchetype chunkArchetype = state.EntityManager.CreateArchetype(
            typeof(TerrainChunkData),
            typeof(LocalTransform),
            typeof(TerrainVertexBufferData),
            typeof(TerrainTriangleBufferData),
            typeof(TerrainUVBufferData),
            typeof(TerrainChunkNeedsGeneration),
            typeof(TerrainChunkNeedsMeshRendering),
            typeof(TerrainChunkReady),
            typeof(Parent)
        );

        // We create all chunk entities in a single call to CreateEntity,
        // which is much faster than creating them one by one in a loop.
        NativeArray<Entity> chunks = state.EntityManager.CreateEntity(
            chunkArchetype,
            gridSize * gridSize,
            state.WorldUpdateAllocator
        );

        // We start by disabling these state tags
        // Later these will be enabled
        foreach (var chunk in chunks)
        {
            state.EntityManager.SetComponentEnabled<TerrainChunkNeedsMeshRendering>(chunk, false);
            state.EntityManager.SetComponentEnabled<TerrainChunkReady>(chunk, false);
        }

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                int index = x * gridSize + z;
                Entity chunkEntity = chunks[index];

                float2 gridPosition = new(x, z);
                float3 worldPosition = new float3(
                    gridPosition.x * terrainData.chunkSize * terrainData.scale,
                    0,
                    gridPosition.y * terrainData.chunkSize * terrainData.scale
                );

                state.EntityManager.SetComponentData(
                    chunkEntity,
                    new Parent { Value = terrainEntity }
                );

                state.EntityManager.SetComponentData(
                    chunkEntity,
                    LocalTransform.FromPosition(worldPosition)
                );

                state.EntityManager.SetComponentData(
                    chunkEntity,
                    new TerrainChunkData
                    {
                        scale = terrainData.scale,
                        width = terrainData.chunkSize,
                        height = terrainData.chunkSize,
                        index = x * gridSize + z,
                        gridPosition = gridPosition,
                        worldPosition = worldPosition,
                    }
                );

                // We pre-allocate the dynamic buffers.
                // This enables writing to them without needing to check if they exist first.
                SystemAPI.GetBuffer<TerrainVertexBufferData>(chunkEntity);
                SystemAPI.GetBuffer<TerrainTriangleBufferData>(chunkEntity);
                SystemAPI.GetBuffer<TerrainUVBufferData>(chunkEntity);
            }
        }
    }
}
