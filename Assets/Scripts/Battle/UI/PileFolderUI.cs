using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace CardBattle
{
    /// <summary>
    /// A folder icon that opens on hover to reveal pile buttons (Inbox/Draw, Archive/Discard, Trash/Exhaust).
    /// Buttons appear vertically to the left. Stays open while mouse is in the hover area.
    /// </summary>
    public class PileFolderUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Folder Sprites")]
        [SerializeField] Sprite folderClosed;
        [SerializeField] Sprite folderOpen;

        [Header("References")]
        [SerializeField] Image folderImage;
        [SerializeField] GameObject pileButtonsContainer;

        [Header("Pile Buttons (assign in Inspector)")]
        [SerializeField] Button inboxButton;
        [SerializeField] Button archiveButton;
        [SerializeField] Button trashButton;

        public event Action OnInboxClicked;
        public event Action OnArchiveClicked;
        public event Action OnTrashClicked;

        private bool _isOpen;

        private void Awake()
        {
            if (folderImage == null)
                folderImage = GetComponent<Image>();

            if (pileButtonsContainer != null)
                pileButtonsContainer.SetActive(false);

            if (folderImage != null && folderClosed != null)
                folderImage.sprite = folderClosed;

            if (inboxButton != null)
                inboxButton.onClick.AddListener(() => OnInboxClicked?.Invoke());
            if (archiveButton != null)
                archiveButton.onClick.AddListener(() => OnArchiveClicked?.Invoke());
            if (trashButton != null)
                trashButton.onClick.AddListener(() => OnTrashClicked?.Invoke());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OpenFolder();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CloseFolder();
        }

        private void OpenFolder()
        {
            _isOpen = true;
            Debug.Log("[PileFolder] Opening folder");
            if (folderImage != null && folderOpen != null)
                folderImage.sprite = folderOpen;
            if (pileButtonsContainer != null)
            {
                pileButtonsContainer.SetActive(true);
                Debug.Log($"[PileFolder] Container '{pileButtonsContainer.name}' active: {pileButtonsContainer.activeSelf}, activeInHierarchy: {pileButtonsContainer.activeInHierarchy}, children: {pileButtonsContainer.transform.childCount}");
            }
            else
            {
                Debug.LogWarning("[PileFolder] pileButtonsContainer is NULL — not assigned in Inspector!");
            }
        }

        private void CloseFolder()
        {
            _isOpen = false;
            if (folderImage != null && folderClosed != null)
                folderImage.sprite = folderClosed;
            if (pileButtonsContainer != null)
                pileButtonsContainer.SetActive(false);
        }

        public bool IsOpen => _isOpen;
    }
}
