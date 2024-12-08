using System.Collections.Generic;
using UnityEngine;

sealed class ProcGen {

  /// Generate a new dungeon map.
  public void GenerateDungeon(int mapWidth, int mapHeight, int roomMaxSize, int roomMinSize, int maxRooms, int maxMonstersPerRoom, int maxItemsPerRoom, List<RectangularRoom> rooms) {
    for (int roomNum = 0; roomNum < maxRooms; roomNum++) {
        int roomWidth = Random.Range(roomMinSize, roomMaxSize);
        int roomHeight = Random.Range(roomMinSize, roomMaxSize);

        // Ensure the room is placed within the map boundaries.
        int roomX = Random.Range(0, mapWidth - roomWidth - 1);
        int roomY = Random.Range(0, mapHeight - roomHeight - 1);

        RectangularRoom newRoom = new RectangularRoom(roomX, roomY, roomWidth, roomHeight);

        // Log room size and placement
        Debug.Log($"Room {roomNum}: Position ({roomX}, {roomY}), Size ({roomWidth}, {roomHeight})");

        // Check for overlap with existing rooms.
        if (newRoom.Overlaps(rooms)) {
            continue;
        }

        // Dig out the room (floor and walls).
        for (int x = roomX; x < roomX + roomWidth; x++) {
            for (int y = roomY; y < roomY + roomHeight; y++) {
                if (x == roomX || x == roomX + roomWidth - 1 || y == roomY || y == roomY + roomHeight - 1) {
                    if (SetWallTileIfEmpty(new Vector3Int(x, y))) {
                        continue;
                    }
                } else {
                    SetFloorTile(new Vector3Int(x, y));
                }
            }
        }

        // Dig tunnels between rooms.
        if (rooms.Count != 0) {
            TunnelBetween(rooms[rooms.Count - 1], newRoom);
        }

        // Place monsters in the room.
        PlaceEntities(rooms, maxMonstersPerRoom, maxItemsPerRoom);
        rooms.Add(newRoom);
    }

    // Get the first room where the player should spawn
    RectangularRoom firstRoom = rooms[0];

    // Calculate the center of the first room
    Vector2Int center2D = new Vector2Int(
        firstRoom.X + firstRoom.Width / 2,
        firstRoom.Y + firstRoom.Height / 2
    );

    // Log the room center
    Debug.Log($"Room Center for Room 0: ({center2D.x}, {center2D.y})");

    // Ensure the spawn position is within the room's bounds and not on the walls
    Vector3Int spawnPosition = new Vector3Int(center2D.x, center2D.y, 0);

    if (spawnPosition.x <= firstRoom.X + 1 || spawnPosition.x >= firstRoom.X + firstRoom.Width - 1 ||
        spawnPosition.y <= firstRoom.Y + 1 || spawnPosition.y >= firstRoom.Y + firstRoom.Height - 1) {
        // If the spawn position is on the wall, randomize it inside the room.
        spawnPosition = new Vector3Int(
            Random.Range(firstRoom.X + 1, firstRoom.X + firstRoom.Width - 1),
            Random.Range(firstRoom.Y + 1, firstRoom.Y + firstRoom.Height - 1),
            0
        );
    }

    // Log final spawn position
    Debug.Log($"Final Spawn Position: ({spawnPosition.x}, {spawnPosition.y})");

    // Convert the spawn position to Vector2 for spawning (for MapManager)
    Vector2 spawnPosition2D = MapManager.instance.FloorMap.CellToWorld(spawnPosition);
    MapManager.instance.CreateEntity("Player", spawnPosition2D);
}

  /// Return an L-shaped tunnel between these two points using Bresenham lines.

  private void TunnelBetween(RectangularRoom oldRoom, RectangularRoom newRoom) {
    Vector2Int oldRoomCenter = oldRoom.Center();
    Vector2Int newRoomCenter = newRoom.Center();
    Vector2Int tunnelCorner;

    if (Random.value < 0.5f) {
      //Move horizontally, then vertically.
      tunnelCorner = new Vector2Int(newRoomCenter.x, oldRoomCenter.y);
    } else {
      //Move vertically, then horizontally.
      tunnelCorner = new Vector2Int(oldRoomCenter.x, newRoomCenter.y);
    }

    //Generate the coordinates for this tunnel.
    List<Vector2Int> tunnelCoords = new List<Vector2Int>();
    BresenhamLine.Compute(oldRoomCenter, tunnelCorner, tunnelCoords);
    BresenhamLine.Compute(tunnelCorner, newRoomCenter, tunnelCoords);

    //Set the tiles for this tunnel.
    for (int i = 0; i < tunnelCoords.Count; i++) {
      SetFloorTile(new Vector3Int(tunnelCoords[i].x, tunnelCoords[i].y));

      //Set the wall tiles around this tile to be walls.
      for (int x = tunnelCoords[i].x - 1; x <= tunnelCoords[i].x + 1; x++) {
        for (int y = tunnelCoords[i].y - 1; y <= tunnelCoords[i].y + 1; y++) {
          if (SetWallTileIfEmpty(new Vector3Int(x, y))) {
            continue;
          }
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
  private void PlaceEntities(List<RectangularRoom> rooms, int maximumMonsters, int maximumItems) {
    foreach (var room in rooms) {
        int numberOfMonsters = Random.Range(0, maximumMonsters + 1);
        int numberOfItems = Random.Range(0, maximumItems + 1);

        // Place monsters
        for (int monster = 0; monster < numberOfMonsters;) {
            int x = Random.Range(room.X + 1, room.X + room.Width - 1); // Randomize position inside room bounds
            int y = Random.Range(room.Y + 1, room.Y + room.Height - 1);

            // Skip spawn if it's at the edge (walls) of the room
            if (x == room.X || x == room.X + room.Width - 1 || y == room.Y || y == room.Y + room.Height - 1) {
                continue;
            }

            // Check if there are any existing actors or items at this position
            bool overlap = false;
            foreach (var actor in GameManager.instance.Actors) {
                Vector3Int actorPos = MapManager.instance.FloorMap.WorldToCell(actor.transform.position);
                if (actorPos.x == x && actorPos.y == y) {
                    overlap = true;
                    break;
                }
            }

            foreach (var entity in GameManager.instance.Entities) {
                Vector3Int entityPos = MapManager.instance.FloorMap.WorldToCell(entity.transform.position);
                if (entityPos.x == x && entityPos.y == y) {
                    overlap = true;
                    break;
                }
            }

            // If there's no overlap, spawn a monster
            if (!overlap) {
                if (Random.value < 0.8f) {
                    MapManager.instance.CreateEntity("FireSprite", new Vector2(x, y));
                } else {
                    MapManager.instance.CreateEntity("FlyMob", new Vector2(x, y));
                }
                monster++;
            }
        }

        // Place items
        for (int item = 0; item < numberOfItems;) {
            int x = Random.Range(room.X + 1, room.X + room.Width - 1); // Randomize position inside room bounds
            int y = Random.Range(room.Y + 1, room.Y + room.Height - 1);

            // Skip spawn if it's at the edge (walls) of the room
            if (x == room.X || x == room.X + room.Width - 1 || y == room.Y || y == room.Y + room.Height - 1) {
                continue;
            }

            // Check if there are any existing actors or items at this position
            bool overlap = false;
            foreach (var actor in GameManager.instance.Actors) {
                Vector3Int actorPos = MapManager.instance.FloorMap.WorldToCell(actor.transform.position);
                if (actorPos.x == x && actorPos.y == y) {
                    overlap = true;
                    break;
                }
            }

            foreach (var entity in GameManager.instance.Entities) {
                Vector3Int entityPos = MapManager.instance.FloorMap.WorldToCell(entity.transform.position);
                if (entityPos.x == x && entityPos.y == y) {
                    overlap = true;
                    break;
                }
            }

            // If there's no overlap, spawn an item
            if (!overlap) {
                MapManager.instance.CreateEntity("Potion of Heart", new Vector2(x, y));
                item++;
            }
        }
    }
  }
}

