
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class EnvironmentTransition : MonoBehaviour
{
    [Header("Ambiente externo")]
    [SerializeField] private GameObject exterior;

    [Header("Todos os interiores")]
    [SerializeField] private GameObject[] interiors;

    [Header("Jogador")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private Behaviour playerMovement;

    [Header("Transição")]
    [SerializeField] private Image transitionImage;
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float cameraSettleDelay = 0.6f;

    [Header("Retorno para casa")]
    [SerializeField] private GameObject homeInterior;
    [SerializeField] private Transform homeSpawn;
    [SerializeField] private Transform homeExteriorExitSpawn;

    public bool IsTransitioning => isTransitioning;
    private bool isTransitioning;

    public string CurrentInteriorId => currentInterior != null ? currentInterior.name : "";

    public bool CanRestoreCheckpoint(string interiorId)
    {
        if (string.IsNullOrEmpty(interiorId)) return exterior != null;
        foreach (var interior in interiors)
            if (interior != null && interior.name == interiorId) return true;
        return false;
    }

    public bool RestoreHomeCheckpoint(Vector3 position)
    {
        return homeInterior != null && RestorePlayerCheckpoint(homeInterior.name, position);
    }

    public bool RestorePlayerCheckpoint(string interiorId, Vector3 position)
    {
        if (exterior == null || isTransitioning || !CanRestoreCheckpoint(interiorId)) return false;
        FindPlayerReferences();
        if (player == null) return false;
        GameObject selected = null;
        foreach (var interior in interiors)
            if (interior != null && interior.name == interiorId) selected = interior;
        Transform exit = null;
        if (selected == homeInterior) exit = homeExteriorExitSpawn;
        else if (selected != null)
            foreach (var door in FindObjectsByType<InteriorDoor>(FindObjectsInactive.Include))
                if (door.Interior == selected && door.ExteriorExitSpawn != null)
                {
                    exit = door.ExteriorExitSpawn;
                    break;
                }
        if (selected != null && exit == null) return false;
        foreach (var interior in interiors)
            if (interior != null) interior.SetActive(interior == selected);
        exterior.SetActive(selected == null);
        currentInterior = selected;
        currentExteriorExitSpawn = exit;
        Vector3 previous = player.position;
        MovePlayer(position);
        var camera = FindAnyObjectByType<CinemachineCamera>();
        if (camera != null)
        {
            camera.OnTargetObjectWarped(player, position - previous);
            camera.PreviousStateIsValid = false;
        }
        Physics2D.SyncTransforms();
        return true;
    }

    public bool TryReturnHome()
    {
        if (!isActiveAndEnabled || isTransitioning || currentInterior == homeInterior ||
            homeInterior == null || homeSpawn == null || homeExteriorExitSpawn == null ||
            exterior == null || transitionImage == null) return false;
        FindPlayerReferences();
        if (player == null) return false;
        StartCoroutine(Transition(homeInterior, homeSpawn.position, homeExteriorExitSpawn, true));
        return true;
    }

    private GameObject currentInterior;
    private Transform currentExteriorExitSpawn;

    private void Start()
    {
        FindPlayerReferences();

        if (exterior != null)
            exterior.SetActive(true);

        foreach (GameObject interior in interiors)
        {
            if (interior != null)
                interior.SetActive(false);
        }

        if (transitionImage != null)
        {
            Color color = transitionImage.color;
            color.a = 0f;
            transitionImage.color = color;
        }
    }

    private void FindPlayerReferences()
    {
        // Procura o Player pela tag.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogError("EnvironmentTransition: Player não encontrado!");
            return;
        }

        player = playerObject.transform;

        playerRb = playerObject.GetComponent<Rigidbody2D>();

        playerMovement = playerObject.GetComponent<PlayerController>();

        Debug.Log("Referências do Player recuperadas!");

        Debug.Log("Transform: " + (player != null));
        Debug.Log("Rigidbody2D: " + (playerRb != null));
        Debug.Log("PlayerMovement: " + (playerMovement != null));
    }

    // CHAMADO PELA PORTA EXTERNA
    public void EnterInterior(
        GameObject interior,
        Transform interiorSpawn,
        Transform exteriorExitSpawn)
    {
        FindPlayerReferences();
        
        if (isTransitioning)
            return;

        if (currentInterior != null)
        {
            Debug.LogWarning("Já existe um interior ativo.");
            return;
        }

        if (interior == null ||
            interiorSpawn == null ||
            exteriorExitSpawn == null ||
            exterior == null ||
            player == null ||
            transitionImage == null)
        {
            Debug.LogError(
                "EnvironmentTransition: verifique as referências de entrada."
            );
            return;
        }

        StartCoroutine(Transition(
            interior,
            interiorSpawn.position,
            exteriorExitSpawn,
            true
        ));
    }

    // CHAMADO PELA PORTA INTERNA
    public void ExitInterior()
    {
        if (isTransitioning)
            return;

        if (currentInterior == null ||
            currentExteriorExitSpawn == null)
        {
            Debug.LogWarning(
                "EnvironmentTransition: não existe interior para sair."
            );
            return;
        }

        StartCoroutine(Transition(
            currentInterior,
            currentExteriorExitSpawn.position,
            null,
            false
        ));
    }

    private IEnumerator Transition(
        GameObject interior,
        Vector3 destination,
        Transform exteriorExitSpawn,
        bool entering)
    {
        isTransitioning = true;

        // Esconde o aviso de interação.
        if (InteractionPromptManager.Instance != null)
            InteractionPromptManager.Instance.Hide();

        // Bloqueia o movimento do jogador.
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        // Escurece a tela.
        yield return StartCoroutine(Fade(0f, 1f));

        // Troca os ambientes.
        if (entering)
        {
            if (currentInterior != null && currentInterior != interior)
                currentInterior.SetActive(false);
            exterior.SetActive(false);
            interior.SetActive(true);

            currentInterior = interior;
            currentExteriorExitSpawn = exteriorExitSpawn;
        }
        else
        {
            interior.SetActive(false);
            exterior.SetActive(true);

            currentInterior = null;
            currentExteriorExitSpawn = null;
        }

        // Teletransporta o jogador.
        MovePlayer(destination);

        Physics2D.SyncTransforms();

        // Aguarda a câmera estabilizar.
        yield return new WaitForSecondsRealtime(cameraSettleDelay);

        // Clareia a tela.
        yield return StartCoroutine(Fade(1f, 0f));

        // Libera o jogador.
        if (playerMovement != null)
            playerMovement.enabled = true;

        isTransitioning = false;

        Debug.Log(
            entering
                ? "Entrou em: " + interior.name
                : "Saiu do interior."
        );
    }

    private IEnumerator Fade(float startAlpha, float targetAlpha)
    {
        float elapsed = 0f;

        Color color = transitionImage.color;
        color.a = startAlpha;
        transitionImage.color = color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            color.a = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                smoothProgress
            );

            transitionImage.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        transitionImage.color = color;
    }

    private void MovePlayer(Vector3 destination)
    {
        if (playerRb != null)
        {
            playerRb.position = new Vector2(
                destination.x,
                destination.y
            );

            player.position = destination;
            playerRb.linearVelocity = Vector2.zero;
        }
        else
        {
            player.position = destination;
        }
    }
}
