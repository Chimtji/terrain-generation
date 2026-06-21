using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct TerrainMeshData : IComponentData
{
    public NativeArray<float3> vertices;
    public NativeArray<int> triangles;
    public NativeArray<float2> uvs;
}
