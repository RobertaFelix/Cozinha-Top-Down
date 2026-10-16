
using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    private bool collected = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (CoinManager.Instance == null)
        {
            Debug.LogError(
                "CoinManager não encontrado na cena!"
            );
            return;
        }

        collected = true;

        CoinManager.Instance.AddCoins(1);

        Destroy(gameObject);
    }
}
