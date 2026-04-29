using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Editor
{
    /// <summary>
    /// One-shot menu item that sets alphaHitTestMinimumThreshold on the
    /// FilingCabinet and Chair Image components so transparent sprite padding
    /// is never clickable.
    /// </summary>
    public static class ApplyAlphaHitThreshold
    {
        private const float AlphaThreshold = 0.1f;

        private static readonly string[] TargetPaths = new[]
        {
            "Canvas/RightPanel/FilingCabinet",
            "Canvas/RightPanel/Chair",
            "Canvas/RightPanel/CoffeeMug",
        };

        [MenuItem("Tools/CardBattle/Apply Alpha Hit Threshold to Hub Items")]
        public static void Apply()
        {
            int applied = 0;

            foreach (string path in TargetPaths)
            {
                GameObject go = GameObject.Find(path);
                if (go == null)
                {
                    Debug.LogWarning($"ApplyAlphaHitThreshold: GameObject not found at '{path}'");
                    continue;
                }

                Image image = go.GetComponent<Image>();
                if (image == null)
                {
                    Debug.LogWarning($"ApplyAlphaHitThreshold: No Image component on '{path}'");
                    continue;
                }

                Undo.RecordObject(image, "Apply Alpha Hit Threshold");
                image.alphaHitTestMinimumThreshold = AlphaThreshold;
                EditorUtility.SetDirty(image);
                applied++;

                Debug.Log($"ApplyAlphaHitThreshold: Set threshold {AlphaThreshold} on '{path}'");
            }

            if (applied > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                Debug.Log($"ApplyAlphaHitThreshold: Done — applied to {applied} object(s). Save the scene.");
            }
        }
    }
}
