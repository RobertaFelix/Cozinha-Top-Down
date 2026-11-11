
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("NPCs disponíveis")]
    [SerializeField] private GameObject customerPrefabA;
    [SerializeField] private GameObject customerPrefabB;
    [SerializeField] private GameObject customerPrefabC;
    [SerializeField] private GameObject customerPrefabD;

    [Header("Fogão")]
    [SerializeField] private StoveController stoveController;

    [Header("Cardápio do restaurante")]
    [SerializeField] private RestaurantMenuController restaurantMenuController;

    [Header("Mesas disponíveis")]
    [SerializeField] private Table[] tables;

    private readonly List<GameObject> spawnedCustomers =
        new List<GameObject>();

    [Header("Funcionamento do restaurante")]
    [SerializeField, Min(0.1f)] private float spawnInterval = 8f;

    public bool IsRestaurantOpen { get; private set; }

    private float nextSpawnTime;

    private void Awake()
    {
        IsRestaurantOpen = false;
    }

    public void SetRestaurantOpen(bool isOpen)
    {
        if (IsRestaurantOpen == isOpen)
            return;

        IsRestaurantOpen = isOpen;
        // Ao abrir, o primeiro cliente pode chegar imediatamente.
        nextSpawnTime = Time.time;
    }

    private void Update()
    {
        if (!IsRestaurantOpen || Time.time < nextSpawnTime || !HasDishesInMenu())
            return;

        nextSpawnTime = Time.time + Mathf.Max(0.1f, spawnInterval);
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
        // Também protege chamadas diretas, além do spawn automático.
        if (!IsRestaurantOpen)
            return;

        CleanupSpawnedLists();

        if (!HasDishesInMenu())
        {
            Debug.Log(
                "Spawn aguardando: adicione pelo menos um prato ao cardápio."
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

        // Todos os prefabs disponíveis.
        GameObject[] allPrefabs =
        {
            customerPrefabA,
            customerPrefabB,
            customerPrefabC,
            customerPrefabD
        };

        // Verifica se todos os prefabs foram configurados.
        foreach (GameObject prefab in allPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogError(
                    "Configure os quatro prefabs de NPC no CustomerSpawner."
                );
                return;
            }
        }

        // Identifica as aparências que ainda não estão em uso.
        List<GameObject> availablePrefabs =
            new List<GameObject>();

        foreach (GameObject prefab in allPrefabs)
        {
            bool alreadySpawned = false;

            foreach (GameObject spawnedCustomer in spawnedCustomers)
            {
                if (spawnedCustomer != null &&
                    spawnedCustomer.GetComponent<CustomerAppearance>()
                    != null &&
                    spawnedCustomer.GetComponent<CustomerAppearance>().prefab
                    == prefab)
                {
                    alreadySpawned = true;
                    break;
                }
            }

            if (!alreadySpawned)
                availablePrefabs.Add(prefab);
        }

        if (availablePrefabs.Count == 0)
        {
            Debug.Log(
                "Os quatro tipos de NPC já estão presentes."
            );
            return;
        }

        // Sorteia uma aparência entre as disponíveis.
        GameObject selectedPrefab =
            availablePrefabs[
                Random.Range(0, availablePrefabs.Count)
            ];

        // Procura mesas com cadeiras livres.
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

        // Cria o NPC.
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

        // Registra a aparência utilizada.
        CustomerAppearance appearance =
            customer.AddComponent<CustomerAppearance>();

        appearance.prefab = selectedPrefab;

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

        // Registra o NPC criado.
        spawnedCustomers.Add(customer);

        Debug.Log(
            "NPC criado: " + selectedPrefab.name
        );
    }

    private void CleanupSpawnedLists()
    {
        spawnedCustomers.RemoveAll(
            spawnedCustomer => spawnedCustomer == null
        );
    }
}

// Identifica qual prefab deu origem ao NPC.
public class CustomerAppearance : MonoBehaviour
{
    public GameObject prefab;
}
