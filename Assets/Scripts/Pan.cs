
using UnityEngine;
using UnityEngine.InputSystem;

public class Pan : MonoBehaviour
{
    [SerializeField]
    private InventoryItem panItem;

    [SerializeField]
    private HotbarController hotbarController;

    private bool playerNearby;

    private static Pan instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        FindHotbarController();
    }

private void FindHotbarController()
{
    GameObject gameControllerObject =
        GameObject.Find("GameController");

    if (gameControllerObject == null)
    {
        Debug.LogError("GameObject GameController não encontrado!");
        return;
    }

    hotbarController =
        gameControllerObject.GetComponent<HotbarController>();

    if (hotbarController == null)
        Debug.LogError("HotbarController não encontrado!");
    else
        Debug.Log("HotbarController recuperado com sucesso!");
}

    private void Update()
    {
        if (!playerNearby)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            PickUp();
        }
    }

    private void PickUp()
    {
        if (panItem == null)
        {
            Debug.LogError(
                "Panela não possui InventoryItem configurado."
            );

            return;
        }

        // Recupera a referência caso tenha sido perdida.
        FindHotbarController();

        if (hotbarController == null)
        {
            Debug.LogError(
                "HotbarController não foi encontrado."
            );

            return;
        }

        bool added = hotbarController.AddItem(panItem);

        if (!added)
        {
            Debug.Log(
                "Não foi possível pegar a panela. Hotbar cheia."
            );

            return;
        }

        gameObject.SetActive(false);

        Debug.Log(
            "Panela adicionada à hotbar."
        );
    }

    public bool IsPan(InventoryItem item)
    {
        if (item == null || panItem == null)
            return false;

        if (item.itemPrefab == null ||
            panItem.itemPrefab == null)
            return false;

        return item.itemPrefab == panItem.itemPrefab;
    }

    public static bool WasUsedInRecipe(InventoryItem[] items)
    {
        if (instance == null || items == null)
            return false;

        foreach (InventoryItem item in items)
        {
            if (instance.IsPan(item))
                return true;
        }

        return false;
    }

    public static void ReturnToCounter()
    {
        if (instance == null)
            return;

        instance.gameObject.SetActive(true);

        Debug.Log(
            "Panela voltou para a bancada."
        );
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = false;
        }
    }
}
