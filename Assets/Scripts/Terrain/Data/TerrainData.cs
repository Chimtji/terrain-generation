using Unity.Entities;

public struct TerrainData : IComponentData
{
    public int chunkSize;
    public float scale;
    public int size;
}
