using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class ResidentSchedule
{
    [Range(0, 23)] public int firstMeal, secondMeal;
    [Range(0, 1)] public float interest;
    public DishTemperature favorite;
    public ResidentSchedule(int lunch, int dinner, float chance, DishTemperature food)
    { firstMeal = lunch; secondMeal = dinner; interest = chance; favorite = food; }
}

public class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject customerPrefabA, customerPrefabB, customerPrefabC, customerPrefabD;
    [SerializeField] private StoveController stoveController;
    [SerializeField] private RestaurantMenuController restaurantMenuController;
    [SerializeField] private Table[] tables;
    [SerializeField, Min(.1f)] private float spawnInterval = 8f;
    [SerializeField, Range(.01f, 1f)] private float advertisedSpawnIntervalMultiplier = .75f;
    [Header("Village routes")]
    [SerializeField] private Transform[] villageStops;
    [SerializeField] private Transform restaurantOutside, restaurantInside;
    [SerializeField, Min(.1f)] private float walkSpeed = 2f;
    [SerializeField] private ResidentSchedule[] schedules = {
        new ResidentSchedule(8,12,.55f,DishTemperature.Fresh),
        new ResidentSchedule(12,18,.60f,DishTemperature.Warm),
        new ResidentSchedule(13,19,.50f,DishTemperature.Neutral),
        new ResidentSchedule(11,18,.55f,DishTemperature.Warm)
    };

    public float CurrentSpawnInterval => Mathf.Max(.1f, spawnInterval * (UpgradePurchases.Has(UpgradePurchases.Television)
        ? Mathf.Clamp(advertisedSpawnIntervalMultiplier,.01f,1f) : 1f));
    public bool IsRestaurantOpen { get; private set; }
    private GameClock clock;
    private RestaurantHistoryModule history;
    private GameObject[] prefabs;
    private readonly CustomerNPC[] residents = new CustomerNPC[4];
    private readonly bool[] busy = new bool[4];
    private readonly int[] lastRoute = { -1,-1,-1,-1 };
    private float nextCheck;

    private void Start()
    {
        prefabs = new[] { customerPrefabA, customerPrefabB, customerPrefabC, customerPrefabD };
        clock = FindAnyObjectByType<GameClock>();
    }

    public void SetRestaurantOpen(bool open) { IsRestaurantOpen = open; nextCheck = Time.time; }

    private void Update()
    {
        if (Time.timeScale <= 0 || Time.time < nextCheck || clock == null) return;
        nextCheck = Time.time + CurrentSpawnInterval;
        SpawnCustomer();
    }

    public void SpawnCustomer()
    {
        if (prefabs == null || villageStops == null || villageStops.Length == 0 || !restaurantInside || !restaurantOutside) return;
        if (!history && SaveGameService.Instance) history = SaveGameService.Instance.GetComponent<RestaurantHistoryModule>();
        if (!history) return;
        if (!restaurantMenuController) restaurantMenuController = RestaurantMenuController.Instance;
        int minutes = (int)clock.TotalGameMinutes;
        int day = minutes / 1440, hour = minutes / 60 % 24;
        for (int i = 0; i < Mathf.Min(prefabs.Length, schedules.Length); i++)
        {
            if (busy[i] || !prefabs[i]) continue;
            Transform home = villageStops[i % villageStops.Length];
            if (!residents[i])
            {
                residents[i] = Instantiate(prefabs[i], home.position, Quaternion.identity).GetComponent<CustomerNPC>();
                residents[i].SetSeated(false);
            }
            var profile = schedules[i];
            int weekendShift = day % 7 >= 5 ? 1 : 0;
            int window = hour >= profile.secondMeal + weekendShift ? 1 : 0;
            int start = (window == 1 ? profile.secondMeal : profile.firstMeal) + weekendShift;
            int offset = new System.Random(unchecked(day * 7919 + i * 397 + window)).Next(0, 35);
            int elapsed = minutes % 1440 - start * 60 - offset;
            if (IsRestaurantOpen && restaurantMenuController && restaurantMenuController.SelectedDishCount > 0 &&
                elapsed >= 0 && elapsed < 100 && history.TryWindow(day, i, window))
            {
                int capacity = 0;
                foreach (var t in tables) if (t) capacity += t.Capacity;
                float rating = history.Rating(restaurantMenuController.SelectedDishCount, capacity);
                float chance = profile.interest * (.75f + rating * .8f);
                if (UpgradePurchases.Has(UpgradePurchases.Television)) chance /= Mathf.Max(.01f, advertisedSpawnIntervalMultiplier);
                if (UnityEngine.Random.value <= chance)
                {
                    StartCoroutine(Visit(i));
                    continue;
                }
            }
            int period = minutes / 180;
            if (lastRoute[i] == period) continue;
            lastRoute[i] = period;
            int stop = hour >= 7 && hour < 21 ? (i + period) % villageStops.Length : i % villageStops.Length;
            StartCoroutine(Errand(i, villageStops[stop]));
        }
    }

    private IEnumerator Errand(int id, Transform destination)
    {
        busy[id] = true;
        yield return Walk(residents[id], destination.position, destination);
        busy[id] = false;
    }

    private IEnumerator Visit(int id)
    {
        busy[id] = true;
        var npc = residents[id];
        yield return Walk(npc, restaurantOutside.position, restaurantOutside);
        Table table = null;
        Transform seat = null;
        if (IsRestaurantOpen && Vector2.Distance(npc.transform.position, restaurantOutside.position) < .15f)
        {
            int first = UnityEngine.Random.Range(0, tables.Length);
            for (int i = 0; i < tables.Length && seat == null; i++)
            {
                table = tables[(i + first) % tables.Length];
                if (table) seat = table.GetRandomFreeChair();
            }
            history.RecordArrival(seat != null);
        }
        if (seat)
        {
            npc.transform.position = restaurantInside.position;
            Vector3 target = seat.position + Vector3.down * .25f;
            yield return Walk(npc, target, restaurantInside, seat);
            if (Vector2.Distance(npc.transform.position, target) < .15f)
            {
                var order = npc.GetComponent<CustomerOrderController>();
                order.Initialize(stoveController, schedules[id].favorite);
                if (order.RequestedRecipe != null)
                {
                    npc.SitAt(table, seat);
                    int visitDay = (int)clock.TotalGameMinutes / 1440;
                    while (npc && npc.IsSeated)
                    {
                        if ((int)clock.TotalGameMinutes / 1440 != visitDay && !order.OrderCompleted)
                        {
                            order.EndVisit();
                            break;
                        }
                        yield return null;
                    }
                }
                else table.FreeChair(seat);
            }
            else table.FreeChair(seat);
            yield return Walk(npc, restaurantInside.position, restaurantInside, seat);
            npc.transform.position = restaurantOutside.position;
        }
        yield return Walk(npc, villageStops[id % villageStops.Length].position, restaurantOutside);
        busy[id] = false;
    }

    private IEnumerator Walk(CustomerNPC npc, Vector3 target, Transform environment, Transform reservedSeat = null)
    {
        // Interiors are unloaded while the player is outside. Recheck the physical
        // route when the player enters midway through a simulated walk.
        bool visible = environment.gameObject.activeInHierarchy;
        var path = CustomerFootpath.Find(npc.transform.position, target, visible, reservedSeat);
        int leg = 0;
        while (path != null && leg < path.Count)
        {
            if (!visible && environment.gameObject.activeInHierarchy)
            {
                visible = true;
                path = CustomerFootpath.Find(npc.transform.position, target, true, reservedSeat);
                leg = 0;
                continue;
            }
            Vector3 point = path[leg];
            npc.transform.position = Vector3.MoveTowards(npc.transform.position, point, walkSpeed * Time.deltaTime);
            if ((npc.transform.position - point).sqrMagnitude < .0001f) leg++;
            yield return null;
        }
    }
}
