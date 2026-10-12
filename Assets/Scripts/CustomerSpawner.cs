using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("NPC")]
    [SerializeField] private GameObject customerPrefab;

    [Header("Fogão")]
    [SerializeField] private StoveController stoveController;

    [Header("Mesas disponíveis")]
    [SerializeField] private Table[] tables;

    private void Start()
    {
        SpawnCustomer();
    }

    public void SpawnCustomer()
    {
        if (customerPrefab == null)
        {
            Debug.LogError(
                "Customer Prefab não foi configurado."
            );
            return;
        }

        if (stoveController == null)
        {
            Debug.LogError(
                "StoveController não foi configurado no CustomerSpawner."
            );
            return;
        }

        List<Table> availableTables = new List<Table>();

        foreach (Table table in tables)
        {
            if (table == null)
                continue;

            if (table.HasFreeChair())
                availableTables.Add(table);
        }

        if (availableTables.Count == 0)
        {
            Debug.Log(
                "Não há cadeiras disponíveis em nenhuma mesa."
            );
            return;
        }

        int randomTableIndex =
            Random.Range(0, availableTables.Count);

        Table selectedTable =
            availableTables[randomTableIndex];

        Transform chair = selectedTable.GetRandomFreeChair();

        if (chair == null)
        {
            Debug.LogError(
                "A mesa selecionada não possui cadeira livre."
            );
            return;
        }

        GameObject customer = Instantiate(customerPrefab);

        CustomerNPC customerNPC =
            customer.GetComponent<CustomerNPC>();

        if (customerNPC == null)
        {
            Debug.LogError(
                "O NPC não possui CustomerNPC."
            );

            Destroy(customer);
            return;
        }

        customerNPC.SitAt(selectedTable, chair);

        CustomerOrderController orderController =
            customer.GetComponent<CustomerOrderController>();

        if (orderController == null)
        {
            Debug.LogError(
                "O NPC não possui CustomerOrderController."
            );

            Destroy(customer);
            return;
        }

        orderController.Initialize(stoveController);

        Debug.Log(
            "NPC sentou e recebeu um pedido aleatório."
        );
    }
}