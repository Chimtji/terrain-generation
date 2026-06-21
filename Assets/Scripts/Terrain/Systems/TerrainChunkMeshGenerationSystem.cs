using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[CreateAfter(typeof(TerrainGenerationSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct TerrainChunkMeshGenerationSystem : ISystem
{
    private EntityQuery _chunksQuery;

    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new GenerateChunkJob
        {
            NeedsGenerationLookup = SystemAPI.GetComponentLookup<TerrainChunkNeedsGeneration>(
                false
            ),
            NeedsMeshRenderingLookup = SystemAPI.GetComponentLookup<TerrainChunkNeedsMeshRendering>(
                false
            ),
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    private static void AddHeightToVertices(
        ref NativeArray<float3> vertices,
        in float scale,
        in float3 worldPosition
    )
    {
        const float noiseFrequency = 0.05f;

        // Apply height variation based on Perlin noise
        for (int i = 0; i < vertices.Length; i++)
        {
            float3 vertex = vertices[i];

            // World-space position for noise sampling
            // (worldPosition + local vertex position) to ensure seamless chunks
            float2 noisePosition =
                (
                    new float2(worldPosition.x + vertex.x, worldPosition.z + vertex.z)
                    * noiseFrequency
                ) / scale;

            // Sample Perlin noise
            float noiseValue = PerlinNoise.Noise(noisePosition);

            // Apply height: noise value * amplitude
            // Adjust amplitude (e.g., 5) based on desired terrain height variation
            float heightAmount = (noiseValue - 0.5f) * 10f; // Range: [-5, 5]

            vertex.y += heightAmount;
            vertices[i] = vertex;
        }
    }

    [WithAll(typeof(TerrainChunkNeedsGeneration))]
    [BurstCompile]
    public partial struct GenerateChunkJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public ComponentLookup<TerrainChunkNeedsGeneration> NeedsGenerationLookup;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<TerrainChunkNeedsMeshRendering> NeedsMeshRenderingLookup;

        private void Execute(
            Entity entity,
            in TerrainChunkData chunkData,
            ref DynamicBuffer<TerrainVertexBufferData> vertexBuffer,
            ref DynamicBuffer<TerrainTriangleBufferData> triangleBuffer,
            ref DynamicBuffer<TerrainUVBufferData> uvBuffer
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

            AddHeightToVertices(
                ref heightMappedVertices,
                in chunkData.scale,
                in chunkData.worldPosition
            );

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
        }
    }
}
