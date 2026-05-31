using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>圓形 UI 遮罩與劇情截圖擷取（不依賴自訂 shader）。</summary>
public static class TutorialPlotIrisMaskUtil
{
    private static Sprite circleMaskSprite;

    public static Sprite GetCircleMaskSprite()
    {
        if (circleMaskSprite != null)
            return circleMaskSprite;

        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.Alpha8, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = center - 1f;
        float radiusSq = radius * radius;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                byte a = dx * dx + dy * dy <= radiusSq ? (byte)255 : (byte)0;
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        circleMaskSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        return circleMaskSprite;
    }

    private const int TransitionOverlaySortOrderThreshold = 60000;

    /// <summary>等畫面渲染完成後擷取；請用此 coroutine，勿在 WaitForEndOfFrame 後再 yield null。</summary>
    public static IEnumerator CaptureScreenAfterFrameEnd(Action<Texture2D> onCaptured, bool usePlotSnapshotFallback = false)
    {
        Canvas[] disabledOverlays = SetTransitionOverlayCanvasesEnabled(false);
        yield return null;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        Texture2D captured = null;
        if (usePlotSnapshotFallback)
        {
            captured = MainPlotSceneController.TryBuildTransitionSnapshotTexture();
            if (captured != null && !IsTextureMostlyBlack(captured))
            {
                GameDevLog.Log("TutorialPlotIrisMaskUtil: using plot sprite composite for iris close.");
                SetTransitionOverlayCanvasesEnabled(disabledOverlays, true);
                onCaptured?.Invoke(captured);
                yield break;
            }

            if (captured != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(captured);
                else
                    UnityEngine.Object.DestroyImmediate(captured);
                captured = null;
            }
        }

        captured = CaptureScreenToTexture();
        if (usePlotSnapshotFallback && (captured == null || IsTextureMostlyBlack(captured)))
        {
            if (captured != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(captured);
                else
                    UnityEngine.Object.DestroyImmediate(captured);
                captured = null;
            }

            captured = MainPlotSceneController.TryBuildTransitionSnapshotTexture();
            if (captured != null)
                GameDevLog.Log("TutorialPlotIrisMaskUtil: screen capture was black; using plot sprite composite.");
        }

        SetTransitionOverlayCanvasesEnabled(disabledOverlays, true);
        onCaptured?.Invoke(captured);
    }

    public static IEnumerator CaptureSceneAfterFrameEnd(string sceneName, Action<Texture2D> onCaptured)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        onCaptured?.Invoke(CaptureSceneToTexture(sceneName));
    }

    /// <summary>須在 <see cref="WaitForEndOfFrame"/> 剛結束的同一 continuation 內呼叫。</summary>
    public static Texture2D CaptureScreenToTexture()
    {
        int w = Mathf.Max(1, Screen.width);
        int h = Mathf.Max(1, Screen.height);

        Texture2D fromScreenCapture = TryScreenCaptureAsTexture();
        if (fromScreenCapture != null)
            return fromScreenCapture;

        Texture2D fromBackBuffer = TryReadScreenBackBuffer(w, h);
        if (fromBackBuffer != null)
            return fromBackBuffer;

        return CaptureFromMainCamera(w, h);
    }

    public static void EnsureSceneRenderingEnabled(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            GameObject root = roots[r];
            if (root == null) continue;

            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null)
                    cameras[i].enabled = true;
            }

            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null)
                    canvases[i].enabled = true;
            }
        }
    }

    private static Texture2D TryReadScreenBackBuffer(int w, int h)
    {
        RenderTexture previousActive = RenderTexture.active;
        try
        {
            RenderTexture.active = null;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
            tex.Apply(false, false);
            return tex;
        }
        catch (Exception ex)
        {
            GameDevLog.LogWarning("TutorialPlotIrisMaskUtil: ReadPixels failed -> " + ex.Message);
            return null;
        }
        finally
        {
            RenderTexture.active = previousActive;
        }
    }

    private static Texture2D TryScreenCaptureAsTexture()
    {
        try
        {
            return ScreenCapture.CaptureScreenshotAsTexture();
        }
        catch (Exception ex)
        {
            GameDevLog.LogWarning("TutorialPlotIrisMaskUtil: CaptureScreenshotAsTexture failed -> " + ex.Message);
            return null;
        }
    }

    public static Texture2D CaptureSceneToTexture(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return null;

        EnsureSceneRenderingEnabled(sceneName);
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        Camera cam = FindPrimaryCameraInScene(scene);
        if (cam == null)
            return CaptureScreenToTexture();

        int w = Mathf.Max(1, Screen.width);
        int h = Mathf.Max(1, Screen.height);
        return RenderCameraToTexture(cam, w, h);
    }

    public static bool IsTextureMostlyBlack(Texture2D texture, float maxAverageLuminance = 0.07f)
    {
        if (texture == null)
            return true;

        try
        {
            const int sampleCount = 48;
            float sum = 0f;
            int w = texture.width;
            int h = texture.height;
            for (int i = 0; i < sampleCount; i++)
            {
                int x = (int)(w * (0.15f + 0.7f * ((i * 17) % 100) / 100f));
                int y = (int)(h * (0.15f + 0.7f * ((i * 31) % 100) / 100f));
                Color c = texture.GetPixel(x, y);
                sum += c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            }

            return sum / sampleCount <= maxAverageLuminance;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>以 Blit 合成劇情圖（圖集不可 Read/Write 也能用）。</summary>
    public static Texture2D BuildSnapshotFromSprites(params Sprite[] sprites)
    {
        int w = Mathf.Max(1, Screen.width);
        int h = Mathf.Max(1, Screen.height);
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;
        Material blitMat = null;

        try
        {
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);

            if (sprites != null)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] == null)
                        continue;
                    blitMat ??= CreateSpriteBlitMaterial(sprites[i]);
                    BlitSpriteCover(rt, sprites[i], blitMat);
                }
            }

            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
            tex.Apply(false, false);
            return tex;
        }
        finally
        {
            if (blitMat != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(blitMat);
                else
                    UnityEngine.Object.DestroyImmediate(blitMat);
            }

            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    private static Material CreateSpriteBlitMaterial(Sprite sprite)
    {
        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
            shader = Shader.Find("UI/Default");
        var mat = new Material(shader);
        mat.mainTexture = sprite.texture;
        return mat;
    }

    private static void BlitSpriteCover(RenderTexture dest, Sprite sprite, Material mat)
    {
        if (sprite == null || sprite.texture == null || dest == null || mat == null)
            return;

        Texture2D tex = sprite.texture;
        Rect tr = sprite.textureRect;
        mat.mainTexture = tex;
        mat.SetTextureScale("_MainTex", new Vector2(tr.width / tex.width, tr.height / tex.height));
        mat.SetTextureOffset("_MainTex", new Vector2(tr.x / tex.width, tr.y / tex.height));
        Graphics.Blit(tex, dest, mat);
    }

    private static Camera FindPrimaryCameraInScene(Scene scene)
    {
        if (!scene.IsValid())
            return null;

        Camera fallback = null;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Camera[] cameras = roots[r].GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam == null || !cam.enabled)
                    continue;
                if (cam.CompareTag("MainCamera"))
                    return cam;
                fallback ??= cam;
            }
        }

        return fallback;
    }

    private static Texture2D RenderCameraToTexture(Camera cam, int w, int h)
    {
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousTarget = cam.targetTexture;
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previousTarget;

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
            tex.Apply(false, false);
            return tex;
        }
        finally
        {
            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    public static Canvas[] SetTransitionOverlayCanvasesEnabled(bool enabled)
    {
        Canvas[] all = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var disabled = new System.Collections.Generic.List<Canvas>(2);
        for (int i = 0; i < all.Length; i++)
        {
            Canvas canvas = all[i];
            if (canvas == null)
                continue;
            if (canvas.sortingOrder < TransitionOverlaySortOrderThreshold)
                continue;
            if (canvas.enabled == enabled)
                continue;

            disabled.Add(canvas);
            canvas.enabled = enabled;
        }

        return disabled.ToArray();
    }

    public static void SetTransitionOverlayCanvasesEnabled(Canvas[] canvases, bool enabled)
    {
        if (canvases == null)
            return;

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null)
                canvases[i].enabled = enabled;
        }
    }

    private static Texture2D CaptureFromMainCamera(int w, int h)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return null;

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousTarget = cam.targetTexture;
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previousTarget;

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
            tex.Apply(false, false);
            return tex;
        }
        finally
        {
            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    public static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
    }

    public static RawImage BuildShrinkingSnapshotPortal(Transform parent, Texture snapshot)
    {
        var portalGo = new GameObject("PlotSnapshotPortal", typeof(RectTransform));
        portalGo.transform.SetParent(parent, false);
        RectTransform portalRt = portalGo.GetComponent<RectTransform>();
        StretchFull(portalRt);

        var maskGo = new GameObject("CircleMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        maskGo.transform.SetParent(portalGo.transform, false);
        RectTransform maskRt = maskGo.GetComponent<RectTransform>();
        StretchFull(maskRt);

        Image maskImage = maskGo.GetComponent<Image>();
        maskImage.sprite = GetCircleMaskSprite();
        maskImage.color = Color.white;
        maskImage.raycastTarget = false;

        Mask mask = maskGo.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        var rawGo = new GameObject("Snapshot", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        rawGo.transform.SetParent(maskGo.transform, false);
        RectTransform rawRt = rawGo.GetComponent<RectTransform>();
        StretchFull(rawRt);

        RawImage raw = rawGo.GetComponent<RawImage>();
        raw.texture = snapshot;
        raw.color = Color.white;
        raw.raycastTarget = false;
        raw.uvRect = new Rect(0f, 0f, 1f, 1f);
        return raw;
    }
}
