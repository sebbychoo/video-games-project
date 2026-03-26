#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class CreateFontAsset
{
    [MenuItem("Tools/Create TMP Font Asset From Selected Font")]
    static void Create()
    {
        Object selected = Selection.activeObject;
        if (selected == null || !(selected is Font font))
        {
            Debug.LogError("Select a .ttf or .otf font file first.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selected);
        string dir = System.IO.Path.GetDirectoryName(path);
        string name = System.IO.Path.GetFileNameWithoutExtension(path);
        string savePath = $"{dir}/{name} SDF.asset";

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
        if (fontAsset == null)
        {
            Debug.LogError("Failed to create font asset.");
            return;
        }

        AssetDatabase.CreateAsset(fontAsset, savePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created TMP font asset at: {savePath}");
        Selection.activeObject = fontAsset;
    }
}
#endif
