using Unity.Collections;
using Unity.Mathematics;

public struct MeshData
{
    public NativeArray<float3> vertices;
    public NativeArray<int> triangles;
    public NativeArray<float2> uvs;

    public void Dispose()
    {
        if (vertices.IsCreated) vertices.Dispose();
        if (triangles.IsCreated) triangles.Dispose();
        if (uvs.IsCreated) uvs.Dispose();
    }
}