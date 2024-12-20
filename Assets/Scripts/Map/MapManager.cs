using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour {
  public static MapManager instance;

  [Header("Map Settings")]
  [SerializeField] private int width = 350;
  [SerializeField] private int height = 200;
  [SerializeField] private int roomMaxSize = 30;
  [SerializeField] private int roomMinSize = 8;
  [SerializeField] private int maxRooms = 10;
  [SerializeField] private int maxMonstersPerRoom = 4;
  [SerializeField] private int maxItemsPerRoom = 5;

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

  public void CreateEntity(string entity, Vector2 position) {
    GameObject prefab = Resources.Load<GameObject>($"Prefabs/{entity}");
    //if (prefab != null) {
       // Instantiate(prefab, position, Quaternion.identity);
    //} else {
    //    Debug.LogError($"Entity prefab '{entity}' not found in Resources/Prefabs");
    //}

    switch (entity) {
      case "Player":
        Instantiate(Resources.Load<GameObject>($"Prefabs/Player"), new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity).name = "$Prefabs/Player";
        break;
      case "FireSprite":
        Instantiate(Resources.Load<GameObject>($"Prefabs/FireSprite"), new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity).name = $"Prefabs/FireSprite";
        break;
      case "FlyingMob":
        Instantiate(Resources.Load<GameObject>($"Prefabs/FlyMob"), new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity).name = $"Prefabs/FlyMob";
        break;
      case "Potion of Heart":
        Instantiate(Resources.Load<GameObject>($"Prefabs/Potion of Heart"), new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity).name = $"Prefabs/Potion of Heart";
        break;
      case "Fireball Scroll":
        Instantiate(Resources.Load<GameObject>($"Prefabs/Fireball Scroll"), new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity).name = $"Prefabs/Fireball Scroll";
        break;
      case "Confusion Scroll":
        Instantiate(Resources.Load<GameObject>($"Prefabs/Confusion Scroll"), new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity).name = $"Prefabs/Confusion Scroll";
        break;
      case "Lightning Scroll":
        Instantiate(Resources.Load<GameObject>($"Prefabs/Lightning Scroll"), new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity).name = $"Prefabs/Lightning Scroll";
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

  public bool IsValidPosition(Vector3 futurePosition) {
    Vector3Int gridPosition = floorMap.WorldToCell(futurePosition);
    if (!InBounds(gridPosition.x, gridPosition.y) || obstacleMap.HasTile(gridPosition)) {
      return false;
    }
    return true;
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