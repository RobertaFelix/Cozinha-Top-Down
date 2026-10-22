using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("NPCs disponíveis")]
    [SerializeField] private GameObject customerPrefabA;
    [SerializeField] private GameObject customerPrefabB;

    [Header("Fogão")]
    [SerializeField] private StoveController stoveController;

    [Header("Cardápio do restaurante")]
    [SerializeField] private RestaurantMenuController restaurantMenuController;

    [Header("Mesas disponíveis")]
    [SerializeField] private Table[] tables;

    private readonly List<GameObject> spawnedA =
        new List<GameObject>();

    private readonly List<GameObject> spawnedB =
        new List<GameObject>();

    private bool initialSpawnCompleted;

    private void Start()
    {
        StartCoroutine(WaitForMenuAndSpawn());
    }

    private IEnumerator WaitForMenuAndSpawn()
    {
        // Aguarda até existir pelo menos um prato no cardápio.
        while (!HasDishesInMenu())
        {
            yield return null;
        }

        // Cria dois NPCs com aparências diferentes.
        SpawnCustomer();
        SpawnCustomer();

        initialSpawnCompleted = true;
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
        CleanupSpawnedLists();

        if (customerPrefabA == null || customerPrefabB == null)
        {
            Debug.LogError(
                "Configure os dois prefabs de NPC no CustomerSpawner."
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

        bool aIsActive = spawnedA.Count > 0;
        bool bIsActive = spawnedB.Count > 0;

        if (aIsActive && bIsActive)
        {
            Debug.Log(
                "Os dois tipos de NPC já estão presentes."
            );
            return;
        }

        GameObject selectedPrefab;

        // Se os dois tipos estão disponíveis, sorteia um.
        if (!aIsActive && !bIsActive)
        {
            selectedPrefab =
                Random.value < 0.5f
                    ? customerPrefabA
                    : customerPrefabB;
        }
        else if (!aIsActive)
        {
            // O tipo A está ausente.
            selectedPrefab = customerPrefabA;
        }
        else
        {
            // O tipo B está ausente.
            selectedPrefab = customerPrefabB;
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
            availableTables[
                Random.Range(0, availableTables.Count)
            ];

        Transform chair = selectedTable.GetRandomFreeChair();

        if (chair == null)
        {
            Debug.LogWarning(
                "A mesa selecionada não possui cadeira livre."
            );
            return;
        }

        GameObject customer = Instantiate(selectedPrefab);

        CustomerNPC customerNPC =
            customer.GetComponent<CustomerNPC>();

        CustomerOrderController orderController =
            customer.GetComponent<CustomerOrderController>();

        if (customerNPC == null || orderController == null)
        {
            Debug.LogError(
                "O prefab precisa conter CustomerNPC e CustomerOrderController.",
                customer
            );

            selectedTable.FreeChair(chair);
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
                "O NPC não encontrou um prato válido para pedir."
            );
            return;
        }

        // Registra qual aparência está ocupando uma cadeira.
        if (selectedPrefab == customerPrefabA)
            spawnedA.Add(customer);
        else
            spawnedB.Add(customer);

        Debug.Log(
            "NPC criado: " + selectedPrefab.name
        );
    }

    private void CleanupSpawnedLists()
    {
        spawnedA.RemoveAll(customer => customer == null);
        spawnedB.RemoveAll(customer => customer == null);
    }
}