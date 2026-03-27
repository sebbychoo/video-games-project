using UnityEngine;
using UnityEngine.EventSystems;
using CardBattle;

/// <summary>
/// Lives in the Explorationscene. On scene start, checks if the current run
/// needs a starting deck selection. If so, shows the StartingDeckCarousel,
/// freezes the game, and waits for the player to pick a deck before
/// unlocking exploration.
/// </summary>
public class RunStartController : MonoBehaviour
{
    [Header("Deck Selection")]
    [SerializeField] StartingDeckCarousel startingDeckCarousel;

    private void Start()
    {
        if (startingDeckCarousel != null)
            startingDeckCarousel.gameObject.SetActive(false);

        if (!NeedsDeckSelection())
            return;

        ShowDeckSelection();
    }

    private bool NeedsDeckSelection()
    {
        if (SaveManager.Instance == null) return false;
        RunState run = SaveManager.Instance.CurrentRun;
        if (run == null) return false;
        return run.deckCardIds == null || run.deckCardIds.Count == 0;
    }

    private void ShowDeckSelection()
    {
        // Freeze everything — player, enemies, physics, all of it
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        startingDeckCarousel.gameObject.SetActive(true);
        startingDeckCarousel.Initialize();
        startingDeckCarousel.OnDeckSelected += OnDeckSelected;
    }

    private void OnDeckSelected()
    {
        startingDeckCarousel.OnDeckSelected -= OnDeckSelected;
        startingDeckCarousel.gameObject.SetActive(false);

        if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
            SaveManager.Instance.CurrentRun.isActive = true;

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveRun();

        // Unfreeze
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }
}
