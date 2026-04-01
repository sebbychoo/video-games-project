namespace CardBattle
{
    /// <summary>
    /// Implement this on any object the player can interact with by pressing E.
    /// The InteractionTooltip system uses this to show "Press E to interact".
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Optional custom prompt text. Return null to use the default.</summary>
        string InteractPrompt { get; }

        /// <summary>Max distance the player can be to interact. Return 0 to use the global default.</summary>
        float InteractRange { get; }
    }
}
