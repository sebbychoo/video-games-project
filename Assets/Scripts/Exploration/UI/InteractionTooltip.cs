using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardBattle;

/// <summary>
/// Attach to the Player. Raycasts from the camera each frame.
/// When the crosshair hits an IInteractable, shows a "Press E to interact" tooltip
/// after a short hover delay.
/// </summary>
public class InteractionTooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    [Header("Settings")]
    [SerializeField] private float interactRange = 4f;
    [SerializeField] private float showDelay = 0.4f;   // seconds before tooltip appears
    [SerializeField] private string defaultPrompt = "Press E to interact";

    private float _hoverTimer;
    private IInteractable _currentTarget;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        HideTooltip();
    }

    private void Update()
    {
        // Hide tooltip while interacting with any UI
        if (WorkBoxTrigger.IsInteracting
            || BathroomShopTrigger.IsInteracting
            || BreakRoomTradeTrigger.IsInteracting)
        {
            _currentTarget = null;
            _hoverTimer = 0f;
            HideTooltip();
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                // Check per-object range
                float range = interactable.InteractRange > 0 ? interactable.InteractRange : interactRange;
                if (hit.distance > range)
                {
                    _currentTarget = null;
                    _hoverTimer = 0f;
                    HideTooltip();
                    return;
                }

                if (interactable != _currentTarget)
                {
                    _currentTarget = interactable;
                    _hoverTimer = 0f;
                    HideTooltip();
                }

                _hoverTimer += Time.deltaTime;
                if (_hoverTimer >= showDelay)
                {
                    string prompt = interactable.InteractPrompt ?? defaultPrompt;
                    ShowTooltip(prompt);
                }
                return;
            }
        }

        // Nothing interactable in range
        _currentTarget = null;
        _hoverTimer = 0f;
        HideTooltip();
    }

    private void ShowTooltip(string text)
    {
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(true);
        if (tooltipText != null) tooltipText.text = text;
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}
