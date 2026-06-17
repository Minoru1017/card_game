using UnityEditor;
using UnityEngine;

/// <summary>合併卡圖：依 Sprite 非透明區域重算 pivot，避免 UI preserveAspect 時立繪視覺偏移。</summary>
public static class CardArtSpritePivotUtility
{
    private const float AlphaThreshold = 0.05f;

    public static bool TrySetPivotToOpaqueCenter(string textureAssetPath, Sprite sprite)
    {
        if (sprite == null || string.IsNullOrWhiteSpace(textureAssetPath))
            return false;

        TextureImporter importer = AssetImporter.GetAtPath(textureAssetPath) as TextureImporter;
        if (importer == null || importer.textureType != TextureImporterType.Sprite)
            return false;

        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
            sprite = FindSpriteByName(textureAssetPath, sprite.name);
            if (sprite == null)
                return false;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
        if (tex == null)
            return false;

        Rect r = sprite.rect;
        int x0 = Mathf.FloorToInt(r.x);
        int y0 = Mathf.FloorToInt(r.y);
        int w = Mathf.FloorToInt(r.width);
        int h = Mathf.FloorToInt(r.height);
        if (w <= 0 || h <= 0)
            return false;

        Color[] px = tex.GetPixels(x0, y0, w, h);
        int minX = w;
        int minY = h;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (px[y * w + x].a <= AlphaThreshold)
                    continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
            return false;

        Vector2 newPivot = new Vector2(
            (minX + maxX + 1f) * 0.5f / w,
            (minY + maxY + 1f) * 0.5f / h);

        Vector2 oldPivotNorm = new Vector2(sprite.pivot.x / w, sprite.pivot.y / h);
        if (Vector2.Distance(oldPivotNorm, newPivot) < 0.01f)
            return false;

        bool applied = ApplySpritePivot(importer, sprite.name, newPivot);
        if (applied && !wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        return applied;
    }

    private static Sprite FindSpriteByName(string textureAssetPath, string spriteName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(textureAssetPath);
        if (assets == null)
            return null;
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite s && s != null && s.name == spriteName)
                return s;
        }
        return null;
    }

    private static bool ApplySpritePivot(TextureImporter importer, string spriteName, Vector2 pivot)
    {
        SerializedObject so = new SerializedObject(importer);
        SerializedProperty sprites = so.FindProperty("m_SpriteSheet.m_Sprites");
        if (sprites == null || !sprites.isArray)
            return false;

        for (int i = 0; i < sprites.arraySize; i++)
        {
            SerializedProperty element = sprites.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = element.FindPropertyRelative("m_Name");
            if (nameProp == null || nameProp.stringValue != spriteName)
                continue;

            SerializedProperty alignment = element.FindPropertyRelative("m_Alignment");
            SerializedProperty pivotProp = element.FindPropertyRelative("m_Pivot");
            if (alignment != null)
                alignment.intValue = (int)SpriteAlignment.Custom;
            if (pivotProp != null)
                pivotProp.vector2Value = pivot;

            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
            return true;
        }

        return false;
    }
}
