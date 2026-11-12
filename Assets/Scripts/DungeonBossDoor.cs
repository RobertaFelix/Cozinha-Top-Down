using UnityEngine;
using Unity.Cinemachine;

public sealed class DungeonBossDoor : MonoBehaviour
{
    [SerializeField] private bool requiresKey = true;
    [SerializeField] private GameObject keyPrefab;
    [SerializeField] private Transform destination;
    [SerializeField] private Transform returnSpawn;
    [SerializeField] private DungeonBossDoor returnDoor;
    public bool IsUnlocked { get; private set; }
    public Transform Destination => destination;

    public bool TryEnter(GameObject player)
    {
        // Validate the destination before consuming anything.
        if (player == null || destination == null || !destination.gameObject.activeInHierarchy ||
            destination.gameObject.scene != gameObject.scene ||
            (returnDoor != null && (returnSpawn == null || !returnSpawn.gameObject.activeInHierarchy))) return false;
        if (requiresKey && !IsUnlocked)
        {
            if (keyPrefab == null || !BossKeyItem.TryConsume(keyPrefab)) return false;
            IsUnlocked = true;
        }
        InventoryStackCursor.Cancel();
        ItemDragHandler.CancelAllDrags();
        if (returnDoor != null) returnDoor.destination = returnSpawn;
        var previous = player.transform.position;
        var position = destination.position;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) { body.position = position; body.linearVelocity = Vector2.zero; }
        player.transform.position = position;
        Physics2D.SyncTransforms();
        var camera = Object.FindAnyObjectByType<CinemachineCamera>();
        if (camera != null)
        {
            camera.OnTargetObjectWarped(player.transform, position - previous);
            camera.PreviousStateIsValid = false;
        }
        return true;
    }

    public void Interact()
    {
        if (TryEnter(GameObject.FindGameObjectWithTag("Player"))) return;
        MarketHoursNotice.Show(requiresKey && !IsUnlocked && !BossKeyItem.Has(keyPrefab)
            ? "You need a boss room key." : "The passage is unavailable.");
    }
}
