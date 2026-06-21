using Unity.Entities;
using Unity.Mathematics;

public struct TerrainChunkData : IComponentData
{
    public float scale;
    public int width;
    public int height;
    public int index;
    public float2 gridPosition;
    public float3 worldPosition;
}
