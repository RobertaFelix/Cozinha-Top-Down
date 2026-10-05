using UnityEngine;

public class CustomerNPC : MonoBehaviour
{
    private Table currentTable;
    private Transform currentChair;

    public void SitAt(
        Table table,
        Transform chair)
    {
        currentTable = table;
        currentChair = chair;

        transform.position =
        chair.position + new Vector3(0f, -0.124f, 0f);
    }

    public void Leave()
    {
        if (currentTable != null)
        {
            currentTable.FreeChair(
                currentChair
            );
        }

        Destroy(gameObject);
    }
}