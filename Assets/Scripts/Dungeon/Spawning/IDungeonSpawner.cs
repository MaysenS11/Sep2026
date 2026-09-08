using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon.Spawning
{
    public interface IDungeonSpawner
    {
        void SpawnContent(
            GameManager.RoomData room,
            RoomTileQuery tileQuery,
            Tilemap floorTilemap,
            Transform parentContainer
        );

        void ClearSpawnedContent();
    }
}
