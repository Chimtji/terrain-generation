using Unity.Entities;

public struct TerrainNoiseSettingsData : IComponentData
{
    public float frequency;
    public float amplitude;
    public int octaves;
    public float lacunarity;
    public float persistence;
    public float offset;
    public uint version;

    public uint GetHash()
    {
        // Simple hash combining all values
        uint hash = 0;
        hash = (hash * 31) + (uint)frequency.GetHashCode();
        hash = (hash * 31) + (uint)amplitude.GetHashCode();
        hash = (hash * 31) + (uint)octaves.GetHashCode();
        hash = (hash * 31) + (uint)lacunarity.GetHashCode();
        hash = (hash * 31) + (uint)persistence.GetHashCode();
        hash = (hash * 31) + (uint)offset.GetHashCode();
        return hash;
    }
}
