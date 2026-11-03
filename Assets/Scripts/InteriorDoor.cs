
using UnityEngine;

public class InteriorDoor : MonoBehaviour
{
    [Header("Controlador de transição")]
    [SerializeField] private EnvironmentTransition transition;

    [Header("Configuração da entrada")]
    [SerializeField] private GameObject interior;
    [SerializeField] private Transform interiorSpawn;
    [SerializeField] private Transform exteriorExitSpawn;

    // Utilizado na porta externa.
    public void Enter()
    {
        if (transition == null)
        {
            Debug.LogError(
                "InteriorDoor: EnvironmentTransition não configurado."
            );
            return;
        }

        transition.EnterInterior(
            interior,
            interiorSpawn,
            exteriorExitSpawn
        );
    }

    // Utilizado na porta interna.
    public void Exit()
    {
        if (transition == null)
        {
            Debug.LogError(
                "InteriorDoor: EnvironmentTransition não configurado."
            );
            return;
        }

        transition.ExitInterior();
    }
}
