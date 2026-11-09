
using UnityEngine;
using UnityEngine.InputSystem;

public class Pot : MonoBehaviour
{
    [SerializeField]
    private InventoryItem potItem;

    [SerializeField]
    private HotbarController hotbarController;

    private bool playerNearby;

    private static Pot instance;

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
        if (potItem == null)
        {
            Debug.LogError(
                "Frigideira não possui InventoryItem configurado."
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

        bool added = hotbarController.AddItem(potItem);

        if (!added)
        {
            Debug.Log(
                "Não foi possível pegar a Frigideira. Hotbar cheia."
            );

            return;
        }

        gameObject.SetActive(false);

        Debug.Log(
            "Frigideira adicionada à hotbar."
        );
    }

    public bool IsPot(InventoryItem item)
    {
        if (item == null || potItem == null)
            return false;

        if (item.itemPrefab == null ||
            potItem.itemPrefab == null)
            return false;

        return item.itemPrefab == potItem.itemPrefab;
    }

    public static bool WasUsedInRecipe(InventoryItem[] items)
    {
        if (instance == null || items == null)
            return false;

        foreach (InventoryItem item in items)
        {
            if (instance.IsPot(item))
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
            "Frigideira voltou para a bancada."
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
