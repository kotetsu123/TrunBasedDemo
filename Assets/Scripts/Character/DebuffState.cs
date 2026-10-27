public sealed class DebuffState
{
    public DebuffEffectType EffectType { get; private set; }
    public string DisplayName { get; private set; }
    public int Power { get; private set; }
    public int RemainingTurns { get; private set; }
    public bool IsActive => RemainingTurns > 0;

    internal void Refresh(DebuffEffectType effectType, string displayName, int power, int durationTurns)
    {
        EffectType = effectType;
        DisplayName = displayName;
        Power = power;
        RemainingTurns = durationTurns;
    }

    internal bool TryAdvanceTurn(out DebuffEffectType effectType, out int power)
    {
        effectType = EffectType;
        power = Power;
        if (!IsActive) return false;

        RemainingTurns--;
        // Snapshot the effect before clearing so the final turn still applies it.
        if (!IsActive) Clear();
        return true;
    }

    internal void Clear()
    {
        EffectType = default;
        DisplayName = null;
        Power = 0;
        RemainingTurns = 0;
    }
}
