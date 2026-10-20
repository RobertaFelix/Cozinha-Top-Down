
using System.Collections;
using UnityEngine;

public class CustomerNPC : MonoBehaviour
{
    private Table currentTable;
    private Transform currentChair;

    [Header("Ícone de pedido disponível")]
    [SerializeField] private GameObject orderAvailableIcon;
    [SerializeField] private float iconFloatHeight = 0.04f;
    [SerializeField] private float iconFloatSpeed = 2f;

    private Vector3 iconInitialLocalPosition;

    [Header("Prato servido")]
    [SerializeField] private Vector3 servedFoodOffset =
        new Vector3(0f, 0.12f, 0f);
    [SerializeField] private float servedFoodDuration = 15f;
    [SerializeField] private Vector3 servedFoodScale = Vector3.one;

    [Header("Visual do prato")]
    [SerializeField] private string foodSortingLayerName = "Default";
    [SerializeField] private int foodSortingOrder = 10;

    [Header("Moeda de recompensa")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private Vector3 coinOffset =
        new Vector3(0f, -0.12f, 0f);

    private GameObject servedFoodObject;
    private bool isLeaving;
    private int rewardValue;

    private void Awake()
    {
        if (orderAvailableIcon != null)
        {
            iconInitialLocalPosition =
                orderAvailableIcon.transform.localPosition;

            orderAvailableIcon.SetActive(false);
        }
    }

    private void Update()
    {
        if (orderAvailableIcon == null ||
            !orderAvailableIcon.activeSelf)
        {
            return;
        }

        float offset =
            Mathf.Sin(Time.time * iconFloatSpeed) * iconFloatHeight;

        orderAvailableIcon.transform.localPosition =
            iconInitialLocalPosition + Vector3.up * offset;
    }

    public void SitAt(Table table, Transform chair)
    {
        currentTable = table;
        currentChair = chair;

        isLeaving = false;
        rewardValue = 0;

        transform.position =
            chair.position + new Vector3(0f, -0.124f, 0f);

        ShowOrderAvailableIcon();
    }

    public void ShowOrderAvailableIcon()
    {
        Debug.Log(
            "Tentando mostrar a exclamação. Referência: " +
            (orderAvailableIcon != null ? orderAvailableIcon.name : "NULA"),
            this
        );

        if (orderAvailableIcon == null || isLeaving)
            return;

        orderAvailableIcon.transform.localPosition =
            iconInitialLocalPosition;

        orderAvailableIcon.SetActive(true);

        Debug.Log(
            "Exclamação ativada: " + orderAvailableIcon.activeSelf,
            orderAvailableIcon
        );
    }


    public void HideOrderAvailableIcon()
    {
        if (orderAvailableIcon != null)
            orderAvailableIcon.SetActive(false);
    }

    public void ServeFood(Sprite foodSprite, int foodPrice)
    {
        if (isLeaving || servedFoodObject != null)
            return;

        if (foodSprite == null)
        {
            Debug.LogError(
                "CustomerNPC: o Sprite do prato é nulo.",
                this
            );
            return;
        }

        rewardValue = Mathf.Max(0, foodPrice);

        servedFoodObject = new GameObject("ServedFood");
        servedFoodObject.transform.SetParent(transform);
        servedFoodObject.transform.localPosition = servedFoodOffset;
        servedFoodObject.transform.localRotation = Quaternion.identity;
        servedFoodObject.transform.localScale = servedFoodScale;

        SpriteRenderer foodRenderer =
            servedFoodObject.AddComponent<SpriteRenderer>();

        foodRenderer.sprite = foodSprite;
        foodRenderer.sortingLayerName = foodSortingLayerName;
        foodRenderer.sortingOrder = foodSortingOrder;

        StartCoroutine(LeaveAfterEating());
    }

    private IEnumerator LeaveAfterEating()
    {
        yield return new WaitForSeconds(servedFoodDuration);

        if (isLeaving)
            yield break;

        SpawnCoin();
        Leave();
    }

    private void SpawnCoin()
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning(
                "CustomerNPC: configure o Coin Prefab no Inspector.",
                this
            );
            return;
        }

        Vector3 referencePosition =
            currentChair != null
                ? currentChair.position
                : transform.position;

        GameObject coin = Instantiate(
            coinPrefab,
            referencePosition + coinOffset,
            Quaternion.identity
        );

        coin.name = "RewardCoin";

        CoinPickup pickup = coin.GetComponent<CoinPickup>();

        if (pickup != null)
        {
            pickup.SetValue(rewardValue);
        }
        else
        {
            Debug.LogError(
                "O prefab da moeda não possui CoinPickup!",
                coin
            );
        }
    }

    public void Leave()
    {
        if (isLeaving)
            return;

        isLeaving = true;
        HideOrderAvailableIcon();

        if (currentTable != null && currentChair != null)
        {
            currentTable.FreeChair(currentChair);
            currentTable = null;
            currentChair = null;
        }

        if (servedFoodObject != null)
        {
            Destroy(servedFoodObject);
            servedFoodObject = null;
        }

        Destroy(gameObject);
    }
}
