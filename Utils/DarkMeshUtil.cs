using System.Collections.Generic;
using Vintagestory.API.Client;

namespace CommonLib.Utils
{
    public static class DarkMeshUtil
    {
        public static MeshData GetRectangle(float totalSize, int gridSize)
        {
            var quadSize = totalSize / gridSize;
            var halfSize = totalSize / 2.0f;

            var vertices = new List<float>();
            var uvs = new List<float>();
            var indices = new List<int>();

            // Vertices and uvs
            for (int y = 0; y <= gridSize; y++)
            {
                for (int x = 0; x <= gridSize; x++)
                {
                    vertices.Add(x * quadSize - halfSize);
                    vertices.Add(y * quadSize - halfSize);
                    vertices.Add(0);
                    uvs.Add((float)x / gridSize);
                    uvs.Add((float)y / gridSize);
                }
            }

            // Indices
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    int topLeft = y * (gridSize + 1) + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + (gridSize + 1);
                    int bottomRight = bottomLeft + 1;

                    // First triangle
                    indices.Add(topLeft);
                    indices.Add(bottomLeft);
                    indices.Add(topRight);

                    // Second triangle
                    indices.Add(topRight);
                    indices.Add(bottomLeft);
                    indices.Add(bottomRight);
                }
            }

            var meshData = new MeshData();
            meshData.SetXyz(vertices.ToArray());
            meshData.SetUv(uvs.ToArray());
            meshData.SetVerticesCount(vertices.Count / 3);
            meshData.SetIndices(indices.ToArray());
            meshData.SetIndicesCount(indices.Count);

            return meshData;
        }
    }
}
