using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour {
  public static MapManager instance;

  [Header("Map Settings")]
  [SerializeField] private int width = 75;
  [SerializeField] private int height = 40;
  [SerializeField] private int roomMaxSize = 10;
  [SerializeField] private int roomMinSize = 6;
  [SerializeField] private int maxRooms = 30;
  [SerializeField] private int maxMonstersPerRoom = 2;

  [SerializeField] private int maxItemsPerRoom = 2;

  [Header("Tiles")]
  [SerializeField] private TileBase floorTile;
  [SerializeField] private TileBase wallTile;
  [SerializeField] private TileBase fogTile;

  [Header("Tilemaps")]
  [SerializeField] private Tilemap floorMap;
  [SerializeField] private Tilemap obstacleMap;
  [SerializeField] private Tilemap fogMap;

  [Header("Features")]
  [SerializeField] private List<RectangularRoom> rooms = new List<RectangularRoom>();
  [SerializeField] private List<Vector3Int> visibleTiles = new List<Vector3Int>();
  private Dictionary<Vector3Int, TileData> tiles = new Dictionary<Vector3Int, TileData>();
  private Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();

  public int Width { get => width; }
  public int Height { get => height; }
  public TileBase FloorTile { get => floorTile; }
  public TileBase WallTile { get => wallTile; }
  public Tilemap FloorMap { get => floorMap; }
  public Tilemap ObstacleMap { get => obstacleMap; }
  public Tilemap FogMap { get => fogMap; }
  public Dictionary<Vector2Int, Node> Nodes { get => nodes; set => nodes = value; }

  private void Awake() {
    if (instance == null) {
      instance = this;
    } else {
      Destroy(gameObject);
    }
  }

  private void Start() {
    ProcGen procGen = new ProcGen();
    procGen.GenerateDungeon(width, height, roomMaxSize, roomMinSize, maxRooms, maxMonstersPerRoom, maxItemsPerRoom, rooms);

    AddTileMapToDictionary(floorMap);
    AddTileMapToDictionary(obstacleMap);

    SetupFogMap();

    Camera.main.transform.position = new Vector3(180, 95f, -10);
    Camera.main.orthographicSize = 90;
  }

  ///<summary>Return True if x and y are inside of the bounds of this map. </summary>
  public bool InBounds(int x, int y) => 0 <= x && x < width && 0 <= y && y < height;
  private Vector3Int FindSpawnPosition() {
    // Randomly select a room from the list of rooms
    RectangularRoom room = rooms[Random.Range(0, rooms.Count)];

    // Loop through the room's area to find a valid position
    for (int x = room.X; x < room.X + room.Width; x++) {
        for (int y = room.Y; y < room.Y + room.Height; y++) {
            Vector3Int tilePosition = new Vector3Int(x, y, 0);

            // Check if the tile is a floor tile and not occupied by any obstacles
            if (floorMap.HasTile(tilePosition) && !obstacleMap.HasTile(tilePosition)) {
                // Ensure that the position is within the overall map bounds before returning
                if (InBounds(x, y)) {
                    return tilePosition; // Return the valid spawn position within bounds
                }
            }
        }
    }

    // If no valid position is found, return a fallback position at the top-left corner of the room
    // or you can return (0,0) for fallback within map bounds
    Debug.LogWarning("No valid spawn position found in the selected room.");
    return new Vector3Int(room.X, room.Y, 0);  // Fallback to top-left of the room
  }


  public void CreateEntity(string entity, Vector2 position) {
    
        // Debug to ensure entity is being created
    Debug.Log($"Creating entity: {entity} at position {position}");

    switch (entity) {
      case "Player":
        Instantiate(Resources.Load<GameObject>("Player"), new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity).name = "Player";
        break;
      case "FireSprite":
        Instantiate(Resources.Load<GameObject>("FireSprite"), new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity).name = "FireSprite";
        break;
      case "FlyMob":
        Instantiate(Resources.Load<GameObject>("FlyMob"), new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity).name = "FlyMob";
        break;
      case "Potion of Heart":
        Instantiate(Resources.Load<GameObject>("Potion of Heart"), new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity).name = "Potion of Heart";
        break;
      default:
        Debug.LogError("Entity not found");
        break;
    }
  }

  public void UpdateFogMap(List<Vector3Int> playerFOV) {
    foreach (Vector3Int pos in visibleTiles) {
      if (!tiles[pos].IsExplored) {
        tiles[pos].IsExplored = true;
      }

      tiles[pos].IsVisible = false;
      fogMap.SetColor(pos, new Color(1.0f, 1.0f, 1.0f, 0.5f));
    }

    visibleTiles.Clear();

    foreach (Vector3Int pos in playerFOV) {
      tiles[pos].IsVisible = true;
      fogMap.SetColor(pos, Color.clear);
      visibleTiles.Add(pos);
    }
  }

  public void SetEntitiesVisibilities() {
    foreach (Entity entity in GameManager.instance.Entities) {
      if (entity.GetComponent<Player>()) {
        continue;
      }

      Vector3Int entityPosition = floorMap.WorldToCell(entity.transform.position);

      if (visibleTiles.Contains(entityPosition)) {
        entity.GetComponent<SpriteRenderer>().enabled = true;
      } else {
        entity.GetComponent<SpriteRenderer>().enabled = false;
      }
    }
  }

  private void AddTileMapToDictionary(Tilemap tilemap) {
    foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin) {
      if (!tilemap.HasTile(pos)) {
        continue;
      }

      TileData tile = new TileData();
      tiles.Add(pos, tile);
    }
  }

  private void SetupFogMap() {
    foreach (Vector3Int pos in tiles.Keys) {
      fogMap.SetTile(pos, fogTile);
      fogMap.SetTileFlags(pos, TileFlags.None);
    }
  }
}