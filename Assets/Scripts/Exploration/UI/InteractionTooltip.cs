using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CardBattle;

/// <summary>
/// Attach to the Player. Shows tooltip when player is inside an IInteractable's trigger collider.
/// Also supports raycast detection for objects the player is looking at from a distance.
/// </summary>
public class InteractionTooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    [Header("Settings")]
    [SerializeField] private float interactRange = 8f;
    [SerializeField] private float showDelay = 0.3f;
    [SerializeField] private string defaultPrompt = "Press E to interact";

    private float _hoverTimer;
    private IInteractable _currentTarget;
    private HashSet<IInteractable> _insideTriggers = new HashSet<IInteractable>();

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        HideTooltip();
    }

    private void OnTriggerEnter(Collider other)
    {
        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null)
            _insideTriggers.Add(interactable);
    }

    private void OnTriggerExit(Collider other)
    {
        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null)
            _insideTriggers.Remove(interactable);
    }

    private void Update()
    {
        if (WorkBoxTrigger.IsInteracting
            || BathroomShopTrigger.IsInteracting
            || BreakRoomTradeTrigger.IsInteracting
            || BossCutsceneController.IsInteracting)
        {
            _currentTarget = null;
            _hoverTimer = 0f;
            HideTooltip();
            return;
        }

        // Priority 1: check if we're inside any interactable's trigger
        IInteractable best = null;
        foreach (var i in _insideTriggers)
        {
            // Skip destroyed objects
            if (i == null || (i as MonoBehaviour) == null) continue;
            best = i;
            break;
        }

        // Priority 2: raycast for distant objects
        if (best == null && playerCamera != null)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, ~0, QueryTriggerInteraction.Collide))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    float range = interactable.InteractRange > 0 ? interactable.InteractRange : interactRange;
                    if (hit.distance <= range)
                        best = interactable;
                }
            }
        }

        if (best != null)
        {
            if (best != _currentTarget)
            {
                _currentTarget = best;
                _hoverTimer = 0f;
            }
            _hoverTimer += Time.deltaTime;
            if (_hoverTimer >= showDelay)
            {
                string prompt = best.InteractPrompt ?? defaultPrompt;
                ShowTooltip(prompt);
            }
        }
        else
        {
            _currentTarget = null;
            _hoverTimer = 0f;
            HideTooltip();
        }

        // Clean up destroyed references
        _insideTriggers.RemoveWhere(i => i == null || (i as MonoBehaviour) == null);
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
