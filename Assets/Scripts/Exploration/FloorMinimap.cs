using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CardBattle;
using Procedural;

namespace CardBattle
{
    /// <summary>
    /// Corner minimap unlocked via the Whiteboard hub upgrade (Req 41).
    ///
    /// Reveal levels (driven by MetaState.hubUpgradeLevels["Whiteboard"]):
    ///   0 — no minimap displayed
    ///   1 — show visited rooms only (grey squares)
    ///   2 — show room type icons in visited rooms
    ///   3 — reveal full floor layout including unvisited rooms
    ///
    /// Call UpdateVisited(roomGO) whenever the player enters a room.
    /// Call Refresh() after the floor is generated to rebuild the display.
    /// </summary>
    public class FloorMinimap : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] RectTransform mapContainer;   // parent panel for room icons
        [SerializeField] GameObject roomIconPrefab;    // simple Image prefab

        [Header("Icon Sprites — by room type")]
        [SerializeField] Sprite officeSprite;
        [SerializeField] Sprite bathroomSprite;
        [SerializeField] Sprite breakRoomSprite;
        [SerializeField] Sprite bossRoomSprite;
        [SerializeField] Sprite exitSprite;

        [Header("Colors")]
        [SerializeField] Color visitedColor   = Color.white;
        [SerializeField] Color unvisitedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("Layout")]
        [SerializeField] float iconSize   = 20f;
        [SerializeField] float iconSpacing = 4f;
        [SerializeField] float mapScale   = 0.06f; // world-units → minimap pixels

        // ── Runtime state ──────────────────────────────────────────────────────

        private int _revealLevel;
        private readonly Dictionary<GameObject, RoomIconEntry> _roomIcons
            = new Dictionary<GameObject, RoomIconEntry>();
        private readonly HashSet<GameObject> _visitedRooms = new HashSet<GameObject>();

        private struct RoomIconEntry
        {
            public GameObject iconGO;
            public Image image;
            public RoomType roomType;
            public bool isExit;
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            // Read reveal level from MetaState (0 if no upgrade).
            _revealLevel = GetRevealLevel();
            gameObject.SetActive(_revealLevel > 0);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the minimap after a new floor is generated.
        /// Pass all placed room GameObjects and their types from LevelGenerator.
        /// </summary>
        public void Refresh(IEnumerable<(GameObject roomGO, RoomType type, Vector3 worldPos)> rooms,
                            GameObject exitGO)
        {
            _revealLevel = GetRevealLevel();
            gameObject.SetActive(_revealLevel > 0);
            if (_revealLevel == 0) return;

            ClearIcons();
            _visitedRooms.Clear();

            foreach (var (roomGO, type, worldPos) in rooms)
            {
                Vector2 mapPos = WorldToMapPos(worldPos);
                GameObject icon = CreateIcon(mapPos);
                Image img = icon.GetComponent<Image>();

                _roomIcons[roomGO] = new RoomIconEntry
                {
                    iconGO  = icon,
                    image   = img,
                    roomType = type,
                    isExit  = false
                };
            }

            // Exit icon
            if (exitGO != null)
            {
                Vector2 exitMapPos = WorldToMapPos(exitGO.transform.position);
                GameObject exitIcon = CreateIcon(exitMapPos);
                Image exitImg = exitIcon.GetComponent<Image>();
                _roomIcons[exitGO] = new RoomIconEntry
                {
                    iconGO  = exitIcon,
                    image   = exitImg,
                    roomType = RoomType.Office, // doesn't matter
                    isExit  = true
                };
            }

            UpdateAllIcons();
        }

        /// <summary>
        /// Call when the player enters a room to mark it as visited.
        /// </summary>
        public void UpdateVisited(GameObject roomGO)
        {
            if (roomGO == null || _revealLevel == 0) return;
            _visitedRooms.Add(roomGO);
            UpdateAllIcons();
        }

        /// <summary>
        /// Updates the reveal level (e.g. after a hub upgrade purchase).
        /// </summary>
        public void SetRevealLevel(int level)
        {
            _revealLevel = Mathf.Clamp(level, 0, 3);
            gameObject.SetActive(_revealLevel > 0);
            UpdateAllIcons();
        }

        // ── Icon management ────────────────────────────────────────────────────

        private void UpdateAllIcons()
        {
            foreach (var kvp in _roomIcons)
            {
                GameObject roomGO = kvp.Key;
                RoomIconEntry entry = kvp.Value;

                bool visited = _visitedRooms.Contains(roomGO);
                bool show    = _revealLevel >= 3 || visited; // level 3 reveals all

                entry.iconGO.SetActive(show);

                if (!show) continue;

                // Level 1: grey square for visited rooms, no type icon.
                // Level 2+: show type-specific sprite.
                // Level 3: show unvisited rooms in dim color.
                if (_revealLevel >= 2)
                    entry.image.sprite = GetSprite(entry);
                else
                    entry.image.sprite = null; // plain square

                entry.image.color = visited ? visitedColor : unvisitedColor;
            }
        }

        private Sprite GetSprite(RoomIconEntry entry)
        {
            if (entry.isExit)    return exitSprite;
            return entry.roomType switch
            {
                RoomType.Bathroom  => bathroomSprite,
                RoomType.BreakRoom => breakRoomSprite,
                RoomType.BossRoom  => bossRoomSprite,
                _                  => officeSprite,
            };
        }

        private GameObject CreateIcon(Vector2 mapPos)
        {
            GameObject icon = Instantiate(roomIconPrefab, mapContainer);
            RectTransform rt = icon.GetComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(iconSize, iconSize);
            rt.anchoredPosition = mapPos;
            return icon;
        }

        private void ClearIcons()
        {
            foreach (var entry in _roomIcons.Values)
                if (entry.iconGO != null) Destroy(entry.iconGO);
            _roomIcons.Clear();
        }

        private Vector2 WorldToMapPos(Vector3 worldPos)
        {
            return new Vector2(worldPos.x * mapScale, worldPos.z * mapScale);
        }

        // ── Hub upgrade lookup ─────────────────────────────────────────────────

        private static int GetRevealLevel()
        {
            if (SaveManager.Instance == null) return 0;
            MetaState meta = SaveManager.Instance.CurrentMeta;
            if (meta?.hubUpgradeLevels == null) return 0;

            foreach (var pair in meta.hubUpgradeLevels)
            {
                if (pair.key == "Whiteboard")
                    return Mathf.Clamp(pair.value, 0, 3);
            }
            return 0;
        }
    }
}
