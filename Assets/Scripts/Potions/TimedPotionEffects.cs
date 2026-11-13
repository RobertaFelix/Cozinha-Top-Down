using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class SavedPotionEffect
{
    public string effectId;
    public float remainingSeconds;
}
[Serializable] public sealed class TimedPotionSaveState
{
    public List<SavedPotionEffect> active = new List<SavedPotionEffect>();
}

// One persistent timer per effect. Refreshing a potion never multiplies its strength.
public sealed class TimedPotionEffects : SaveParticipant<TimedPotionSaveState>
{
    [SerializeField] private TimedPotionEffect[] definitions;
    private readonly Dictionary<string, float> remaining = new Dictionary<string, float>();
    public static TimedPotionEffects Instance { get; private set; }
    public override string SaveKey => "timed-potion-effects";
    public override int RestoreOrder => 25;
    public IReadOnlyList<TimedPotionEffect> Definitions => definitions;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { Instance = null; }
    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private TimedPotionEffect Find(string id)
    {
        if (definitions != null) foreach (var effect in definitions)
            if (effect != null && effect.EffectId == id) return effect;
        return null;
    }
    public float RemainingSeconds(string id) => !string.IsNullOrEmpty(id) && remaining.TryGetValue(id, out float seconds) ? seconds : 0f;
    public float GetMultiplier(TimedPotionKind kind)
    {
        float value = 1f;
        if (definitions != null) foreach (var effect in definitions)
            if (effect != null && effect.Kind == kind && RemainingSeconds(effect.EffectId) > 0f)
                value = Mathf.Max(value, effect.Multiplier);
        return value;
    }
    private PlayerHealth regenerationTarget;
    public bool TryApply(TimedPotionEffect effect, PlayerHealth target = null)
    {
        var saves = GetComponent<SaveGameService>();
        if (Instance != this || !isActiveAndEnabled || Time.timeScale <= 0 || (saves != null && !saves.IsReady) ||
            effect == null || Find(effect.EffectId) != effect || !Valid(effect.DurationSeconds) || effect.DurationSeconds <= 0 ||
            !Valid(effect.Multiplier) || effect.Multiplier < 1f) return false;
        if (effect.Kind == TimedPotionKind.Regeneration)
        {
            if (!target || !target.InDungeon) return false;
            regenerationTarget = target;
        }
        remaining[effect.EffectId] = effect.DurationSeconds;
        return true;
    }
    private void Update()
    {
        if (Instance != this || definitions == null || Time.deltaTime <= 0) return;
        var saves = GetComponent<SaveGameService>();
        if (saves != null && !saves.IsReady) return;
        foreach (var effect in definitions)
            if (effect != null && remaining.TryGetValue(effect.EffectId, out float seconds))
            {
                float previous = seconds;
                seconds = Mathf.Max(0, seconds - Time.deltaTime);
                if (effect.Kind == TimedPotionKind.Regeneration)
                {
                    if (!regenerationTarget || !regenerationTarget.InDungeon) seconds = 0;
                    else
                    {
                        int ticks = Mathf.FloorToInt((effect.DurationSeconds - seconds) / effect.TickSeconds)
                                  - Mathf.FloorToInt((effect.DurationSeconds - previous) / effect.TickSeconds);
                        if (ticks > 0) regenerationTarget.Heal(ticks * effect.HealPerTick);
                    }
                }
                if (seconds <= 0) remaining.Remove(effect.EffectId);
                else remaining[effect.EffectId] = seconds;
            }
    }
    protected override TimedPotionSaveState Capture()
    {
        var state = new TimedPotionSaveState();
        foreach (var effect in remaining)
            if (effect.Value > 0 && Find(effect.Key).Kind != TimedPotionKind.Regeneration) state.active.Add(new SavedPotionEffect { effectId = effect.Key, remainingSeconds = effect.Value });
        state.active.Sort((a,b) => string.CompareOrdinal(a.effectId,b.effectId));
        return state;
    }
    private static bool Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    protected override void Validate(TimedPotionSaveState state)
    {
        Require(state.active != null && state.active.Count <= 100, "Invalid timed potion effects.");
        var ids = new HashSet<string>();
        foreach (var effect in state.active)
            Require(effect != null && !string.IsNullOrEmpty(effect.effectId) && Find(effect.effectId) != null &&
                ids.Add(effect.effectId) && Valid(effect.remainingSeconds) && effect.remainingSeconds >= 0 && effect.remainingSeconds <= 86400,
                "Invalid active potion or remaining duration.");
    }
    protected override void Restore(TimedPotionSaveState state)
    {
        remaining.Clear();
        foreach (var effect in state.active) if (effect.remainingSeconds > 0) remaining.Add(effect.effectId, effect.remainingSeconds);
    }
}
