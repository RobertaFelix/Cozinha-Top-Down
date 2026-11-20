using UnityEngine;

[CreateAssetMenu(menuName = "UnderGrub/Potions/Save checkpoint")]
public sealed class SavePotionEffect : PotionEffect
{
    public string Description => "Saves your progress and current location without advancing the day.";
    public override bool TryApply(GameObject user)
    {
        var service = SaveGameService.Instance;
        if (user == null || service == null || !service.IsReady) return false;
        bool saved = service.SaveCheckpoint();
        MarketHoursNotice.Show(saved ? "Game saved." : "Could not save. Your potion was not consumed.");
        return saved;
    }
}
