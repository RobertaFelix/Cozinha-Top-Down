using UnityEngine;

public enum TimedPotionKind { MovementSpeed, CoinGain, Regeneration }

[CreateAssetMenu(menuName = "UnderGrub/Potions/Timed Effect")]
public sealed class TimedPotionEffect : PotionEffect
{
    [SerializeField] private string effectId;
    [SerializeField] private TimedPotionKind kind;
    [SerializeField, Min(1f)] private float multiplier = 1.5f;
    [SerializeField, Min(1f)] private float durationSeconds = 180f;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int healPerTick = 5;
    [SerializeField, Min(1)] private int tickSeconds = 5;
    public int HealPerTick => healPerTick;
    public int TickSeconds => tickSeconds;
    public string EffectId => effectId;
    public TimedPotionKind Kind => kind;
    public float Multiplier => multiplier;
    public float DurationSeconds => durationSeconds;
    public Sprite Icon => icon;
    public string Description
    {
        get
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            string effect = kind == TimedPotionKind.Regeneration
                ? $"Restores {healPerTick} HP every {tickSeconds} seconds."
                : kind == TimedPotionKind.MovementSpeed
                ? "Increases movement speed by " + ((multiplier - 1f) * 100f).ToString("0.#", culture) + "%."
                : "Multiplies coin gains by " + multiplier.ToString("0.##", culture) + "x.";
            string duration = durationSeconds >= 60f
                ? (durationSeconds / 60f).ToString("0.#", culture) + " minutes"
                : durationSeconds.ToString("0.#", culture) + " seconds";
            return effect + "\nDuration: " + duration;
        }
    }

    public override bool TryApply(GameObject user) => user != null && user.GetComponent<PlayerController>() != null &&
        TimedPotionEffects.Instance != null && TimedPotionEffects.Instance.TryApply(this, user.GetComponent<PlayerHealth>());
}
