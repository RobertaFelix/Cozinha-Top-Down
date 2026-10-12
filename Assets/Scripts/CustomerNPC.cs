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

    private GameObject servedFoodObject;
    private bool isLeaving;

    public void SitAt(Table table, Transform chair)
    {
        currentTable = table;
        currentChair = chair;

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

        // Cria a representação visual da comida no mundo 2D.
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

        Leave();
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
