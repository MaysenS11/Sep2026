using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Board
{
    public static class BoardCoordinate
    {        public static readonly Vector2Int North = new Vector2Int(0, 1);
        public static readonly Vector2Int South = new Vector2Int(0, -1);
        public static readonly Vector2Int East = new Vector2Int(1, 0);
        public static readonly Vector2Int West = new Vector2Int(-1, 0);

        public static readonly Vector2Int Up = North;
        public static readonly Vector2Int Down = South;
        public static readonly Vector2Int Right = East;
        public static readonly Vector2Int Left = West;

        public static readonly Vector2Int NorthEast = new Vector2Int(1, 1);
        public static readonly Vector2Int SouthEast = new Vector2Int(1, -1);
        public static readonly Vector2Int SouthWest = new Vector2Int(-1, -1);
        public static readonly Vector2Int NorthWest = new Vector2Int(-1, 1);

        public static readonly Vector2Int[] CardinalDirections =
        {
            North,
            East,
            South,
            West
        };

        public static readonly Vector2Int[] DiagonalDirections =
        {
            NorthEast,
            SouthEast,
            SouthWest,
            NorthWest
        };

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

        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

    
        public static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        }

        public static float EuclideanDistance(Vector2Int a, Vector2Int b)
        {
            int dx = a.x - b.x;
            int dy = a.y - b.y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static int EuclideanDistanceSquared(Vector2Int a, Vector2Int b)
        {
            int dx = a.x - b.x;
            int dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        public static bool IsCardinal(Vector2Int delta)
        {
            return (Math.Abs(delta.x) == 1 && delta.y == 0) ||
                   (Math.Abs(delta.y) == 1 && delta.x == 0);
        }

        public static bool IsDiagonal(Vector2Int delta)
        {
            return Math.Abs(delta.x) == 1 && Math.Abs(delta.y) == 1;
        }

        public static bool IsKnightOffset(Vector2Int delta)
        {
            int ax = Math.Abs(delta.x);
            int ay = Math.Abs(delta.y);
            return (ax == 1 && ay == 2) || (ax == 2 && ay == 1);
        }

        public static bool IsAdjacentCardinal(Vector2Int a, Vector2Int b)
        {
            return ManhattanDistance(a, b) == 1;
        }

        public static bool IsAdjacentChebyshev(Vector2Int a, Vector2Int b)
        {
            return ChebyshevDistance(a, b) == 1;
        }

        public static Vector2Int GetSignVector(Vector2Int delta)
        {
            return new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
        }

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

        public static Vector2Int Clamp(Vector2Int pos, Vector2Int minInclusive, Vector2Int maxInclusive)
        {
            return new Vector2Int(
                Mathf.Clamp(pos.x, minInclusive.x, maxInclusive.x),
                Mathf.Clamp(pos.y, minInclusive.y, maxInclusive.y)
            );
        }

        public static Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        }

        public static Vector3 GridToWorldCenter(Vector2Int gridPos, float z = 0f)
        {
            return new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, z);
        }
    }
}
