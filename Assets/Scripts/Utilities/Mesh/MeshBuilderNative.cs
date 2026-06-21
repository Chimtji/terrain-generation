using Unity.Collections;
using Unity.Mathematics;

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
/// Generic utility class for mesh generation and manipulation, designed for use within Unity's Burst-compiled jobs and systems.
/// </summary>
public static class MeshBuilderNative
{
    // The count of vertices for a quad: 2 triangles per quad, 3 vertices per triangle
    const int QUAD_VERTICES = 6;

    /// <summary>
    /// Creates a mesh with unique vertices, which means that each triangle has its own set of vertices, even if they are in the same position as other triangles.
    /// </summary>
    /// <param name="width">The mesh width</param>
    /// <param name="height">The mesh height</param>
    /// <param name="scale">The scale of the mesh</param>
    /// <param name="allocator">The allocator for the native arrays</param>
    /// <returns>The generated mesh data</returns>
    public static MeshData CreateUniqueVertexMesh(
        int width,
        int height,
        float scale = 1f,
        Allocator allocator = Allocator.Temp
    )
    {
        int vertexCount = GetMeshVertices(width, height);

        var vertices = new NativeArray<float3>(vertexCount, allocator);
        var triangles = new NativeArray<int>(vertexCount, allocator);
        var uvs = new NativeArray<float2>(vertexCount, allocator);

        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Four corners of this quad
                float3 bottomLeft = new(x * scale, 0, y * scale);
                float3 bottomRight = new((x + 1) * scale, 0, y * scale);
                float3 topLeft = new(x * scale, 0, (y + 1) * scale);
                float3 topRight = new((x + 1) * scale, 0, (y + 1) * scale);

                // Texture is calculated from 0 to 1 across the entire mesh, so we normalize the size:
                float uvLeft = (float)x / width;
                float uvRight = (float)(x + 1) / width;
                float uvBottom = (float)y / height;
                float uvTop = (float)(y + 1) / height;

                // FYI: Incrementation in parameters happens after the function has run
                // Triangle 1
                SetVertexData(vertices, triangles, uvs, index++, bottomLeft, uvLeft, uvBottom);
                SetVertexData(vertices, triangles, uvs, index++, topLeft, uvLeft, uvTop);
                SetVertexData(vertices, triangles, uvs, index++, bottomRight, uvRight, uvBottom);

                // Triangle 2
                SetVertexData(vertices, triangles, uvs, index++, bottomRight, uvRight, uvBottom);
                SetVertexData(vertices, triangles, uvs, index++, topLeft, uvLeft, uvTop);
                SetVertexData(vertices, triangles, uvs, index++, topRight, uvRight, uvTop);
            }
        }

        return new MeshData
        {
            vertices = vertices,
            triangles = triangles,
            uvs = uvs,
        };
    }

    /// <summary>
    /// Creates a mesh with shared vertices, which means that each vertex is only created once and are used in multiple triangles.
    /// </summary>
    /// <param name="width">The mesh width</param>
    /// <param name="height">The mesh height</param>
    /// <param name="scale">The scale of the mesh</param>
    /// <param name="allocator">The allocator for the native arrays</param>
    /// <returns>The generated mesh data</returns>
    public static MeshData CreateSharedVertexMesh(
        int width,
        int height,
        float scale = 1f,
        Allocator allocator = Allocator.Temp
    )
    {
        // Unique vertices: a grid of (width+1) × (height+1)
        int vertexCountUnique = (width + 1) * (height + 1);
        int triangleCount = width * height * 2; // 2 triangles per quad
        int indiceCount = triangleCount * 3; // 3 indices per triangle

        var vertices = new NativeArray<float3>(vertexCountUnique, allocator);
        var triangles = new NativeArray<int>(indiceCount, allocator);
        var uvs = new NativeArray<float2>(vertexCountUnique, allocator);

        // Step 1: Create the grid of unique vertices
        for (int y = 0; y <= height; y++)
        {
            for (int x = 0; x <= width; x++)
            {
                int vertexIndex = y * (width + 1) + x;
                vertices[vertexIndex] = new float3(x * scale, 0, y * scale);
                uvs[vertexIndex] = new float2((float)x / width, (float)y / height);
            }
        }

        // Step 2: Create triangles by referencing those vertices
        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Grid indices for this quad's corners
                int bottomLeft = y * (width + 1) + x;
                int bottomRight = y * (width + 1) + (x + 1);
                int topLeft = (y + 1) * (width + 1) + x;
                int topRight = (y + 1) * (width + 1) + (x + 1);

                // Triangle 1
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;

                // Triangle 2
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }

        return new MeshData
        {
            vertices = vertices,
            triangles = triangles,
            uvs = uvs,
        };
    }

    /// <summary>
    /// Gets the total number of vertices needed to create a plane mesh based on the given width and height.
    /// </summary>
    /// <param name="width">The mesh width</param>
    /// <param name="height">The mesh height</param>
    /// <returns>The total number of vertices</returns>
    public static int GetMeshVertices(int width, int height)
    {
        // The mesh vertices are calculated as the number of quads (width * height) multiplied by the number of vertices per quad.
        return width * height * QUAD_VERTICES;
    }

    public static void SetVertexData(
        NativeArray<float3> vertices,
        NativeArray<int> triangles,
        NativeArray<float2> uvs,
        int index,
        float3 vertex,
        float uvX,
        float uvY
    )
    {
        vertices[index] = vertex;
        triangles[index] = index;
        uvs[index] = new float2(uvX, uvY);
    }

    /// <summary>
    /// Converts a flat triangle array to int3 format
    /// Flat is used for Unity Mesh.
    /// Int3 is used for Unity Physics.
    /// Example: [0, 1, 2, 3, 4, 5] -> [(0, 1, 2), (3, 4, 5)]
    /// </summary>
    /// <param name="flatTriangles">The flat array of triangle indices.</param>
    /// <param name="allocator">The allocator to use for the NativeArray.</param>
    /// <returns>A NativeArray of int3 representing the triangles.</returns>
    public static NativeArray<int3> ToInt3Triangles(
        NativeArray<int> flatTriangles,
        Allocator allocator = Allocator.Temp
    )
    {
        int triangleCount = flatTriangles.Length / 3;
        NativeArray<int3> int3Triangles = new(triangleCount, allocator);

        for (int i = 0; i < triangleCount; i++)
        {
            int3Triangles[i] = new int3(
                flatTriangles[i * 3 + 0],
                flatTriangles[i * 3 + 1],
                flatTriangles[i * 3 + 2]
            );
        }

        return int3Triangles;
    }

    public static float2 PixelToUV(int pixelX, int pixelY, int textureWidth, int textureHeight)
    {
        return new float2((float)pixelX / textureWidth, (float)pixelY / textureHeight);
    }

    /// <summary>
    /// Calculates the normals for a mesh given its vertices and triangle indices.
    /// </summary>
    /// <param name="vertices">The array of vertices.</param>
    /// <param name="indices">The array of triangle indices.</param>
    /// <param name="allocator">The allocator to use for the NativeArray.</param>
    /// <returns>A NativeArray of float3 representing the normals.</returns>
    public static NativeArray<float3> CalculateNormals(
        NativeArray<float3> vertices,
        NativeArray<int> indices,
        Allocator allocator = Allocator.Temp
    )
    {
        NativeArray<float3> normals = new(vertices.Length, allocator);

        // For each triangle, calculate its normal and add it to each vertex
        for (int i = 0; i < indices.Length; i += 3)
        {
            // A single triangle:
            int i0 = indices[i];
            int i1 = indices[i + 1];
            int i2 = indices[i + 2];

            // The vertices of the triangle:
            float3 v0 = vertices[i0];
            float3 v1 = vertices[i1];
            float3 v2 = vertices[i2];

            // Calculate face normal using cross product
            float3 edge1 = v1 - v0;
            float3 edge2 = v2 - v0;
            float3 faceNormal = math.normalize(math.cross(edge1, edge2));

            // Add to each vertex's normal
            normals[i0] += faceNormal;
            normals[i1] += faceNormal;
            normals[i2] += faceNormal;
        }

        // Normalize all normals
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = math.normalize(normals[i]);
        }

        return normals;
    }
}
