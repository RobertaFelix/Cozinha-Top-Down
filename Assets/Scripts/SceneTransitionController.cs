
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class SceneTransitionController : MonoBehaviour
{
    [Header("Jogador")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D playerRb;

    [Header("Movimento do jogador")]
    [SerializeField] private Behaviour playerMovement;

    [Header("Configuração da Dungeon")]
    [SerializeField] private string dungeonSceneName = "Dungeon";
    [SerializeField] private string dungeonSpawnTag = "DungeonSpawn";

    [Header("Configuração da primeira cena")]
    [SerializeField] private string firstSceneName = "SampleScene";
    [SerializeField] private string firstSceneSpawnTag = "VillageSpawn";

    private bool returnHomeAfterLoad;
    private bool isLoading;
    public bool IsLoading => isLoading;

    public bool TryReturnHome()
    {
        if (isLoading || !Application.CanStreamedLevelBeLoaded(firstSceneName)) return false;
        returnHomeAfterLoad = true;
        LoadScene(firstSceneName, firstSceneSpawnTag);
        return true;
    }

    private IEnumerator FinishReturnHome()
    {
        // Scene Start initializes the exterior and interior state first.
        yield return null;
        var environment = FindFirstObjectByType<EnvironmentTransition>();
        if (environment != null) environment.TryReturnHome();
    }
    private string pendingSceneName;
    private string pendingSpawnTag;
    private bool pendingRandomDungeonSpawn;
    public string DungeonSceneName => dungeonSceneName;

    public void LoadDungeon()
    {
        if (isLoading) return;
        pendingRandomDungeonSpawn = true;
        LoadScene(dungeonSceneName, dungeonSpawnTag);
    }

    public void ReturnToFirstScene()
    {
        LoadScene(firstSceneName, firstSceneSpawnTag);
    }

    private void LoadScene(string sceneName, string spawnTag)
    {
        if (isLoading)
            return;

        SaveGameService.Instance?.CaptureSceneState();
        isLoading = true;

        pendingSceneName = sceneName;
        pendingSpawnTag = spawnTag;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        SceneManager.sceneLoaded += OnSceneLoaded;

        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != pendingSceneName)
            return;

        GameObject gameControllerObject =
            GameObject.Find("GameController");

        if (gameControllerObject != null)
        {
            InventoryController inventory =
                gameControllerObject.GetComponent<InventoryController>();

            HotbarController hotbar =
                gameControllerObject.GetComponent<HotbarController>();

            if (inventory != null)
                inventory.CleanEmptySlots();

            if (hotbar != null)
                hotbar.CleanEmptySlots();
        }
        else
        {
            Debug.LogError("[INVENTÁRIO] GameController não encontrado!");
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameObject spawnObject = null;
        if (pendingRandomDungeonSpawn)
        {
            pendingRandomDungeonSpawn = false;
            if (DungeonLayout.TrySelectSpawn(scene, out Transform dungeonSpawn))
                spawnObject = dungeonSpawn.gameObject;
            else
                Debug.LogWarning("No configured dungeon spawn points; using the legacy spawn.");
        }
        if (spawnObject == null)
            spawnObject = GameObject.FindGameObjectWithTag(pendingSpawnTag);

        if (spawnObject == null)
        {
            Debug.LogError(
                "Não foi encontrado um objeto com a tag: " +
                pendingSpawnTag
            );

            EnablePlayerMovement();
            isLoading = false;
            return;
        }

        if (player == null)
        {
            Debug.LogError(
                "Player não foi configurado no SceneTransitionController."
            );

            EnablePlayerMovement();
            isLoading = false;
            return;
        }

        Vector3 destination = spawnObject.transform.position;

        if (playerRb != null)
        {
            playerRb.position = new Vector2(
                destination.x,
                destination.y
            );

            playerRb.linearVelocity = Vector2.zero;
        }

        player.position = destination;

        Physics2D.SyncTransforms();

        CinemachineCamera cinemachineCamera =
            FindAnyObjectByType<CinemachineCamera>();

        if (cinemachineCamera != null)
        {
            cinemachineCamera.Target.TrackingTarget = player;

            Debug.Log(
                "Câmera configurada para seguir o Player."
            );
        }
        else
        {
            Debug.LogError(
                "Nenhuma Cinemachine Camera foi encontrada na cena."
            );
        }

        EnablePlayerMovement();

        isLoading = false;
        if (returnHomeAfterLoad)
        {
            returnHomeAfterLoad = false;
            StartCoroutine(FinishReturnHome());
        }

        Debug.Log(
            "Cena carregada: " + scene.name +
            ". Player reposicionado."
        );
    }

    private void EnablePlayerMovement()
    {
        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
