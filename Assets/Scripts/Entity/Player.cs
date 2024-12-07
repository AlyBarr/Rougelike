using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Actor))]
sealed class Player : MonoBehaviour, Controls.IPlayerActions {
  private Controls controls;

  [SerializeField] private bool moveKeyHeld; //read-only

  private void Awake() => controls = new Controls();

  private void OnEnable() {
    controls.Player.SetCallbacks(this);
    controls.Player.Enable();
  }

  private void OnDisable() {
    controls.Player.SetCallbacks(null);
    controls.Player.Disable();
  }

  void Controls.IPlayerActions.OnMovement(InputAction.CallbackContext context) {
    if (context.started)
      moveKeyHeld = true;
    else if (context.canceled)
      moveKeyHeld = false;
  }

  void Controls.IPlayerActions.OnExit(InputAction.CallbackContext context) {
    if (context.performed) {
      UIManager.instance.ToggleMenu();
    }
  }

  public void OnView(InputAction.CallbackContext context) {
    if (context.performed) {
      if (!UIManager.instance.IsMenuOpen || UIManager.instance.IsMessageHistoryOpen) {
        UIManager.instance.ToggleMessageHistory();
      }
    }
  }

  public void OnPickUp(InputAction.CallbackContext context) {
    if (context.performed) {
      Action.PickUpAction(GetComponent<Actor>());
    }
  }

  public void OnInventory(InputAction.CallbackContext context) {
    if (context.performed) {
      if (!UIManager.instance.IsMenuOpen || UIManager.instance.IsInventoryOpen) {
        if (GetComponent<Inventory>().Items.Count > 0) {
          UIManager.instance.ToggleInventory(GetComponent<Actor>());
        } else {
          UIManager.instance.AddMessage("You have no items.", "#808080");
        }
      }
    }
  }

  public void OnDrop(InputAction.CallbackContext context) {
    if (context.performed) {
      if (!UIManager.instance.IsMenuOpen || UIManager.instance.IsDropMenuOpen) {
        if (GetComponent<Inventory>().Items.Count > 0) {
          UIManager.instance.ToggleDropMenu(GetComponent<Actor>());
        } else {
          UIManager.instance.AddMessage("You have no items.", "#808080");
        }
      }
    }
  }

  private void FixedUpdate() {
    if (!UIManager.instance.IsMenuOpen) {
      if (GameManager.instance.IsPlayerTurn && moveKeyHeld && GetComponent<Actor>().IsAlive) {
        MovePlayer();
      }
    }
  }

  private float movementSpeed = 30f;
  


private void MovePlayer() {
    Vector2 direction = controls.Player.Movement.ReadValue<Vector2>();
    Vector2 roundedDirection = new Vector2(Mathf.Round(direction.x), Mathf.Round(direction.y));
    Vector3 moveStep = (Vector3)direction.normalized * movementSpeed * Time.deltaTime;
    
    Vector3 futurePosition = transform.position + moveStep;

    if (IsValidPosition(futurePosition)) {
        transform.position += moveStep;
        moveKeyHeld = Action.BumpAction(GetComponent<Actor>(), roundedDirection);
    }
}

  private bool IsValidPosition(Vector3 futurePosition) {
    Vector3Int gridPosition = MapManager.instance.FloorMap.WorldToCell(futurePosition);
    if (!MapManager.instance.InBounds(gridPosition.x, gridPosition.y) || MapManager.instance.ObstacleMap.HasTile(gridPosition) || futurePosition == transform.position) {
      return false;
    }
    return true;
  }
}