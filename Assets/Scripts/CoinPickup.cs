using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [SerializeField, Min(0)]
    private int coinValue = 1;

    [Header("Upgrade de coleta")]
    [SerializeField, Min(0f)] private float upgradedCollectionRadius = 1f;
    private Transform player;

    private bool collected = false;

    private void Update()
    {
        if (collected || !UpgradePurchases.Has(UpgradePurchases.Magnet))
            return;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null && (player.position - transform.position).sqrMagnitude <=
            upgradedCollectionRadius * upgradedCollectionRadius)
            Collect();
    }

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

        Collect();
    }

    private void Collect()
    {
        if (collected)
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
