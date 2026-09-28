using System;
using System.Collections.Generic;
using Core.Occupants;
using UnityEngine;

namespace Core.Board
{
    public class GameBoard
    {
        public int Width { get; }
        public int Height { get; }
        public Vector2Int Origin { get; }

        private readonly Dictionary<Vector2Int, BoardCell> _cells = new Dictionary<Vector2Int, BoardCell>();
        private readonly HashSet<Vector2Int> _walls = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, TileOccupant> _occupants = new Dictionary<Vector2Int, TileOccupant>();
        private readonly Dictionary<int, TileOccupant> _occupantsById = new Dictionary<int, TileOccupant>();

        public event Action<TileOccupant, Vector2Int> OnOccupantPlaced;
        public event Action<TileOccupant, Vector2Int> OnOccupantRemoved;
        public event Action<TileOccupant, Vector2Int, Vector2Int> OnOccupantMoved;

        public GameBoard(int width, int height, Vector2Int origin = default)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Origin = origin;

            for (int x = Origin.x; x < Origin.x + Width; x++)
            {
                for (int y = Origin.y; y < Origin.y + Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    _cells[pos] = new BoardCell(pos, TerrainType.Floor);
                }
            }
        }

        #region Bounds & Terrain Queries

        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= Origin.x && pos.x < Origin.x + Width &&
                   pos.y >= Origin.y && pos.y < Origin.y + Height;
        }

        public bool IsWall(Vector2Int pos)
        {
            if (!IsInBounds(pos)) return true;
            if (_walls.Contains(pos)) return true;

            if (_cells.TryGetValue(pos, out var cell))
            {
                return cell.Terrain == TerrainType.Wall || cell.Terrain == TerrainType.Void;
            }

            return false;
        }

        public void SetCell(Vector2Int pos, TerrainType terrain)
        {
            if (_cells.TryGetValue(pos, out var cell))
            {
                cell.Terrain = terrain;
            }
            else
            {
                _cells[pos] = new BoardCell(pos, terrain);
            }

            if (terrain == TerrainType.Wall || terrain == TerrainType.Void)
            {
                _walls.Add(pos);
            }
            else
            {
                _walls.Remove(pos);
            }
        }

        public void SetWall(Vector2Int pos, bool isWall)
        {
            SetCell(pos, isWall ? TerrainType.Wall : TerrainType.Floor);
        }

        public BoardCell GetCell(Vector2Int pos)
        {
            _cells.TryGetValue(pos, out var cell);
            return cell;
        }

        #endregion

        #region Spatial Occupancy Queries

        public bool IsOccupied(Vector2Int pos)
        {
            return _occupants.ContainsKey(pos) && _occupants[pos] != null;
        }

        public bool CanEnter(Vector2Int pos)
        {
            return IsInBounds(pos) && !IsWall(pos) && !IsOccupied(pos);
        }

        public TileOccupant GetOccupant(Vector2Int pos)
        {
            _occupants.TryGetValue(pos, out var occupant);
            return occupant;
        }

        public bool TryGetOccupant<T>(Vector2Int pos, out T occupant) where T : TileOccupant
        {
            if (_occupants.TryGetValue(pos, out var raw) && raw is T typed)
            {
                occupant = typed;
                return true;
            }
            occupant = null;
            return false;
        }

        public TileOccupant GetOccupantById(int id)
        {
            _occupantsById.TryGetValue(id, out var occupant);
            return occupant;
        }

        public IReadOnlyCollection<TileOccupant> GetAllOccupants()
        {
            return _occupants.Values;
        }

        public IEnumerable<T> GetOccupantsOfType<T>() where T : TileOccupant
        {
            foreach (var occupant in _occupants.Values)
            {
                if (occupant is T typed)
                {
                    yield return typed;
                }
            }
        }

        public PlayerOccupant FindPlayer()
        {
            foreach (var occupant in _occupants.Values)
            {
                if (occupant is PlayerOccupant player)
                {
                    return player;
                }
            }
            return null;
        }

        #endregion

        #region Board Mutation (Place, Remove, Move)

        public bool Place(TileOccupant occupant, Vector2Int pos)
        {
            if (occupant == null)
            {
                throw new ArgumentNullException(nameof(occupant));
            }

            if (!CanEnter(pos))
            {
                return false;
            }

            if (_occupantsById.ContainsKey(occupant.Id))
            {
                Remove(occupant);
            }

            occupant.GridPosition = pos;
            _occupants[pos] = occupant;
            _occupantsById[occupant.Id] = occupant;

            OnOccupantPlaced?.Invoke(occupant, pos);
            return true;
        }

        public void ForcePlace(TileOccupant occupant, Vector2Int pos)
        {
            if (occupant == null) throw new ArgumentNullException(nameof(occupant));

            if (_occupants.TryGetValue(pos, out var existing) && existing != occupant)
            {
                Remove(existing);
            }

            if (_occupantsById.ContainsKey(occupant.Id))
            {
                Remove(occupant);
            }

            occupant.GridPosition = pos;
            _occupants[pos] = occupant;
            _occupantsById[occupant.Id] = occupant;

            OnOccupantPlaced?.Invoke(occupant, pos);
        }

        public bool Remove(TileOccupant occupant)
        {
            if (occupant == null) return false;

            if (_occupants.TryGetValue(occupant.GridPosition, out var current) && current == occupant)
            {
                _occupants.Remove(occupant.GridPosition);
            }

            bool removed = _occupantsById.Remove(occupant.Id);
            if (removed)
            {
                OnOccupantRemoved?.Invoke(occupant, occupant.GridPosition);
            }

            return removed;
        }

        public bool RemoveAt(Vector2Int pos)
        {
            if (_occupants.TryGetValue(pos, out var occupant))
            {
                return Remove(occupant);
            }
            return false;
        }

        public bool Move(TileOccupant occupant, Vector2Int newPos)
        {
            if (occupant == null) return false;

            if (!_occupantsById.ContainsKey(occupant.Id))
            {
                return false;
            }

            Vector2Int oldPos = occupant.GridPosition;
            if (oldPos == newPos) return true;

            if (!CanEnter(newPos))
            {
                return false;
            }

            _occupants.Remove(oldPos);
            occupant.GridPosition = newPos;
            _occupants[newPos] = occupant;

            OnOccupantMoved?.Invoke(occupant, oldPos, newPos);
            return true;
        }

        public void ClearOccupants()
        {
            var all = new List<TileOccupant>(_occupants.Values);
            foreach (var occ in all)
            {
                Remove(occ);
            }
        }

        #endregion

        #region Spatial Line Queries (Raycasting)

        public List<Vector2Int> Raycast(Vector2Int start, Vector2Int direction, int maxSteps, bool stopAtOccupant = true)
        {
            var result = new List<Vector2Int>();
            if (direction == Vector2Int.zero || maxSteps <= 0) return result;

            Vector2Int current = start;
            for (int i = 0; i < maxSteps; i++)
            {
                current += direction;

                if (!IsInBounds(current) || IsWall(current))
                {
                    break;
                }

                result.Add(current);

                if (stopAtOccupant && IsOccupied(current))
                {
                    break;
                }
            }

            return result;
        }

        public bool HasClearLineOfSight(Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;
            int dx = delta.x;
            int dy = delta.y;

            bool isOrthogonal = (dx == 0 && dy != 0) || (dy == 0 && dx != 0);
            bool isDiagonal = Math.Abs(dx) == Math.Abs(dy) && dx != 0;

            if (!isOrthogonal && !isDiagonal) return false;

            Vector2Int step = new Vector2Int(Math.Sign(dx), Math.Sign(dy));
            Vector2Int current = from + step;

            while (current != to)
            {
                if (!IsInBounds(current) || IsWall(current) || IsOccupied(current))
                {
                    return false;
                }
                current += step;
            }

            return true;
        }

        #endregion
    }
}
