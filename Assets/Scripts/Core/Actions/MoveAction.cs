using System;
using System.Collections.Generic;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using UnityEngine;

namespace Core.Actions
{
    /// Moves an occupant along a validated linear path of empty tiles to a target destination.
    /// Updates GameBoard state immediately and emits a MoveEffect.
    public class MoveAction : IBoardAction
    {
        public TileOccupant Occupant { get; }
        public Vector2Int Destination { get; }
        public Vector2Int[] Path { get; }

        public MoveAction(TileOccupant occupant, Vector2Int destination)
        {
            Occupant = occupant ?? throw new ArgumentNullException(nameof(occupant));
            Destination = destination;
            Path = null;
        }

        public MoveAction(TileOccupant occupant, Vector2Int[] path)
        {
            Occupant = occupant ?? throw new ArgumentNullException(nameof(occupant));
            if (path == null || path.Length == 0)
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }
            Path = path;
            Destination = path[path.Length - 1];
        }

        public List<BoardEffect> Execute(GameBoard board)
        {
            var effects = new List<BoardEffect>();
            if (board == null || Occupant == null) return effects;

            Vector2Int startPos = Occupant.GridPosition;
            if (startPos == Destination) return effects;

            List<Vector2Int> resolvedPath = new List<Vector2Int> { startPos };

            if (Path != null && Path.Length > 0)
            {
                for (int i = 0; i < Path.Length; i++)
                {
                    Vector2Int step = Path[i];
                    if (step == startPos) continue;

                    if (!board.CanEnter(step))
                    {
                        return effects; // Path is blocked
                    }
                    resolvedPath.Add(step);
                }
            }
            else
            {
                resolvedPath = new List<Vector2Int> { startPos };
                Vector2Int delta = Destination - startPos;
                int dx = delta.x;
                int dy = delta.y;

                bool isOrthogonal = (dx == 0 && dy != 0) || (dy == 0 && dx != 0);
                bool isDiagonal = Math.Abs(dx) == Math.Abs(dy) && dx != 0;

                if (isOrthogonal || isDiagonal)
                {
                    Vector2Int step = new Vector2Int(Math.Sign(dx), Math.Sign(dy));
                    Vector2Int current = startPos + step;

                    while (current != Destination)
                    {
                        if (!board.CanEnter(current))
                        {
                            return effects; // Intervening tile is blocked
                        }
                        resolvedPath.Add(current);
                        current += step;
                    }

                    if (!board.CanEnter(Destination))
                    {
                        return effects; // Destination is blocked
                    }
                    resolvedPath.Add(Destination);
                }
                else
                {
                    if (!board.CanEnter(Destination))
                    {
                        return effects;
                    }
                    resolvedPath.Add(Destination);
                }
            }

            bool moved = board.Move(Occupant, Destination);
            if (moved)
            {
                effects.Add(new MoveEffect(Occupant.Id, resolvedPath.ToArray()));
            }

            return effects;
        }

        public override string ToString()
        {
            return $"MoveAction: Occupant {Occupant?.Id} from {Occupant?.GridPosition} -> {Destination}";
        }
    }
}
