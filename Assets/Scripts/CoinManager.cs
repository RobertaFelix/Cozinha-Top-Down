
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
    public int RewardMultiplier => rewardMultiplier;
    public float PendingCoinFraction { get; private set; }

    public void RestoreEconomy(int coins, int multiplier, bool doubleDishPrices, float pendingCoinFraction = 0f)
    {
        totalCoins = Mathf.Max(0, coins);
        PendingCoinFraction = pendingCoinFraction >= 0f && pendingCoinFraction < 1f ? pendingCoinFraction : 0f;
        rewardMultiplier = Mathf.Max(1, multiplier);
        AreDishPricesDoubled = doubleDishPrices;
        UpdateUI();
    }

    public bool AreDishPricesDoubled { get; private set; }

    public void EnableDoubleDishPrices()
    {
        // O upgrade é único: aplicar novamente mantém o bônus em 2x.
        AreDishPricesDoubled = true;
    }

    public int GetDishSalePrice(int basePrice)
    {
        return Mathf.Max(0, basePrice) * (AreDishPricesDoubled ? 2 : 1);
    }

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

        float potionMultiplier = TimedPotionEffects.Instance != null
            ? TimedPotionEffects.Instance.GetMultiplier(TimedPotionKind.CoinGain) : 1f;
        // Carry fractional coins across rewards so two 1-coin pickups at 1.5x yield exactly 3.
        decimal reward = (decimal)amount * rewardMultiplier * (decimal)potionMultiplier + (decimal)PendingCoinFraction;
        decimal whole = System.Math.Floor(reward);
        PendingCoinFraction = (float)(reward - whole);
        totalCoins = (int)System.Math.Min(int.MaxValue, totalCoins + whole);
        if (totalCoins == int.MaxValue) PendingCoinFraction = 0f;
        UpdateUI();
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || totalCoins < amount)
            return false;

        totalCoins -= amount;
        UpdateUI();
        return true;
    }

    private void UpdateUI()
    {
        if (coinText != null)
            coinText.text = totalCoins.ToString("D9");
    }

    public void SetRewardMultiplier(int multiplier)
    {
        rewardMultiplier = Mathf.Max(1, multiplier);
    }
}
