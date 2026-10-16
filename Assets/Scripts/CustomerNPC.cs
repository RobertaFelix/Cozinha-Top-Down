using System.Collections;
using UnityEngine;

public class CustomerNPC : MonoBehaviour
{
    private Table currentTable;
    private Transform currentChair;

    [Header("Prato servido")]
    [SerializeField] private Vector3 servedFoodOffset =
        new Vector3(0f, 0.12f, 0f);

    [SerializeField] private float servedFoodDuration = 15f;

    [SerializeField] private Vector3 servedFoodScale =
        Vector3.one;

    [Header("Visual do prato")]
    [SerializeField] private string foodSortingLayerName = "Default";
    [SerializeField] private int foodSortingOrder = 10;

    [Header("Moeda de recompensa")]
    [SerializeField] private GameObject coinPrefab;

    [SerializeField] private Vector3 coinOffset =
        new Vector3(0f, -0.12f, 0f);

    private GameObject servedFoodObject;
    private bool isLeaving;

    public void SitAt(Table table, Transform chair)
    {
        currentTable = table;
        currentChair = chair;

        isLeaving = false;

        transform.position =
            chair.position + new Vector3(0f, -0.124f, 0f);
    }

    public void ServeFood(Sprite foodSprite)
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

        Debug.Log(
            "Prato servido. O NPC irá embora em " +
            servedFoodDuration + " segundos."
        );

        StartCoroutine(LeaveAfterEating());
    }

    private IEnumerator LeaveAfterEating()
    {
        yield return new WaitForSeconds(servedFoodDuration);

        if (isLeaving)
            yield break;

        // Cria a moeda depois que o NPC termina de comer.
        SpawnCoin();

        // Libera a cadeira e remove o NPC.
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

        Vector3 spawnPosition =
            referencePosition + coinOffset;

        // Instancia o prefab com seu Animator e Controller.
        // A moeda não será filha do NPC.
        GameObject coin = Instantiate(
            coinPrefab,
            spawnPosition,
            Quaternion.identity
        );

        coin.name = "RewardCoin";

        Debug.Log("Moeda criada após o NPC terminar de comer.");
    }

    public void Leave()
    {
        if (isLeaving)
            return;

        isLeaving = true;

        // Libera a cadeira para outro NPC.
        if (currentTable != null && currentChair != null)
        {
            currentTable.FreeChair(currentChair);

            currentTable = null;
            currentChair = null;
        }

        // Remove o prato, caso ainda exista.
        if (servedFoodObject != null)
        {
            Destroy(servedFoodObject);
            servedFoodObject = null;
        }

        Debug.Log("NPC foi embora e liberou a cadeira.");

        Destroy(gameObject);
    }
}