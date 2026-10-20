
using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Dinheiro")]
    [SerializeField] private int totalCoins = 0;

    [Header("Interface")]
    [SerializeField] private TMP_Text coinText;

    public int TotalCoins => totalCoins;

    [Header("Multiplicador de ganhos")]
    [SerializeField, Min(1)]
    private int rewardMultiplier = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        UpdateUI();
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        totalCoins += amount * rewardMultiplier;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (coinText != null)
        {
            coinText.text = totalCoins.ToString("D9");
        }
    }

    public void SetRewardMultiplier(int multiplier)
    {
        rewardMultiplier = Mathf.Max(1, multiplier);
    }
}
