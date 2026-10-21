
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("NPC")]
    [SerializeField] private GameObject customerPrefab;

    [Header("Fogão")]
    [SerializeField] private StoveController stoveController;

    [Header("Cardápio do restaurante")]
    [SerializeField] private RestaurantMenuController restaurantMenuController;

    [Header("Mesas disponíveis")]
    [SerializeField] private Table[] tables;

    private bool initialSpawnCompleted;

    private void Start()
    {
        StartCoroutine(WaitForMenuAndSpawn());
    }

    private IEnumerator WaitForMenuAndSpawn()
    {
        // Aguarda até existir um prato selecionado no cardápio.
        while (!HasDishesInMenu())
        {
            yield return null;
        }

        // Cria o primeiro NPC automaticamente.
        SpawnCustomer();
    }

    private bool HasDishesInMenu()
    {
        if (restaurantMenuController == null)
        {
            restaurantMenuController =
                RestaurantMenuController.Instance;
        }

        if (restaurantMenuController == null)
            return false;

        IReadOnlyList<CookingRecipe> recipes =
            restaurantMenuController.GetSelectedRecipes();

        return recipes != null && recipes.Count > 0;
    }

    public void SpawnCustomer()
    {
        if (initialSpawnCompleted)
            return;

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

        if (!HasDishesInMenu())
        {
            Debug.Log(
                "Spawn aguardando: adicione pelo menos um prato ao cardápio."
            );
            return;
        }

        List<Table> availableTables = new List<Table>();

        if (tables != null)
        {
            foreach (Table table in tables)
            {
                if (table != null && table.HasFreeChair())
                    availableTables.Add(table);
            }
        }

        if (availableTables.Count == 0)
        {
            Debug.Log(
                "Não há cadeiras disponíveis em nenhuma mesa."
            );
            return;
        }

        Table selectedTable =
            availableTables[Random.Range(0, availableTables.Count)];

        Transform chair = selectedTable.GetRandomFreeChair();

        if (chair == null)
        {
            Debug.LogWarning(
                "A mesa selecionada não possui cadeira livre."
            );
            return;
        }

        GameObject customer = Instantiate(customerPrefab);

        CustomerNPC customerNPC =
            customer.GetComponent<CustomerNPC>();

        CustomerOrderController orderController =
            customer.GetComponent<CustomerOrderController>();

        if (customerNPC == null || orderController == null)
        {
            Debug.LogError(
                "O prefab precisa conter CustomerNPC e CustomerOrderController."
            );

            Destroy(customer);
            return;
        }

        customerNPC.SitAt(selectedTable, chair);

        orderController.Initialize(stoveController);

        if (orderController.RequestedRecipe == null)
        {
            selectedTable.FreeChair(chair);
            Destroy(customer);

            Debug.LogWarning(
                "O NPC não encontrou um prato válido no cardápio."
            );
            return;
        }

        initialSpawnCompleted = true;

        Debug.Log(
            "NPC criado automaticamente porque existe um prato no cardápio."
        );
    }
}
