using Unity.Entities;

// This acts like a tag even though is not enableable. Its because i only use it on the Terrain Root (Singleton)
// And singleton queries can't use enableable components, so it doesn't make sense to make it enableable.
public struct TerrainGenerated : IComponentData { }
