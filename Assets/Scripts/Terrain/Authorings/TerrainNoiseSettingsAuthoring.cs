using Unity.Entities;
using UnityEngine;

public class TerrainNoiseSettingsAuthoring : MonoBehaviour
{
    [Header("Noise Generation")]
    [Range(0.01f, 0.5f)]
    public float noiseFrequency = 0.05f;

    [Range(1f, 20f)]
    public float noiseAmplitude = 10f;

    [Range(1, 6)]
    public int octaves = 4;

    [Range(0.1f, 2f)]
    public float lacunarity = 2f;

    [Range(0f, 1f)]
    public float persistence = 0.5f;

    [Range(-1f, 1f)]
    public float offset = 0f;

    private float _lastFrequency;
    private float _lastAmplitude;
    private int _lastOctaves;
    private float _lastLacunarity;
    private float _lastPersistence;
    private float _lastOffset;

    private uint _lastVersion;

    private void OnValidate()
    {
        // Called whenever inspector values change
        // Check if any value differs from last frame
        if (!SettingsEqual())
        {
            // Cache current values
            CacheSettings();

            // Notify the game that settings changed
            // (We'll do this via IComponentData versioning in the system)
            _lastVersion++;
        }
    }

    private bool SettingsEqual()
    {
        return noiseFrequency == _lastFrequency
            && noiseAmplitude == _lastAmplitude
            && octaves == _lastOctaves
            && lacunarity == _lastLacunarity
            && persistence == _lastPersistence
            && offset == _lastOffset;
    }

    private void CacheSettings()
    {
        _lastFrequency = noiseFrequency;
        _lastAmplitude = noiseAmplitude;
        _lastOctaves = octaves;
        _lastLacunarity = lacunarity;
        _lastPersistence = persistence;
        _lastOffset = offset;
    }

    public class Baker : Baker<TerrainNoiseSettingsAuthoring>
    {
        public override void Bake(TerrainNoiseSettingsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(
                entity,
                new TerrainNoiseSettingsData
                {
                    frequency = authoring.noiseFrequency,
                    amplitude = authoring.noiseAmplitude,
                    octaves = authoring.octaves,
                    lacunarity = authoring.lacunarity,
                    persistence = authoring.persistence,
                    offset = authoring.offset,
                    version = authoring._lastVersion,
                }
            );
        }
    }
}
