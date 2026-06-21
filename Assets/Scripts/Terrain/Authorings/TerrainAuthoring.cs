using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class TerrainAuthoring : MonoBehaviour
{
    public Material material;
    public int chunkSize;
    public float scale;
    public int size;

    public class Baker : Baker<TerrainAuthoring>
    {
        public override void Bake(TerrainAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            // We set position to 0,0,0
            AddComponent(entity, LocalTransform.Identity);

            AddComponent(
                entity,
                new TerrainData
                {
                    chunkSize = authoring.chunkSize,
                    scale = authoring.scale,
                    size = authoring.size,
                }
            );
            AddComponentObject(entity, new TerrainRenderData { Material = authoring.material });
        }
    }
}
