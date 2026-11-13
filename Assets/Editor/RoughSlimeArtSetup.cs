using UnityEditor;
using UnityEngine;

public static class RoughSlimeArtSetup
{
    private static readonly string[] Names = { "Green", "Blue", "Red", "Yellow", "Toxic", "Mega" };
    private static readonly string[] Prefabs =
    {
        "Green Slime", "Blue Slime", "Red Slime", "Yellow Electric Slime", "Toxic Slime", "MEGA Slime"
    };

    [MenuItem("Tools/Apply Rough Slime Art")]
    public static void Apply()
    {
        for (int i = 0; i < Names.Length; i++)
        {
            string spritePath = $"Assets/Resources/RoughSlimes/{Names[i]}.png";
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Missing slime art: {spritePath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogError($"Could not import slime sprite: {spritePath}");
                return;
            }

            string prefabPath = $"Assets/Prefabs/{Prefabs[i]}.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SpriteRenderer original = root.GetComponent<SpriteRenderer>();
                if (original == null)
                {
                    Debug.LogError($"No SpriteRenderer on {prefabPath}");
                    return;
                }

                Transform previous = root.transform.Find("Rough Slime Art");
                if (previous != null)
                    Object.DestroyImmediate(previous.gameObject);

                GameObject art = new GameObject("Rough Slime Art");
                art.layer = root.layer;
                art.transform.SetParent(root.transform, false);
                SpriteRenderer visible = art.AddComponent<SpriteRenderer>();
                visible.sprite = sprite;
                visible.sortingLayerID = original.sortingLayerID;
                visible.sortingOrder = original.sortingOrder;
                art.AddComponent<RoughSlimeIdle>();
                original.enabled = false;

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied rough pixel art to all six slime enemy prefabs.");
    }
}
