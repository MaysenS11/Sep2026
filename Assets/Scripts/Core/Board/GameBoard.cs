using System;
using System.Collections.Generic;
using Core.Occupants;
using UnityEngine;

namespace Core.Board
{
    /// Pure authoritative C# 2D simulation board.
    /// Manages grid boundaries, terrain cells, and spatial tile occupancy.
    /// Free of MonoBehaviours, Rigidbodies, colliders, or Unity Physics2D.
    public class GameBoard
    {
        public int Width { get; }
        public int Height { get; }
        public Vector2Int Origin { get; }

        private readonly Dictionary<Vector2Int, BoardCell> _cells = new Dictionary<Vector2Int, BoardCell>();
        private readonly HashSet<Vector2Int> _walls = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, TileOccupant> _occupants = new Dictionary<Vector2Int, TileOccupant>();
        private readonly Dictionary<int, TileOccupant> _occupantsById = new Dictionary<int, TileOccupant>();

        // Simulation board events
        public event Action<TileOccupant, Vector2Int> OnOccupantPlaced;
        public event Action<TileOccupant, Vector2Int> OnOccupantRemoved;
        public event Action<TileOccupant, Vector2Int, Vector2Int> OnOccupantMoved;

        /// Creates a GameBoard with specified width and height, starting at (0, 0) or custom origin.
        public GameBoard(int width, int height, Vector2Int origin = default)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Origin = origin;

            // Initialize default floor cells
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

        /// Checks whether a coordinate is within the board's configured rectangular boundaries.
        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= Origin.x && pos.x < Origin.x + Width &&
                   pos.y >= Origin.y && pos.y < Origin.y + Height;
        }

        /// Checks whether a tile is a solid wall or impassable terrain (including out of bounds and void).
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

        /// Sets the terrain type of a cell at the specified coordinate.
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

        /// Convenience method to mark or unmark a tile as a wall.
        public void SetWall(Vector2Int pos, bool isWall)
        {
            SetCell(pos, isWall ? TerrainType.Wall : TerrainType.Floor);
        }

        /// Gets the BoardCell metadata at a position, or null if uninitialized.
        public BoardCell GetCell(Vector2Int pos)
        {
            _cells.TryGetValue(pos, out var cell);
            return cell;
        }

        #endregion

        #region Spatial Occupancy Queries

        /// Checks if a tile currently contains an active occupant.
        public bool IsOccupied(Vector2Int pos)
        {
            return _occupants.ContainsKey(pos) && _occupants[pos] != null;
        }

        /// Checks if a cell is free to enter (in-bounds, not a wall/void, not occupied).
        public bool CanEnter(Vector2Int pos)
        {
            return IsInBounds(pos) && !IsWall(pos) && !IsOccupied(pos);
        }

        /// Retrieves the occupant residing at a tile, or null if the tile is vacant.
        public TileOccupant GetOccupant(Vector2Int pos)
        {
            _occupants.TryGetValue(pos, out var occupant);
            return occupant;
        }

        /// Attempts to retrieve an occupant of a specific concrete type at the given tile.
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

        /// Retrieves an occupant by its unique ID.
        public TileOccupant GetOccupantById(int id)
        {
            _occupantsById.TryGetValue(id, out var occupant);
            return occupant;
        }

        /// Returns all active occupants currently placed on the board.
        public IReadOnlyCollection<TileOccupant> GetAllOccupants()
        {
            return _occupants.Values;
        }

        /// Returns all occupants of the specified type currently on the board.
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

        /// Finds the primary player occupant on the board.
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

        /// Places an occupant onto the board at a target position.
        /// Fails if position is invalid or already occupied.
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

            // If already on board elsewhere, remove from old position first
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

        /// Force places an occupant, overwriting any existing occupant at that position if necessary.
        /// Primarily used by initial room spawners and test fixtures.
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

        /// Removes an occupant from the board.
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

        /// Removes whatever occupant is currently at the target coordinate.
        public bool RemoveAt(Vector2Int pos)
        {
            if (_occupants.TryGetValue(pos, out var occupant))
            {
                return Remove(occupant);
            }
            return false;
        }

        /// Moves an occupant to a new coordinate instantly in the simulation.
        /// Strictly validates destination bounds, walls, and occupancy.
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

        /// Clears all dynamic occupants from the board.
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

        /// Casts a ray along a directional step vector up to maxSteps.
        /// Returns all visited coordinates until hitting a wall, out of bounds, or an occupant (if stopAtOccupant is true).
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

        /// Checks whether there is a clear, unblocked straight line (cardinal or diagonal) between two points.
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
