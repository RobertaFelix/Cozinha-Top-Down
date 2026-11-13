using System;

[Serializable] public sealed class EconomySaveState
{
    public int coins;
    public int rewardMultiplier;
    public float pendingCoinFraction;
    public bool doubleDishPrices;
    public int[] purchased;
}
public sealed class EconomySaveModule : SaveParticipant<EconomySaveState>
{
    public override string SaveKey => "economy-upgrades";
    public override int RestoreOrder => 20;
    protected override EconomySaveState Capture() => new EconomySaveState
    {
        coins = CoinManager.Instance.TotalCoins,
        pendingCoinFraction = CoinManager.Instance.PendingCoinFraction,
        rewardMultiplier = CoinManager.Instance.RewardMultiplier,
        doubleDishPrices = CoinManager.Instance.AreDishPricesDoubled,
        purchased = UpgradePurchases.GetPurchased()
    };
    protected override void Validate(EconomySaveState state)
    {
        Require(state.coins >= 0 && state.rewardMultiplier >= 1 && state.purchased != null, "Invalid saved economy.");
        Require(!float.IsNaN(state.pendingCoinFraction) && state.pendingCoinFraction >= 0 && state.pendingCoinFraction < 1f, "Invalid pending coin fraction.");
        foreach (int index in state.purchased) Require(index >= 0, "Invalid upgrade ID.");
    }
    protected override void Restore(EconomySaveState state)
    {
        UpgradePurchases.RestorePurchases(state.purchased);
        CoinManager.Instance.RestoreEconomy(state.coins, state.rewardMultiplier, state.doubleDishPrices || UpgradePurchases.Has(9), state.pendingCoinFraction);
    }
}
