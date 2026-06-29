using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TerrainChunkMeshGenerationSystem))]
public partial class TerrainNoiseSettingsChangeDetectionSystem : SystemBase
{
    private EntityQuery _noiseSettingsQuery;
    private EntityQuery _terrainChunksQuery;

    protected override void OnCreate()
    {
        _noiseSettingsQuery = SystemAPI.QueryBuilder().WithAll<TerrainNoiseSettingsData>().Build();

        _terrainChunksQuery = SystemAPI
            .QueryBuilder()
            .WithAll<TerrainChunkData, TerrainChunkNoiseSettingsVersionData>()
            .Build();

        RequireForUpdate(_noiseSettingsQuery);
        RequireForUpdate(_terrainChunksQuery);
    }

    protected override void OnUpdate()
    {
        TerrainNoiseSettingsData settings =
            _noiseSettingsQuery.GetSingleton<TerrainNoiseSettingsData>();
        uint currentVersion = settings.version;

        var commandBuffer = SystemAPI
            .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(World.Unmanaged);

        foreach (
            var (chunk, version, entity) in SystemAPI
                .Query<RefRO<TerrainChunkData>, RefRO<TerrainChunkNoiseSettingsVersionData>>()
                .WithEntityAccess()
        )
        {
            if (version.ValueRO.lastSeenSettingsVersion != currentVersion)
            {
                commandBuffer.SetComponentEnabled<TerrainChunkNeedsGeneration>(entity, true);
            }
        }
    }
}
