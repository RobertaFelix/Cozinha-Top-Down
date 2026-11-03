
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    private bool isTransitioning;

    private GameObject currentInterior;
    private Transform currentExteriorExitSpawn;

    private void Start()
    {
        if (exterior != null)
            exterior.SetActive(true);

        // Todos os interiores começam desativados.
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

    // CHAMADO PELA PORTA EXTERNA
    public void EnterInterior(
        GameObject interior,
        Transform interiorSpawn,
        Transform exteriorExitSpawn)
    {
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
