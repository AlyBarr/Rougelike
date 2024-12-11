using System;
using System.Collections.Generic;
using UnityEngine;

static public class Action {

    // Wait for the player's turn to end
    static public void WaitAction() {
        GameManager.instance.EndTurn();
    }

    // Perform a bump action: either attack the target or move in the given direction
    static public bool BumpAction(Actor actor, Vector2 direction) {
        Vector3 targetPosition = actor.transform.position + (Vector3)direction;
        Actor target = GameManager.instance.GetActorAtLocation(targetPosition);

        if (target) {
            MeleeAction(actor, target);
            return false; // Stop movement if we hit a target.
        } else {
            MovementAction(actor, direction);
            return true; // Continue movement if no target is found.
        }
    }

    // Handle actor movement action
    static public void MovementAction(Actor actor, Vector2 direction) {
        actor.Move(direction);
        actor.UpdateFieldOfView(); // Update the actor's vision after moving
        GameManager.instance.EndTurn();
    }

    // Perform melee attack on the target
    static public void MeleeAction(Actor actor, Actor target) {
        if (actor == null || target == null) {
            Debug.LogWarning("Actor or target is null in MeleeAction");
            return;
        }

        int damage = actor.GetComponent<Fighter>().Power - target.GetComponent<Fighter>().Defense;
        string attackDesc = $"{actor.name} attacks {target.name}";

        // Determine attack message color based on actor type (player vs. non-player)
        string colorHex = actor.GetComponent<Player>() ? "#ffffff" : "#d1a3a4"; // White for player, light red for non-player

        if (damage > 0) {
            UIManager.instance.AddMessage($"{attackDesc} for {damage} hit points.", colorHex);
            target.GetComponent<Fighter>().Hp -= damage;
        } else {
            UIManager.instance.AddMessage($"{attackDesc} but does no damage.", colorHex);
        }

        GameManager.instance.EndTurn();
    }

    // Pick up an item and add it to the actor's inventory
    static public void PickUpAction(Actor actor) {
        if (actor == null) {
            Debug.LogWarning("Actor is null in PickUpAction");
            return;
        }

        for (int i = 0; i < GameManager.instance.Entities.Count; i++) {
            var entity = GameManager.instance.Entities[i];
            // Skip if the entity is already an actor or is not at the correct position
            if (entity.GetComponent<Actor>() || actor.transform.position != entity.transform.position) {
                continue;
            }

            if (actor.Inventory.Items.Count >= actor.Inventory.Capacity) {
                UIManager.instance.AddMessage("Your inventory is full.", "#808080");
                return; // Stop if inventory is full
            }

            Item item = entity.GetComponent<Item>();
            if (item != null) {
                item.transform.SetParent(actor.transform);
                actor.Inventory.Items.Add(item);
                UIManager.instance.AddMessage($"You picked up the {item.name}!", "#FFFFFF");

                GameManager.instance.RemoveEntity(item);
                GameManager.instance.EndTurn();
            }
        }
    }

    // Drop an item from the actor's inventory
    static public void DropAction(Actor actor, Item item) {
        if (actor == null || item == null) {
            Debug.LogWarning("Actor or item is null in DropAction");
            return;
        }

        actor.Inventory.Drop(item);
        UIManager.instance.ToggleDropMenu();
        GameManager.instance.EndTurn();
    }

    // Use a consumable item
    static public void UseAction(Actor consumer, Item item) {
        if (consumer == null || item == null) {
            Debug.LogWarning("Consumer or item is null in UseAction");
            return;
        }

        bool itemUsed = false;

        if (item.GetComponent<Consumable>()) {
            itemUsed = item.GetComponent<Consumable>().Activate(consumer);
        }

        UIManager.instance.ToggleInventory();

        if (itemUsed) {
            GameManager.instance.EndTurn();
        }
    }

    // Cast a consumable spell or ability on a target
    static public void CastAction(Actor consumer, Actor target, Consumable consumable) {
        if (consumer == null || target == null || consumable == null) {
            Debug.LogWarning("Consumer, target, or consumable is null in CastAction");
            return;
        }

        bool castSuccess = consumable.Cast(consumer, target);

        if (castSuccess) {
            GameManager.instance.EndTurn();
        }
    }

    // Cast a consumable spell or ability on multiple targets
    static public void CastAction(Actor consumer, List<Actor> targets, Consumable consumable) {
        if (consumer == null || targets == null || consumable == null) {
            Debug.LogWarning("Consumer, targets, or consumable is null in CastAction (multiple targets)");
            return;
        }

        bool castSuccess = consumable.Cast(consumer, targets);

        if (castSuccess) {
            GameManager.instance.EndTurn();
        }
    }
}
