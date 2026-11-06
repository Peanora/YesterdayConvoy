namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        // Runtime context only; the referenced ScriptableObject is never mutated by shop or battle state.
        SynergyTrialDefinition activeSynergyTrial;
    }
}
