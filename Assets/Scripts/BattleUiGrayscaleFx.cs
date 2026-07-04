using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 戰鬥 UI 漸變去飽和（黑白）效果。致死慢動作期間啟用；勝負結算面板子樹排除在外。
/// </summary>
public static class BattleUiGrayscaleFx
{
    private const float RampToFullSeconds = 1.4f;
    private const float ReleaseSeconds = 0.55f;
    private const string UiGrayscaleShaderName = "UI/Grayscale";

    private static readonly int DesaturateId = Shader.PropertyToID("_Desaturate");

    private sealed class GraphicTrack
    {
        public Graphic graphic;
        public Material originalMaterial;
        public bool hadCustomMaterial;
    }

    private sealed class TmpTrack
    {
        public TMP_Text text;
        public Color baseVertexColor;
        public Color baseFaceColor;
        public Color baseOutlineColor;
        public bool hasOutline;
    }

    private static MonoBehaviour coroutineHost;
    private static Material sharedUiGrayscaleMaterial;
    private static float desaturateAmount;
    private static bool active;
    private static Coroutine rampRoutine;
    private static Coroutine refreshRoutine;
    private static readonly List<GraphicTrack> graphicTracks = new List<GraphicTrack>();
    private static readonly List<TmpTrack> tmpTracks = new List<TmpTrack>();
    private static Image battleBackgroundImage;

    public static float DesaturateAmount => desaturateAmount;
    public static bool IsActive => active;

    public static void SetCoroutineHost(MonoBehaviour host) => coroutineHost = host;

    /// <summary>致死慢動作開始：自 0 漸變至全黑白。</summary>
    public static void BeginGradualRamp()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (!EnsureMaterial()) return;

        active = true;
        StopRoutines();
        rampRoutine = coroutineHost != null
            ? coroutineHost.StartCoroutine(CoRampToFull())
            : null;
        if (rampRoutine == null)
        {
            SetAmount(1f);
            RefreshTargets();
        }

        refreshRoutine = coroutineHost != null
            ? coroutineHost.StartCoroutine(CoRefreshWhileActive())
            : null;
    }

    /// <summary>慢動作結束且確認落敗：維持全黑白直到結算關閉。</summary>
    public static void HoldFull()
    {
        if (!active) return;
        SetAmount(1f);
        RefreshTargets();
    }

    /// <summary>還原所有受影響 UI 的原始色彩。</summary>
    public static void Release()
    {
        if (!active && desaturateAmount <= 0.001f) return;
        StopRoutines();
        if (coroutineHost != null && coroutineHost.isActiveAndEnabled)
            coroutineHost.StartCoroutine(CoRelease());
        else
            ReleaseImmediate();
    }

    private static void ReleaseImmediate()
    {
        active = false;
        SetAmount(0f);
        RestoreAll();
    }

    private static IEnumerator CoRampToFull()
    {
        float start = desaturateAmount;
        float t = 0f;
        while (t < RampToFullSeconds)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutCubic(Mathf.Clamp01(t / RampToFullSeconds));
            SetAmount(Mathf.Lerp(start, 1f, p));
            RefreshTargets();
            yield return null;
        }

        SetAmount(1f);
        RefreshTargets();
        rampRoutine = null;
    }

    private static IEnumerator CoRelease()
    {
        float start = desaturateAmount;
        float t = 0f;
        while (t < ReleaseSeconds)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / ReleaseSeconds);
            SetAmount(Mathf.Lerp(start, 0f, p));
            RefreshTargets();
            yield return null;
        }

        ReleaseImmediate();
    }

    private static IEnumerator CoRefreshWhileActive()
    {
        while (active && desaturateAmount > 0.001f)
        {
            RefreshTargets();
            yield return null;
        }

        refreshRoutine = null;
    }

    private static void StopRoutines()
    {
        if (coroutineHost == null) return;
        if (rampRoutine != null)
        {
            coroutineHost.StopCoroutine(rampRoutine);
            rampRoutine = null;
        }
        if (refreshRoutine != null)
        {
            coroutineHost.StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    private static void SetAmount(float amount)
    {
        desaturateAmount = Mathf.Clamp01(amount);
        if (sharedUiGrayscaleMaterial != null)
            sharedUiGrayscaleMaterial.SetFloat(DesaturateId, desaturateAmount);
    }

    private static bool EnsureMaterial()
    {
        if (sharedUiGrayscaleMaterial != null) return true;

        Shader shader = Shader.Find(UiGrayscaleShaderName);
        if (shader == null)
        {
            Debug.LogWarning("BattleUiGrayscaleFx: shader '" + UiGrayscaleShaderName + "' not found.");
            return false;
        }

        sharedUiGrayscaleMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        sharedUiGrayscaleMaterial.SetFloat(DesaturateId, desaturateAmount);
        return true;
    }

    private static void RefreshTargets()
    {
        if (!active || desaturateAmount <= 0.001f || !EnsureMaterial()) return;

        PruneDestroyedTracks();
        CollectBattleBackgroundIfNeeded();
        CollectGraphicsUnderBattleScope();
        CollectTmpUnderBattleScope();
        ApplyAllTracks();
    }

    private static void PruneDestroyedTracks()
    {
        for (int i = graphicTracks.Count - 1; i >= 0; i--)
        {
            if (graphicTracks[i].graphic == null)
                graphicTracks.RemoveAt(i);
        }

        for (int i = tmpTracks.Count - 1; i >= 0; i--)
        {
            if (tmpTracks[i].text == null)
                tmpTracks.RemoveAt(i);
        }
    }

    private static void CollectBattleBackgroundIfNeeded()
    {
        if (battleBackgroundImage != null) return;
        battleBackgroundImage = ResolveBattleBackgroundImage();
        if (battleBackgroundImage == null || IsExcluded(battleBackgroundImage.transform)) return;
        if (ContainsGraphic(battleBackgroundImage)) return;

        graphicTracks.Add(new GraphicTrack
        {
            graphic = battleBackgroundImage,
            originalMaterial = battleBackgroundImage.material,
            hadCustomMaterial = battleBackgroundImage.material != null
        });
    }

    private static void CollectGraphicsUnderBattleScope()
    {
        Canvas canvas = ResolveBattleCanvas();
        if (canvas == null) return;

        Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g == null || g is TMP_Text) continue;
            if (IsExcluded(g.transform)) continue;
            if (ContainsGraphic(g)) continue;

            graphicTracks.Add(new GraphicTrack
            {
                graphic = g,
                originalMaterial = g.material,
                hadCustomMaterial = g.material != null
            });
        }
    }

    private static void CollectTmpUnderBattleScope()
    {
        Canvas canvas = ResolveBattleCanvas();
        if (canvas == null) return;

        TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text tmp = texts[i];
            if (tmp == null || IsExcluded(tmp.transform) || ContainsTmp(tmp)) continue;

            Material shared = tmp.fontSharedMaterial;
            Color face = shared != null ? shared.GetColor(ShaderUtilities.ID_FaceColor) : Color.white;
            Color outline = shared != null ? shared.GetColor(ShaderUtilities.ID_OutlineColor) : Color.black;
            bool hasOutline = shared != null && outline.a > 0.001f && outline.maxColorComponent > 0.001f;

            tmpTracks.Add(new TmpTrack
            {
                text = tmp,
                baseVertexColor = tmp.color,
                baseFaceColor = face,
                baseOutlineColor = outline,
                hasOutline = hasOutline
            });
        }
    }

    private static void ApplyAllTracks()
    {
        for (int i = 0; i < graphicTracks.Count; i++)
        {
            GraphicTrack track = graphicTracks[i];
            Graphic g = track.graphic;
            if (g == null || IsExcluded(g.transform)) continue;

            if (desaturateAmount <= 0.001f)
            {
                g.material = track.hadCustomMaterial ? track.originalMaterial : null;
                continue;
            }

            g.material = sharedUiGrayscaleMaterial;
        }

        for (int i = 0; i < tmpTracks.Count; i++)
        {
            TmpTrack track = tmpTracks[i];
            TMP_Text tmp = track.text;
            if (tmp == null || IsExcluded(tmp.transform)) continue;

            if (desaturateAmount <= 0.001f)
            {
                tmp.color = track.baseVertexColor;
                Material mat = tmp.fontMaterial;
                mat.SetColor(ShaderUtilities.ID_FaceColor, track.baseFaceColor);
                if (track.hasOutline)
                    mat.SetColor(ShaderUtilities.ID_OutlineColor, track.baseOutlineColor);
                continue;
            }

            tmp.color = DesaturateColor(track.baseVertexColor, desaturateAmount);
            Material fontMat = tmp.fontMaterial;
            fontMat.SetColor(ShaderUtilities.ID_FaceColor, DesaturateColor(track.baseFaceColor, desaturateAmount));
            if (track.hasOutline)
            {
                fontMat.SetColor(
                    ShaderUtilities.ID_OutlineColor,
                    DesaturateColor(track.baseOutlineColor, desaturateAmount));
            }
        }
    }

    private static void RestoreAll()
    {
        for (int i = 0; i < graphicTracks.Count; i++)
        {
            GraphicTrack track = graphicTracks[i];
            if (track.graphic == null) continue;
            track.graphic.material = track.hadCustomMaterial ? track.originalMaterial : null;
        }

        for (int i = 0; i < tmpTracks.Count; i++)
        {
            TmpTrack track = tmpTracks[i];
            if (track.text == null) continue;
            track.text.color = track.baseVertexColor;
            Material mat = track.text.fontMaterial;
            mat.SetColor(ShaderUtilities.ID_FaceColor, track.baseFaceColor);
            if (track.hasOutline)
                mat.SetColor(ShaderUtilities.ID_OutlineColor, track.baseOutlineColor);
        }

        if (battleBackgroundImage != null)
            battleBackgroundImage.material = null;

        graphicTracks.Clear();
        tmpTracks.Clear();
        battleBackgroundImage = null;
    }

    private static bool ContainsGraphic(Graphic g)
    {
        for (int i = 0; i < graphicTracks.Count; i++)
        {
            if (graphicTracks[i].graphic == g)
                return true;
        }

        return false;
    }

    private static bool ContainsTmp(TMP_Text tmp)
    {
        for (int i = 0; i < tmpTracks.Count; i++)
        {
            if (tmpTracks[i].text == tmp)
                return true;
        }

        return false;
    }

    private static Canvas ResolveBattleCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas picked = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || c.gameObject == null || !c.gameObject.scene.IsValid()) continue;
            string name = c.gameObject.name;
            if (name == "Canvas" || name == "Canvas2" || name == "Canva2")
            {
                picked = c;
                break;
            }
        }

        if (picked != null) return picked;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c != null && c.gameObject != null && c.gameObject.scene.IsValid())
                return c;
        }

        return null;
    }

    private static Image ResolveBattleBackgroundImage()
    {
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Transform[] all = roots[r].GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null ||
                    !string.Equals(t.name, HarborTrainingBattleBackground.BattleBackgroundObjectName,
                        System.StringComparison.Ordinal))
                    continue;

                Image img = t.GetComponent<Image>();
                if (img != null)
                    return img;
            }
        }

        return null;
    }

    /// <summary>結算面板及其子節點維持原色。</summary>
    private static bool IsExcluded(Transform t)
    {
        while (t != null)
        {
            string name = t.name;
            if (name == "EndBattlePanel" || name == "TutorialSettlementPanel")
                return true;
            if (name == "Panel" && t.parent != null && t.parent.name == "M12SettlementOverlay")
                return true;
            t = t.parent;
        }

        return false;
    }

    private static Color DesaturateColor(Color c, float amount)
    {
        float luma = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
        return Color.Lerp(c, new Color(luma, luma, luma, c.a), amount);
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
}
