using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public struct CardTransformTarget
    {
        public Vector2 anchoredPosition;
        public Vector3 rotation;   // euler
        public float   zOffset;
    }

    public class CardLayoutController : MonoBehaviour
    {
        [SerializeField] float arcRadius        = 800f;
        [SerializeField] float maxAngleSpread   = 30f;
        [SerializeField] float anglePerCard     = 5f;
        [SerializeField] float minCardSpacing   = 160f; // minimum pixel gap between card centers
        [SerializeField] float depthOffsetScale = 10f;
        [SerializeField] Vector2 arcCenter;
        [SerializeField] float neighborSeparation = 40f;

        /// <summary>
        /// Returns the target RectTransform state for card at index i of count total.
        /// </summary>
        public CardTransformTarget GetTargetTransform(int index, int count)
        {
            if (count <= 1)
            {
                return new CardTransformTarget
                {
                    anchoredPosition = arcCenter,
                    rotation         = Vector3.zero,
                    zOffset          = 0f
                };
            }

            // Scale spread based on card count so fewer cards stay tight
            float spread = Mathf.Min((count - 1) * anglePerCard, maxAngleSpread);

            // Ensure minimum spacing — convert minCardSpacing to angle at this radius
            float minAnglePerGap = Mathf.Rad2Deg * (minCardSpacing / arcRadius);
            float minSpread = (count - 1) * minAnglePerGap;
            spread = Mathf.Clamp(spread, minSpread, maxAngleSpread);

            float t     = (float)index / (count - 1);
            float angle = Mathf.Lerp(-spread / 2f, spread / 2f, t);

            // Convert angle to radians; arc opens upward from arcCenter
            float rad = angle * Mathf.Deg2Rad;
            float x   = arcCenter.x + arcRadius * Mathf.Sin(rad);
            float y   = arcCenter.y + arcRadius * (Mathf.Cos(rad) - 1f);

            // Z offset: edge cards slightly behind center (cosine-based, 0 at edges, max at center)
            float zOffset = -(1f - Mathf.Cos(rad)) * depthOffsetScale;

            return new CardTransformTarget
            {
                anchoredPosition = new Vector2(x, y),
                rotation         = new Vector3(0f, 0f, -angle),
                zOffset          = zOffset
            };
        }

        /// <summary>
        /// Applies layout transforms to each card, optionally shifting neighbors of the hovered card.
        /// </summary>
        public void RefreshLayout(IReadOnlyList<CardInstance> cards, int hoveredIndex = -1)
        {
            int count = cards.Count;
            for (int i = 0; i < count; i++)
            {
                CardTransformTarget target = GetTargetTransform(i, count);

                // Shift neighbors of hovered card outward
                if (hoveredIndex >= 0)
                {
                    if (i == hoveredIndex - 1)
                    {
                        target.anchoredPosition += Vector2.left * neighborSeparation;
                    }
                    else if (i == hoveredIndex + 1)
                    {
                        target.anchoredPosition += Vector2.right * neighborSeparation;
                    }
                }

                RectTransform rt = cards[i].RectTransform;
                if (rt == null) continue;

                rt.anchoredPosition  = target.anchoredPosition;
                rt.localEulerAngles  = target.rotation;
                // Apply z offset via localPosition z
                Vector3 pos = rt.localPosition;
                pos.z = target.zOffset;
                rt.localPosition = pos;
            }
        }
    }
}
