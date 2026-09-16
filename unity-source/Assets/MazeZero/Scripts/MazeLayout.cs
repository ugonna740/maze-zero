using System;
using System.Collections.Generic;
using UnityEngine;

namespace MazeZero
{
    public sealed class MazeLayout
    {
        [Flags]
        public enum Openings { None = 0, North = 1, East = 2, South = 4, West = 8 }

        public readonly int Width;
        public readonly int Height;
        public readonly Openings[,] Cells;
        public readonly Vector2Int Start;
        public readonly Vector2Int Exit;

        private MazeLayout(int width, int height, Openings[,] cells, Vector2Int exit)
        {
            Width = width;
            Height = height;
            Cells = cells;
            Start = Vector2Int.zero;
            Exit = exit;
        }

        public static MazeLayout Generate(int width, int height, int seed)
        {
            width = Mathf.Max(3, width);
            height = Mathf.Max(3, height);
            var random = new MazeRandom(seed);
            var cells = new Openings[width, height];
            var visited = new bool[width, height];
            var distances = new int[width, height];
            var stack = new Stack<Vector2Int>();
            var current = Vector2Int.zero;
            visited[0, 0] = true;
            stack.Push(current);

            Vector2Int farthest = current;
            var directions = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            var opening = new[] { Openings.North, Openings.East, Openings.South, Openings.West };
            var opposite = new[] { Openings.South, Openings.West, Openings.North, Openings.East };

            while (stack.Count > 0)
            {
                current = stack.Peek();
                var options = new List<int>(4);
                for (var i = 0; i < directions.Length; i++)
                {
                    var next = current + directions[i];
                    if (next.x >= 0 && next.x < width && next.y >= 0 && next.y < height && !visited[next.x, next.y]) options.Add(i);
                }

                if (options.Count == 0) { stack.Pop(); continue; }
                var choice = options[random.Next(options.Count)];
                var neighbour = current + directions[choice];
                cells[current.x, current.y] |= opening[choice];
                cells[neighbour.x, neighbour.y] |= opposite[choice];
                visited[neighbour.x, neighbour.y] = true;
                distances[neighbour.x, neighbour.y] = distances[current.x, current.y] + 1;
                if (distances[neighbour.x, neighbour.y] > distances[farthest.x, farthest.y]) farthest = neighbour;
                stack.Push(neighbour);
            }

            return new MazeLayout(width, height, cells, farthest);
        }
    }
}
