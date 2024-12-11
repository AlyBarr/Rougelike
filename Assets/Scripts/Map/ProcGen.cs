using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

sealed class ProcGen {
    // Generate a new dungeon map.
    public void GenerateDungeon(int mapWidth, int mapHeight, int roomMaxSize, int roomMinSize, int maxRooms, int maxMonstersPerRoom, int maxItemsPerRoom, List<RectangularRoom> rooms) {
        // Generate the rooms.
        for (int roomNum = 0; roomNum < maxRooms; roomNum++) {
            int roomWidth = Random.Range(roomMinSize, roomMaxSize);
            int roomHeight = Random.Range(roomMinSize, roomMaxSize);

            int roomX = Random.Range(0, mapWidth - roomWidth);
            int roomY = Random.Range(0, mapHeight - roomHeight);

            // Ensure the room is within bounds.
            roomX = Mathf.Clamp(roomX, 0, mapWidth - roomWidth);
            roomY = Mathf.Clamp(roomY, 0, mapHeight - roomHeight);

            RectangularRoom newRoom = new RectangularRoom(roomX, roomY, roomWidth, roomHeight);

            // Check if this room intersects with any other rooms
            if (newRoom.Overlaps(rooms)) {
                continue;
            }

            // Dig out this room's inner area and build the walls.
            for (int x = roomX; x < roomX + roomWidth; x++) {
                for (int y = roomY; y < roomY + roomHeight; y++) {
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    if (x == roomX || x == roomX + roomWidth - 1 || y == roomY || y == roomY + roomHeight - 1) {
                        if (SetWallTileIfEmpty(pos)) {
                            continue;
                        }
                    } else {
                        SetFloorTile(pos);
                    }
                }
            }

            if (rooms.Count != 0) {
                // Dig out a tunnel between this room and the previous one.
                TunnelBetween(rooms[rooms.Count - 1], newRoom);
            }

            PlaceEntities(newRoom, maxMonstersPerRoom, maxItemsPerRoom);

            rooms.Add(newRoom);
        }

        // Get the first room where the player should spawn
        RectangularRoom firstRoom = rooms[0];
        Vector2Int center2D = new Vector2Int(
            firstRoom.X + firstRoom.Width / 2,
            firstRoom.Y + firstRoom.Height / 2
        );

        // Ensure the spawn position is inside the room and not on the walls.
        Vector3Int spawnPosition = new Vector3Int(
            Mathf.Clamp(center2D.x, firstRoom.X + 1, firstRoom.X + firstRoom.Width - 1),
            Mathf.Clamp(center2D.y, firstRoom.Y + 1, firstRoom.Y + firstRoom.Height - 1),
            0
        );

        // Log final spawn position
        Debug.Log($"Final Spawn Position: ({spawnPosition.x}, {spawnPosition.y})");

        // Convert the spawn position to Vector2 for spawning (for MapManager)
        Vector2 spawnPosition2D = MapManager.instance.FloorMap.CellToWorld(spawnPosition);
        MapManager.instance.CreateEntity("Player", spawnPosition2D);
    }

    // Return an L-shaped tunnel between these two points using Bresenham lines.
    private void TunnelBetween(RectangularRoom oldRoom, RectangularRoom newRoom) {
        Vector2Int oldRoomCenter = oldRoom.Center();
        Vector2Int newRoomCenter = newRoom.Center();
        Vector2Int tunnelCorner;

        if (Random.value < 0.5f) {
            // Move horizontally, then vertically.
            tunnelCorner = new Vector2Int(newRoomCenter.x, oldRoomCenter.y);
        } else {
            // Move vertically, then horizontally.
            tunnelCorner = new Vector2Int(oldRoomCenter.x, newRoomCenter.y);
        }

        // Generate the coordinates for this tunnel.
        List<Vector2Int> tunnelCoords = new List<Vector2Int>();
        BresenhamLine.Compute(oldRoomCenter, tunnelCorner, tunnelCoords);
        BresenhamLine.Compute(tunnelCorner, newRoomCenter, tunnelCoords);

        // Set the tiles for this tunnel.
        foreach (Vector2Int tunnelCoord in tunnelCoords) {
            Vector3Int pos = new Vector3Int(tunnelCoord.x, tunnelCoord.y, 0);
            SetFloorTile(pos);

            // Set the wall tiles around this tile to be walls.
            for (int x = tunnelCoord.x - 1; x <= tunnelCoord.x + 1; x++) {
                for (int y = tunnelCoord.y - 1; y <= tunnelCoord.y + 1; y++) {
                    SetWallTileIfEmpty(new Vector3Int(x, y, 0));
                }
            }
        }
    }

    private bool SetWallTileIfEmpty(Vector3Int pos) {
        if (MapManager.instance.FloorMap.GetTile(pos)) {
            return true;
        } else {
            MapManager.instance.ObstacleMap.SetTile(pos, MapManager.instance.WallTile);
            return false;
        }
    }

    private void SetFloorTile(Vector3Int pos) {
        if (MapManager.instance.ObstacleMap.GetTile(pos)) {
            MapManager.instance.ObstacleMap.SetTile(pos, null);
        }
        MapManager.instance.FloorMap.SetTile(pos, MapManager.instance.FloorTile);
    }

    private void PlaceEntities(RectangularRoom newRoom, int maximumMonsters, int maximumItems) {
        // Debug: Output current room bounds and entity count for monsters and items
        Debug.Log($"Placing entities in room at ({newRoom.X}, {newRoom.Y}) with size ({newRoom.Width}, {newRoom.Height})");

        // Limit the number of entities based on max number.
        int numberOfMonsters = Mathf.Min(Random.Range(0, maximumMonsters + 1), 2);
        int numberOfItems = Mathf.Min(Random.Range(0, maximumItems + 1), 2);

        Debug.Log($"Number of Monsters: {numberOfMonsters}");
        Debug.Log($"Number of Items: {numberOfItems}");

        // Place monsters
        for (int monster = 0; monster < numberOfMonsters; monster++) {
            // Generate random position inside room, avoiding walls
            int x = Random.Range(newRoom.X + 1, newRoom.X + newRoom.Width - 1);
            int y = Random.Range(newRoom.Y + 1, newRoom.Y + newRoom.Height - 1);

            // Ensure the position is within the bounds of the map and not occupied
            if (IsTileOccupied(x, y)) {
                Debug.Log($"Monster skipped at position ({x}, {y}) because it is occupied or out of bounds.");
                continue;
            }

            // Spawn a monster at the valid position
            string monsterType = Random.value < 0.8f ? "FlyMob" : "FireSprite";
            MapManager.instance.CreateEntity(monsterType, new Vector2(x, y));
            Debug.Log($"Monster spawned at ({x}, {y})");
        }

        // Place items
        for (int item = 0; item < numberOfItems; item++) {
            // Generate random position inside room, avoiding walls
            int x = Random.Range(newRoom.X + 1, newRoom.X + newRoom.Width - 1);
            int y = Random.Range(newRoom.Y + 1, newRoom.Y + newRoom.Height - 1);

            // Ensure the position is within the bounds of the map and not occupied
            if (IsTileOccupied(x, y)) {
                Debug.Log($"Item skipped at position ({x}, {y}) because it is occupied or out of bounds.");
                continue;
            }

            // Spawn an item at the valid position
            string itemType = Random.value < 0.7f ? "Potion of Heart" : Random.value < 0.8f ? "Fireball Scroll" : "Lightning Scroll";
            MapManager.instance.CreateEntity(itemType, new Vector2(x, y));
            Debug.Log($"Item spawned at ({x}, {y})");
        }
    }

    private bool IsTileOccupied(int x, int y) {
        // Ensure the position is inside bounds of the map.
        if (!MapManager.instance.InBounds(x, y)) {
            return true; // Out of bounds, treat as occupied.
        }

        // Check if the tile is occupied by an existing actor (e.g., player, monster, item)
        foreach (var actor in GameManager.instance.Actors) {
            Vector3Int actorPos = MapManager.instance.FloorMap.WorldToCell(actor.transform.position);
            if (actorPos.x == x && actorPos.y == y) {
                return true; // Occupied by an actor.
            }
        }

        foreach (var entity in GameManager.instance.Entities) {
            Vector3Int entityPos = MapManager.instance.FloorMap.WorldToCell(entity.transform.position);
            if (entityPos.x == x && entityPos.y == y) {
                return true; // Occupied by another entity (item).
            }
        }

        // Check if the tile is a wall or obstacle.
        TileBase tile = MapManager.instance.ObstacleMap.GetTile(new Vector3Int(x, y, 0));
        if (tile != null) {
            return true; // Tile is a wall or obstacle.
        }

        return false; // Tile is free.
    }
}
