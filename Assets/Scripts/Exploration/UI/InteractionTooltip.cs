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
    [SerializeField] [Range(0.02f, 0.2f)] private float raycastInterval = 0.08f;

    private float _hoverTimer;
    private float _raycastTimer;
    private IInteractable _currentTarget;
    private IInteractable _cachedRaycastResult;
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
            if (i == null || (i as MonoBehaviour) == null) continue;
            best = i;
            break;
        }

        // Priority 2: throttled raycast for distant objects (runs every raycastInterval seconds)
        if (best == null && playerCamera != null)
        {
            _raycastTimer += Time.deltaTime;
            if (_raycastTimer >= raycastInterval)
            {
                _raycastTimer = 0f;
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactRange, ~0, QueryTriggerInteraction.Collide))
                {
                    var interactable = hit.collider.GetComponentInParent<IInteractable>();
                    if (interactable != null)
                    {
                        float range = interactable.InteractRange > 0 ? interactable.InteractRange : interactRange;
                        _cachedRaycastResult = hit.distance <= range ? interactable : null;
                    }
                    else
                    {
                        _cachedRaycastResult = null;
                    }
                }
                else
                {
                    _cachedRaycastResult = null;
                }
            }
            best = _cachedRaycastResult;
        }
        else if (best != null)
        {
            // Inside a trigger — clear cached raycast so we don't ghost-show it later
            _cachedRaycastResult = null;
            _raycastTimer = 0f;
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

        // Periodically clean up destroyed references (not every frame)
        if (Time.frameCount % 60 == 0)
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
