using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;

public static class ChickenPrefabGenerator
{
    private const string ChickenPrefabPath = "Assets/Prefab/Entities/Chicken.prefab";
    private const string OutputFolder = "Assets/Prefab/Entities/Chicken";
    private const string PlaceholderSpritePath = "Assets/Sprites/ChickenCirclePlaceholder.png";

    private const int NpcLayer = 8;
    private const int PlayerLayer = 6;

    [MenuItem("Tools/DUDE/Generate Chicken Prefabs")]
    public static void Generate()
    {
        // 0) Load Chicken.prefab to grab its sprite
        var chickenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChickenPrefabPath);
        if (chickenPrefab == null)
        {
            Debug.LogError($"[ChickenPrefabGen] Chicken.prefab not found at {ChickenPrefabPath}");
            return;
        }

        var chickenSprite = chickenPrefab.GetComponentInChildren<SpriteRenderer>();
        Sprite chickenSpriteAsset = chickenSprite != null ? chickenSprite.sprite : null;
        int chickenSortingOrder = chickenSprite != null ? chickenSprite.sortingOrder : 0;

        // 1) Generate placeholder circle sprite
        Sprite placeholderSprite = GetOrCreatePlaceholderSprite();

        // 2) Ensure output folder exists
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefab/Entities", "Chicken");
            Debug.Log($"[ChickenPrefabGen] Created folder: {OutputFolder}");
        }

        // 3) Generate prefabs
        var miniPrefab = CreateMiniChickenRevenge(chickenSpriteAsset, chickenSortingOrder);
        var eggPrefab = CreateChickenRevengeEgg(placeholderSprite, chickenSortingOrder);
        var speedPrefab = CreateChickenSpeedBoost(placeholderSprite, chickenSortingOrder);
        var textPrefab = CreateChickenFloatingText();

        // 4) Auto-wire to Chicken.prefab
        if (miniPrefab != null || eggPrefab != null || speedPrefab != null || textPrefab != null)
        {
            AutoWireChickenPrefab(chickenPrefab, miniPrefab, eggPrefab, speedPrefab, textPrefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ChickenPrefabGen] Done! All chicken prefabs generated.");
    }

    // ==================== PLACEHOLDER SPRITE ====================

    private static Sprite GetOrCreatePlaceholderSprite()
    {
        // If already exists, reuse it
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        if (existing != null)
        {
            Debug.Log($"[ChickenPrefabGen] Placeholder sprite already exists: {PlaceholderSpritePath}");
            return existing;
        }

        // Generate 32x32 white circle PNG
        int sz = 32;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        float c = sz * 0.5f, r = c - 1f;
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
                tex.SetPixel(x, y,
                    Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) <= r
                        ? Color.white : Color.clear);
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        string dir = Path.GetDirectoryName(PlaceholderSpritePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(PlaceholderSpritePath, pngData);
        AssetDatabase.ImportAsset(PlaceholderSpritePath, ImportAssetOptions.ForceUpdate);

        // Configure as sprite
        var importer = AssetImporter.GetAtPath(PlaceholderSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        Debug.Log($"[ChickenPrefabGen] Created placeholder sprite: {PlaceholderSpritePath}");
        return sprite;
    }

    // ==================== MINI CHICKEN REVENGE ====================

    private static GameObject CreateMiniChickenRevenge(Sprite chickenSprite, int sortingOrder)
    {
        string path = $"{OutputFolder}/MiniChickenRevenge.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.LogWarning($"[ChickenPrefabGen] Already exists, skipping: {path}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        var go = new GameObject("MiniChickenRevenge");
        go.layer = NpcLayer;
        go.transform.localScale = Vector3.one * 0.3f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = chickenSprite;
        sr.color = new Color(1f, 0.3f, 0.3f);
        sr.sortingOrder = sortingOrder;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        go.AddComponent<MiniChickenRevenge>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[ChickenPrefabGen] Created: {path}");
        return prefab;
    }

    // ==================== CHICKEN REVENGE EGG ====================

    private static GameObject CreateChickenRevengeEgg(Sprite placeholder, int sortingOrder)
    {
        string path = $"{OutputFolder}/ChickenRevengeEgg.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.LogWarning($"[ChickenPrefabGen] Already exists, skipping: {path}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        var go = new GameObject("ChickenRevengeEgg");
        go.layer = NpcLayer;
        go.transform.localScale = Vector3.one * 0.4f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = placeholder;
        sr.color = Color.white;
        sr.sortingOrder = sortingOrder;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = false;

        var egg = go.AddComponent<ChickenRevengeEgg>();
        // Set playerMask via serialized field — layer 6 (Player)
        var so = new SerializedObject(egg);
        var maskProp = so.FindProperty("playerMask");
        if (maskProp != null)
        {
            maskProp.intValue = 1 << PlayerLayer;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[ChickenPrefabGen] Created: {path}");
        return prefab;
    }

    // ==================== CHICKEN SPEED BOOST ====================

    private static GameObject CreateChickenSpeedBoost(Sprite placeholder, int sortingOrder)
    {
        string path = $"{OutputFolder}/ChickenSpeedBoost.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.LogWarning($"[ChickenPrefabGen] Already exists, skipping: {path}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        var go = new GameObject("ChickenSpeedBoost");
        go.layer = NpcLayer;
        go.transform.localScale = Vector3.one * 0.3f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = placeholder;
        sr.color = new Color(0.3f, 1f, 0.8f);
        sr.sortingOrder = sortingOrder;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        go.AddComponent<ChickenSpeedBoost>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[ChickenPrefabGen] Created: {path}");
        return prefab;
    }

    // ==================== CHICKEN FLOATING TEXT ====================

    private static GameObject CreateChickenFloatingText()
    {
        string path = $"{OutputFolder}/ChickenFloatingText.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.LogWarning($"[ChickenPrefabGen] Already exists, skipping: {path}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        var go = new GameObject("ChickenFloatingText");

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "+0";
        tmp.fontSize = 6;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.yellow;
        tmp.sortingOrder = 100;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[ChickenPrefabGen] Created: {path}");
        return prefab;
    }

    // ==================== AUTO-WIRE TO CHICKEN.PREFAB ====================

    private static void AutoWireChickenPrefab(
        GameObject chickenPrefab,
        GameObject miniPrefab,
        GameObject eggPrefab,
        GameObject speedPrefab,
        GameObject textPrefab)
    {
        var chicken = chickenPrefab.GetComponent<RainbowChicken>();
        if (chicken == null)
        {
            Debug.LogError("[ChickenPrefabGen] Chicken.prefab has no RainbowChicken component!");
            return;
        }

        var so = new SerializedObject(chicken);
        int wired = 0;

        if (miniPrefab != null)
        {
            var prop = so.FindProperty("revengeChickenPrefab");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = miniPrefab;
                wired++;
            }
        }

        if (eggPrefab != null)
        {
            var prop = so.FindProperty("revengeEggPrefab");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = eggPrefab;
                wired++;
            }
        }

        if (speedPrefab != null)
        {
            var prop = so.FindProperty("speedBoostPrefab");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = speedPrefab;
                wired++;
            }
        }

        if (textPrefab != null)
        {
            var prop = so.FindProperty("pointsCanvasPrefab");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = textPrefab;
                wired++;
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(chickenPrefab);
        Debug.Log($"[ChickenPrefabGen] Auto-wired {wired} prefab references to Chicken.prefab");
    }
}
