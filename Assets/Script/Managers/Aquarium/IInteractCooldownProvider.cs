public interface IInteractCooldownProvider
{
    bool ShouldShowCooldownUI();
    float GetCooldownRemainingSeconds();
    float GetCooldownDurationSeconds();
}
