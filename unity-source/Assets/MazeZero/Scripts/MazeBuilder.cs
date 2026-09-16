using System.Collections.Generic;
using UnityEngine;

namespace MazeZero
{
    public sealed class MazeBuilder : MonoBehaviour
    {
        public float CellSize { get; private set; } = 4f;
        public readonly List<Vector2Int> OrbCells = new();

        public Vector3 CellWorld(Vector2Int cell) => new(cell.x * CellSize, 0f, cell.y * CellSize);

        public MazeLayout Build(int width, int height, int seed)
        {
            var layout = MazeLayout.Generate(width, height, seed);
            var random = new MazeRandom(seed ^ 0x5f3759df);
            var floorMaterial = Resources.Load<Material>("MazeZeroGroundTiles")
                ?? MaterialWithColor("Maze Floor", new Color(0.08f, 0.10f, 0.12f));
            var wallMaterial = Resources.Load<Material>("MazeZeroIndustrialWall")
                ?? MaterialWithColor("Maze Walls", new Color(0.18f, 0.22f, 0.25f));

            // A broad dark foundation keeps the elevated camera filled with world geometry.
            CreateCube("World Backdrop", new Vector3((layout.Width - 1) * CellSize * .5f, -.55f, (layout.Height - 1) * CellSize * .5f),
                new Vector3(layout.Width * CellSize * 2.5f, .8f, layout.Height * CellSize * 2.5f), floorMaterial);

            for (var x = 0; x < layout.Width; x++)
            for (var y = 0; y < layout.Height; y++)
            {
                var cell = new Vector2Int(x, y);
                CreateCube("Floor", CellWorld(cell) + Vector3.down * 0.15f, new Vector3(CellSize, 0.3f, CellSize), floorMaterial);
                var openings = layout.Cells[x, y];
                if ((openings & MazeLayout.Openings.South) == 0) CreateWall(CellWorld(cell) + Vector3.back * CellSize * .5f, false, wallMaterial);
                if ((openings & MazeLayout.Openings.West) == 0) CreateWall(CellWorld(cell) + Vector3.left * CellSize * .5f, true, wallMaterial);
                if (y == layout.Height - 1 && (openings & MazeLayout.Openings.North) == 0) CreateWall(CellWorld(cell) + Vector3.forward * CellSize * .5f, false, wallMaterial);
                if (x == layout.Width - 1 && (openings & MazeLayout.Openings.East) == 0) CreateWall(CellWorld(cell) + Vector3.right * CellSize * .5f, true, wallMaterial);
                if (cell != layout.Start && cell != layout.Exit && random.NextDouble() < .48) OrbCells.Add(cell);
            }
            return layout;
        }

        private void CreateWall(Vector3 position, bool alongZ, Material material)
        {
            var scale = alongZ ? new Vector3(.25f, 2.8f, CellSize + .25f) : new Vector3(CellSize + .25f, 2.8f, .25f);
            CreateCube("Wall", position + Vector3.up * 1.4f, scale, material);
        }

        private void CreateCube(string objectName, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(transform);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            var renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            if (material != null && material.name == "Maze Zero Brick Ground")
            {
                // Keep a consistent tile size on individual cells and the broad backdrop.
                var properties = new MaterialPropertyBlock();
                properties.SetVector("_BaseMap_ST", new Vector4(
                    Mathf.Max(1f, scale.x / 2f), Mathf.Max(1f, scale.z / 2f), 0f, 0f));
                renderer.SetPropertyBlock(properties);
            }
        }

        public static Material MaterialWithColor(string materialName, Color color, bool emissive = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = materialName, color = color };
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 3f);
            }
            return material;
        }
    }
}
