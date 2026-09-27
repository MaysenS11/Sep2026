using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Core.Occupants;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core.Board
{
    public class DoorInfo
    {
        public Vector2Int Position;
        public string DoorType;
        public int RoomIndex;

        public DoorInfo(Vector2Int position, string doorType, int roomIndex = -1)
        {
            Position = position;
            DoorType = doorType;
            RoomIndex = roomIndex;
        }
    }

    public static class BoardPrinter
    {
        public static string DumpToConsole(GameBoard board = null, bool fullBoard = false)
        {
            string output = PrintToString(board, fullBoard);
            Debug.Log(output);
            return output;
        }

        public static string DumpToFile(string filePath = null, GameBoard board = null, bool fullBoard = false)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.Combine(Application.dataPath, "GameBoard_Dump.txt");
            }

            string output = PrintToString(board, fullBoard);
            File.WriteAllText(filePath, output, Encoding.UTF8);
            Debug.Log($"GameBoard ASCII dump written to: {filePath}");
            return output;
        }

        public static string PrintToString(GameBoard board = null, bool fullBoard = false, Dictionary<Vector2Int, DoorInfo> customDoors = null)
        {
            if (board == null)
            {
                var gm = GameManager.Instance;
                if (gm != null)
                {
                    board = gm.Board;
                }
            }

            if (board == null)
            {
                return "Error: No active GameBoard found to print.";
            }

            var doors = customDoors ?? ExtractDoorsFromGameManager();
            var occupants = board.GetAllOccupants().ToList();

            int minX = board.Origin.x;
            int maxX = board.Origin.x + board.Width - 1;
            int minY = board.Origin.y;
            int maxY = board.Origin.y + board.Height - 1;

            if (!fullBoard)
            {
                bool hasContent = false;
                int actMinX = int.MaxValue;
                int actMaxX = int.MinValue;
                int actMinY = int.MaxValue;
                int actMaxY = int.MinValue;

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        var pos = new Vector2Int(x, y);
                        var cell = board.GetCell(pos);
                        bool isFloor = cell != null && cell.Terrain == TerrainType.Floor;
                        bool hasOcc = board.IsOccupied(pos);
                        bool hasDoor = doors.ContainsKey(pos);

                        if (isFloor || hasOcc || hasDoor)
                        {
                            hasContent = true;
                            actMinX = Math.Min(actMinX, x);
                            actMaxX = Math.Max(actMaxX, x);
                            actMinY = Math.Min(actMinY, y);
                            actMaxY = Math.Max(actMaxY, y);
                        }
                    }
                }

                if (hasContent)
                {
                    int margin = 1;
                    minX = Math.Max(board.Origin.x, actMinX - margin);
                    maxX = Math.Min(board.Origin.x + board.Width - 1, actMaxX + margin);
                    minY = Math.Max(board.Origin.y, actMinY - margin);
                    maxY = Math.Min(board.Origin.y + board.Height - 1, actMaxY + margin);
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== GAMEBOARD ASCII DUMP ===");
            sb.AppendLine($"Board Bounds: Origin=({board.Origin.x},{board.Origin.y}), Size={board.Width}x{board.Height}");
            sb.AppendLine($"Rendered Grid Bounds: X=[{minX}..{maxX}] (Width={maxX - minX + 1}), Y=[{minY}..{maxY}] (Height={maxY - minY + 1})");
            sb.AppendLine();
            sb.AppendLine("Map Legend:");
            sb.AppendLine("  . = Empty floor");
            sb.AppendLine("  # = Wall / Void");
            sb.AppendLine("  P = Player");
            sb.AppendLine("  E = Enemy");
            sb.AppendLine("  B = Barrel (Destructible Prop)");
            sb.AppendLine("  C = Chest");
            sb.AppendLine("  \u220F = Pillar");
            sb.AppendLine("  D = Door (D_in, D_out, D_spec)");
            sb.AppendLine();

            int yPadding = Math.Max(Math.Abs(maxY).ToString().Length, Math.Abs(minY).ToString().Length) + 1;

            AppendXAxisHeader(sb, minX, maxX, yPadding);

            for (int y = maxY; y >= minY; y--)
            {
                sb.Append(y.ToString().PadLeft(yPadding)).Append(" ");
                for (int x = minX; x <= maxX; x++)
                {
                    var pos = new Vector2Int(x, y);
                    char c = GetCellCharacter(board, pos, doors);
                    sb.Append(c);
                }
                sb.Append(" ").Append(y);
                sb.AppendLine();
            }

            AppendXAxisHeader(sb, minX, maxX, yPadding);
            sb.AppendLine();

            AppendOccupantList(sb, occupants);
            AppendDoorList(sb, doors);

            return sb.ToString();
        }

        public static char GetCellCharacter(GameBoard board, Vector2Int pos, Dictionary<Vector2Int, DoorInfo> doors)
        {
            var occupant = board.GetOccupant(pos);
            if (occupant != null)
            {
                if (occupant is PlayerOccupant) return 'P';
                if (occupant is EnemyOccupant) return 'E';
                if (occupant is DestructiblePropOccupant) return 'B';
                if (occupant is ChestOccupant) return 'C';
                if (occupant is PillarOccupant) return '\u220F';
                if (occupant is ObstacleOccupant obs && obs.ObstacleType == ObstacleType.Pillar) return '\u220F';
                return occupant.Name.Length > 0 ? occupant.Name[0] : 'O';
            }

            if (doors != null && doors.ContainsKey(pos))
            {
                return 'D';
            }

            var cell = board.GetCell(pos);
            if (cell != null && cell.Terrain == TerrainType.Floor)
            {
                return '.';
            }

            return '#';
        }

        private static void AppendXAxisHeader(StringBuilder sb, int minX, int maxX, int yPadding)
        {
            int width = maxX - minX + 1;
            sb.Append(new string(' ', yPadding + 1));
            for (int x = minX; x <= maxX; x++)
            {
                if (x % 10 == 0)
                {
                    sb.Append(Math.Abs(x / 10 % 10));
                }
                else
                {
                    sb.Append(' ');
                }
            }
            sb.AppendLine();

            sb.Append(new string(' ', yPadding + 1));
            for (int x = minX; x <= maxX; x++)
            {
                sb.Append(Math.Abs(x % 10));
            }
            sb.AppendLine();
        }

        private static void AppendOccupantList(StringBuilder sb, List<TileOccupant> occupants)
        {
            sb.AppendLine("=== OCCUPANT COORDINATE LIST ===");
            sb.AppendLine($"Total Occupants: {occupants.Count}");

            var pillarGroups = new Dictionary<Vector2Int, List<PillarOccupant>>();
            var nonPillarOccupants = new List<TileOccupant>();

            for (int i = 0; i < occupants.Count; i++)
            {
                if (occupants[i] is PillarOccupant pillar)
                {
                    if (!pillarGroups.TryGetValue(pillar.FootprintOrigin, out var list))
                    {
                        list = new List<PillarOccupant>();
                        pillarGroups[pillar.FootprintOrigin] = list;
                    }
                    list.Add(pillar);
                }
                else
                {
                    nonPillarOccupants.Add(occupants[i]);
                }
            }

            nonPillarOccupants.Sort((a, b) => a.Id.CompareTo(b.Id));
            for (int i = 0; i < nonPillarOccupants.Count; i++)
            {
                var occ = nonPillarOccupants[i];
                string typeCategory = GetOccupantCategory(occ);
                string detail = GetOccupantDetail(occ);
                sb.AppendLine($"  [ID: {occ.Id,3}] {typeCategory,-6} \"{occ.Name}\" | Pos: ({occ.GridPosition.x,3}, {occ.GridPosition.y,3}) | Dim: 1x1 | {detail}");
            }

            if (pillarGroups.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- Multi-Tile Pillar Boundaries ---");
                var sortedOrigins = pillarGroups.Keys.OrderBy(k => k.x).ThenBy(k => k.y).ToList();
                for (int i = 0; i < sortedOrigins.Count; i++)
                {
                    var origin = sortedOrigins[i];
                    var cells = pillarGroups[origin];
                    cells.Sort((a, b) => a.GridPosition.x != b.GridPosition.x ? a.GridPosition.x.CompareTo(b.GridPosition.x) : a.GridPosition.y.CompareTo(b.GridPosition.y));
                    var first = cells[0];
                    Vector2Int size = first.FootprintSize;
                    Vector2Int maxCoord = origin + size - Vector2Int.one;

                    sb.AppendLine($"  Pillar Footprint: Origin=({origin.x},{origin.y}) to Max=({maxCoord.x},{maxCoord.y}) | Dim: {size.x}x{size.y} ({cells.Count} cells registered)");
                    sb.Append("    Registered Tiles: ");
                    for (int c = 0; c < cells.Count; c++)
                    {
                        var cell = cells[c];
                        sb.Append($"[ID:{cell.Id} at ({cell.GridPosition.x},{cell.GridPosition.y}) offset ({cell.LocalOffset.x},{cell.LocalOffset.y})]");
                        if (c < cells.Count - 1) sb.Append(", ");
                    }
                    sb.AppendLine();
                }
            }
            sb.AppendLine();
        }

        private static string GetOccupantCategory(TileOccupant occ)
        {
            if (occ is PlayerOccupant) return "PLAYER";
            if (occ is EnemyOccupant) return "ENEMY";
            if (occ is DestructiblePropOccupant) return "PROP";
            if (occ is ChestOccupant) return "CHEST";
            if (occ is ObstacleOccupant) return "OBSTACLE";
            return "OTHER";
        }

        private static string GetOccupantDetail(TileOccupant occ)
        {
            if (occ is PlayerOccupant player)
            {
                return $"HP: {player.CurrentHealth}/{player.MaxHealth}, Atk: {player.AttackDamage}";
            }
            if (occ is EnemyOccupant enemy)
            {
                return $"Archetype: {enemy.Archetype}, HP: {enemy.CurrentHealth}/{enemy.MaxHealth}, Atk: {enemy.AttackDamage}, Boss: {enemy.IsBoss}";
            }
            if (occ is DestructiblePropOccupant prop)
            {
                return $"PropType: {prop.PropType}, HP: {prop.CurrentHealth}/{prop.MaxHealth}";
            }
            if (occ is ChestOccupant chest)
            {
                return $"IsOpen: {chest.IsOpen}, ChestRoom: {chest.IsChestRoom}";
            }
            return $"HP: {occ.CurrentHealth}/{occ.MaxHealth}";
        }

        private static void AppendDoorList(StringBuilder sb, Dictionary<Vector2Int, DoorInfo> doors)
        {
            sb.AppendLine("=== DOOR COORDINATE LIST ===");
            sb.AppendLine($"Total Doors: {doors.Count}");
            if (doors.Count == 0)
            {
                sb.AppendLine("  (No doors registered)");
                return;
            }

            var sortedDoors = doors.Values.OrderBy(d => d.RoomIndex).ThenBy(d => d.DoorType).ToList();
            for (int i = 0; i < sortedDoors.Count; i++)
            {
                var d = sortedDoors[i];
                string roomStr = d.RoomIndex >= 0 ? $"Room {d.RoomIndex}" : "Unknown Room";
                sb.AppendLine($"  {d.DoorType,-6} at ({d.Position.x,3}, {d.Position.y,3}) | {roomStr} | Dim: 1x1");
            }
            sb.AppendLine();
        }

        public static Dictionary<Vector2Int, DoorInfo> ExtractDoorsFromGameManager()
        {
            var doors = new Dictionary<Vector2Int, DoorInfo>();
            var gm = GameManager.Instance;
            if (gm == null || gm.DungeonDictionary == null) return doors;

            foreach (var kvp in gm.DungeonDictionary)
            {
                int roomIdx = kvp.Key;
                var room = kvp.Value;

                if (room.EntranceDoorTile.HasValue)
                {
                    string tag = room.Type == RoomType.Chest ? "D_spec" : "D_in";
                    doors[room.EntranceDoorTile.Value] = new DoorInfo(room.EntranceDoorTile.Value, tag, roomIdx);
                }

                if (room.ExitDoorTile.HasValue)
                {
                    string tag = room.ParentRoomIndex != -1 && room.HasSpecialChestRoom ? "D_spec" : "D_out";
                    doors[room.ExitDoorTile.Value] = new DoorInfo(room.ExitDoorTile.Value, tag, roomIdx);
                }

                if (room.SpecialEntryDoorPosition.HasValue)
                {
                    var pos = new Vector2Int(Mathf.FloorToInt(room.SpecialEntryDoorPosition.Value.x), Mathf.FloorToInt(room.SpecialEntryDoorPosition.Value.y));
                    if (!doors.ContainsKey(pos))
                    {
                        doors[pos] = new DoorInfo(pos, "D_spec", roomIdx);
                    }
                }

                if (room.SpecialExitDoorPosition.HasValue)
                {
                    var pos = new Vector2Int(Mathf.FloorToInt(room.SpecialExitDoorPosition.Value.x), Mathf.FloorToInt(room.SpecialExitDoorPosition.Value.y));
                    if (!doors.ContainsKey(pos))
                    {
                        doors[pos] = new DoorInfo(pos, "D_spec", roomIdx);
                    }
                }
            }

            return doors;
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Board/Dump Board to Console")]
        public static void MenuItemDumpToConsole()
        {
            DumpToConsole();
        }

        [MenuItem("Tools/Board/Dump Board to File (Assets/GameBoard_Dump.txt)")]
        public static void MenuItemDumpToFile()
        {
            string path = Path.Combine(Application.dataPath, "GameBoard_Dump.txt");
            DumpToFile(path);
        }
#endif
    }
}
