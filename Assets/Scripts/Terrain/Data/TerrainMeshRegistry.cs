using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public static class TerrainMeshRegistry
{
    public static Dictionary<Entity, Mesh> Meshes = new();
}
