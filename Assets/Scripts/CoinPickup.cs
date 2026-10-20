using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [SerializeField, Min(0)]
    private int coinValue = 1;

    private bool collected = false;

    public void SetValue(int value)
    {
        coinValue = Mathf.Max(0, value);
    }

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

        CoinManager.Instance.AddCoins(coinValue);

        Destroy(gameObject);
    }
}
