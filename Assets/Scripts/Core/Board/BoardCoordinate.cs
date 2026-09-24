using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Board
{
    /// Static utility and extension methods for 2D discrete grid coordinates (Vector2Int).
    /// Provides cardinal/diagonal direction constants, distance metrics, and vector operations.
    public static class BoardCoordinate
    {
        // Cardinal Directions
        public static readonly Vector2Int North = new Vector2Int(0, 1);
        public static readonly Vector2Int South = new Vector2Int(0, -1);
        public static readonly Vector2Int East = new Vector2Int(1, 0);
        public static readonly Vector2Int West = new Vector2Int(-1, 0);

        // Aliases for intuitive directional reading
        public static readonly Vector2Int Up = North;
        public static readonly Vector2Int Down = South;
        public static readonly Vector2Int Right = East;
        public static readonly Vector2Int Left = West;

        // Diagonal Directions
        public static readonly Vector2Int NorthEast = new Vector2Int(1, 1);
        public static readonly Vector2Int SouthEast = new Vector2Int(1, -1);
        public static readonly Vector2Int SouthWest = new Vector2Int(-1, -1);
        public static readonly Vector2Int NorthWest = new Vector2Int(-1, 1);

        /// The 4 standard orthogonal cardinal directions (North, East, South, West).
        public static readonly Vector2Int[] CardinalDirections =
        {
            North,
            East,
            South,
            West
        };

        /// The 4 standard diagonal directions.
        public static readonly Vector2Int[] DiagonalDirections =
        {
            NorthEast,
            SouthEast,
            SouthWest,
            NorthWest
        };

        /// All 8 adjacent directions (cardinals + diagonals).
        public static readonly Vector2Int[] AllDirections =
        {
            North,
            NorthEast,
            East,
            SouthEast,
            South,
            SouthWest,
            West,
            NorthWest
        };

        /// The 8 standard L-shape jump offsets for Knight movement.
        public static readonly Vector2Int[] KnightOffsets =
        {
            new Vector2Int(1, 2),
            new Vector2Int(2, 1),
            new Vector2Int(2, -1),
            new Vector2Int(1, -2),
            new Vector2Int(-1, -2),
            new Vector2Int(-2, -1),
            new Vector2Int(-2, 1),
            new Vector2Int(-1, 2)
        };

        /// Calculates the Manhattan (L1) distance: |x1 - x2| + |y1 - y2|.
        /// Used for orthogonal path lengths and Rook/Pawn movement validation.
        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        /// Calculates the Chebyshev (L-infinity) distance: max(|x1 - x2|, |y1 - y2|).
        /// Used for King movement or range queries where diagonals count as 1 step.
        public static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        }

        /// Calculates standard Euclidean distance between two grid cells.
        public static float EuclideanDistance(Vector2Int a, Vector2Int b)
        {
            int dx = a.x - b.x;
            int dy = a.y - b.y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// Calculates squared Euclidean distance (avoids square root for comparisons).
        public static int EuclideanDistanceSquared(Vector2Int a, Vector2Int b)
        {
            int dx = a.x - b.x;
            int dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        /// Checks whether a delta vector is a valid 1-step cardinal direction.
        public static bool IsCardinal(Vector2Int delta)
        {
            return (Math.Abs(delta.x) == 1 && delta.y == 0) ||
                   (Math.Abs(delta.y) == 1 && delta.x == 0);
        }

        /// Checks whether a delta vector is a valid 1-step diagonal direction.
        public static bool IsDiagonal(Vector2Int delta)
        {
            return Math.Abs(delta.x) == 1 && Math.Abs(delta.y) == 1;
        }

        /// Checks whether a delta vector is a valid Knight L-jump.
        public static bool IsKnightOffset(Vector2Int delta)
        {
            int ax = Math.Abs(delta.x);
            int ay = Math.Abs(delta.y);
            return (ax == 1 && ay == 2) || (ax == 2 && ay == 1);
        }

        /// Returns true if two coordinates are cardinally adjacent (Manhattan distance == 1).
        public static bool IsAdjacentCardinal(Vector2Int a, Vector2Int b)
        {
            return ManhattanDistance(a, b) == 1;
        }

        /// Returns true if two coordinates are adjacent in any of the 8 directions (Chebyshev distance == 1).
        public static bool IsAdjacentChebyshev(Vector2Int a, Vector2Int b)
        {
            return ChebyshevDistance(a, b) == 1;
        }

        /// Returns a normalized sign direction vector (-1, 0, or 1 for each axis).
        public static Vector2Int GetSignVector(Vector2Int delta)
        {
            return new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
        }

        /// Resolves the dominant cardinal direction from 'from' to 'to'.
        /// Prioritizes the axis with larger displacement.
        public static Vector2Int GetDominantCardinalDirection(Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;
            if (delta == Vector2Int.zero) return Vector2Int.zero;

            if (Math.Abs(delta.x) >= Math.Abs(delta.y))
            {
                return delta.x > 0 ? East : West;
            }
            return delta.y > 0 ? North : South;
        }

        /// Clamps a grid position within [minInclusive, maxInclusive].
        public static Vector2Int Clamp(Vector2Int pos, Vector2Int minInclusive, Vector2Int maxInclusive)
        {
            return new Vector2Int(
                Mathf.Clamp(pos.x, minInclusive.x, maxInclusive.x),
                Mathf.Clamp(pos.y, minInclusive.y, maxInclusive.y)
            );
        }

        /// Converts continuous world coordinates to discrete grid coordinates using floor rounding.
        public static Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        }

        /// Calculates the mathematical center of a grid tile in continuous world space.
        public static Vector3 GridToWorldCenter(Vector2Int gridPos, float z = 0f)
        {
            return new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, z);
        }
    }
}
