using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

// DICTIONARY:
// Vertex: a float3 point in 3D space (x,y,z). This is the position of a corner of a triangle.
// Vertices: Plural of Vertex.
// Triangle: A triangle defined by 3 vertices.
// Quad: A square defined by 4 vertices, which can be split into 2 triangles.
// Indice: The index of a vertex.
// Indices: A list of Indice to define the order of vertices to form a triangle.
// UV: A float2 representing the texture coordinates for a vertex. (u,v) where u is the horizontal coordinate and v is the vertical coordinate.
// Normal: A float3 representing the direction perpendicular to the surface at a vertex, used for lighting calculations.

/// <summary>
/// Generic utility class for mesh generation and manipulation from Native to Managed Meshes
/// </summary>
public static class MeshBuildManaged
{
    /// <summary>
    /// Converts Native data to Unity Mesh Format.
    /// Native Data works for Unity DOTS, where Mesh works for Unity GameObjects.
    /// </summary>
    /// <param name="vertices">The array of vertices.</param>
    /// <param name="triangles">The array of triangle indices.</param>
    /// <param name="uvs">The array of UV coordinates.</param>
    /// <param name="meshName">The name of the generated mesh.</param>
    /// <returns>The generated Unity Mesh.</returns>
    public static Mesh ConvertNativeDataToMesh(
        NativeArray<float3> vertices,
        NativeArray<int> triangles,
        NativeArray<float2> uvs,
        string meshName = "GeneratedMesh"
    )
    {
        Mesh mesh = new()
        {
            name = meshName,
            vertices = vertices.Reinterpret<Vector3>().ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.Reinterpret<Vector2>().ToArray(),
        };

        // Recalculate normals for proper lighting
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// Validates the mesh data to ensure it is consistent and can be used to create a mesh.
    /// </summary>
    /// <param name="vertices">The array of vertices.</param>
    /// <param name="triangles">The array of triangle indices.</param>
    /// <param name="uvs">The array of UV coordinates.</param>
    /// <returns>True if the mesh data is valid, false otherwise.</returns>
    public static bool ValidateMeshData(
        NativeArray<float3> vertices,
        NativeArray<int> triangles,
        NativeArray<float2> uvs
    )
    {
        // Vertices and UVs must match in count
        if (vertices.Length != uvs.Length)
        {
            Debug.LogError($"Vertex count ({vertices.Length}) != UV count ({uvs.Length})");
            return false;
        }

        // Triangle indices must be even (3 per triangle)
        if (triangles.Length % 3 != 0)
        {
            Debug.LogError($"Triangle count ({triangles.Length}) is not divisible by 3");
            return false;
        }

        // Triangle indices must point to valid vertices
        for (int i = 0; i < triangles.Length; i++)
        {
            int idx = triangles[i];
            if (idx < 0 || idx >= vertices.Length)
            {
                Debug.LogError($"Triangle index {i} points to invalid vertex {idx}");
                return false;
            }
        }

        return true;
    }
}
