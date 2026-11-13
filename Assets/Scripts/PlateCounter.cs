using UnityEngine;
using UnityEngine.InputSystem;

public class PlateCounter : MonoBehaviour
{
    [SerializeField] private GameObject plate;
    private bool nearby;

    private void Awake()
    {
        var area = gameObject.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(1.1f, 1.6f);
        area.offset = Vector2.down * .25f;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (nearby || !other.CompareTag("Player")) return;
        nearby = true;
        InteractionPromptManager.Instance?.Show(transform);
    }

    private void Update()
    {
        if (nearby && Time.timeScale > 0 && !InventoryPanelSession.HasOpenPanel &&
            CookwareCounter.InteractPressed)
        {
            var hotbar = FindAnyObjectByType<HotbarController>();
            if (hotbar) hotbar.AddItem(new InventoryItem { itemPrefab = plate, amount = 1 });
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) HidePrompt();
    }

    private void OnDisable() => HidePrompt();

    private void HidePrompt()
    {
        nearby = false;
        InteractionPromptManager.Instance?.Hide(transform);
    }
}
