
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

        totalCoins += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (coinText != null)
        {
            coinText.text = totalCoins.ToString("D9");
        }
    }
}
