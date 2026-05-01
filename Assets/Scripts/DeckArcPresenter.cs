using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DeckArcPresenter : MonoBehaviour
{
    private const float SettledPosEpsilon = 0.035f;
    private const float ScrollSettledEpsilon = 0.002f;

    private int _lastAppliedFrame = -1;
    private int _completedHeavyApplyFrame = -1;
    private int _cachedGeomSignature = int.MinValue;
    private int _lastFullApplyFullSig = int.MinValue;
    private float _lastFullApplyScrollY = float.NaN;

    private readonly List<float> _layoutBaseXCache = new List<float>();
    private readonly List<float> _layoutBaseYCache = new List<float>();
    private readonly Dictionary<int, float> _lastSmoothedArcXByChildId = new Dictionary<int, float>();
    private readonly HashSet<int> _seenIds = new HashSet<int>();

    /// <summary>
    /// Call after deck list structure changes (e.g. card removed) so the next <see cref="ApplyLayout"/>
    /// cannot skip work via cached geometry / "already settled" shortcuts.
    /// </summary>
    public void InvalidateLayoutState()
    {
        _cachedGeomSignature = int.MinValue;
        _lastFullApplyFullSig = int.MinValue;
        _lastFullApplyScrollY = float.NaN;
        _lastAppliedFrame = -1;
        _completedHeavyApplyFrame = -1;
        _layoutBaseXCache.Clear();
        _layoutBaseYCache.Clear();
    }

    public void ApplyLayout(
        RectTransform content,
        bool enableArc,
        int visibleSlotCount,
        float circleRadiusPx,
        float chordHalfWidthPx,
        float arcShapeStrength,
        float visibleRangePaddingSlots,
        float fallbackCellHeight,
        float fallbackSpacingY,
        List<float> slotXOffsetById,
        float horizontalSmoothTime,
        float horizontalMaxSpeed,
        bool oncePerFrame)
    {
        if (content == null) return;
        if (oncePerFrame && _lastAppliedFrame == Time.frameCount) return;
        if (oncePerFrame) _lastAppliedFrame = Time.frameCount;

        if (!enableArc) return;
        int n = content.childCount;
        if (n <= 0) return;

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        float cellH = (grid != null && grid.cellSize.y > 1f) ? grid.cellSize.y : fallbackCellHeight;
        float spacingY = grid != null ? grid.spacing.y : fallbackSpacingY;
        float stepY = cellH + spacingY;
        float scrollY = content.anchoredPosition.y;
        float firstVisibleIndex = stepY > 0.01f ? scrollY / stepY : 0f;

        int normalizedSlotCount = Mathf.Max(3, visibleSlotCount);
        if ((normalizedSlotCount & 1) == 0) normalizedSlotCount += 1;
        int centerSlotId = (normalizedSlotCount + 1) / 2;
        int maxSlotDistance = Mathf.Max(1, centerSlotId - 1);

        float L = Mathf.Max(1f, chordHalfWidthPx);
        float R = Mathf.Max(circleRadiusPx, L + 0.5f);
        float shapeStrength = Mathf.Clamp(arcShapeStrength, 0.05f, 1.5f);
        float rangePaddingSlots = Mathf.Max(0f, visibleRangePaddingSlots);
        int geomSig = ComputeGeomSignature(content, n, cellH, spacingY);
        int arcSig = ComputeArcSignature(
            normalizedSlotCount,
            R,
            L,
            shapeStrength,
            rangePaddingSlots,
            slotXOffsetById,
            horizontalSmoothTime,
            horizontalMaxSpeed);
        int fullSig = geomSig ^ arcSig;

        bool needGridRebuild = geomSig != _cachedGeomSignature || _layoutBaseXCache.Count != n;

        if (!needGridRebuild
            && fullSig == _lastFullApplyFullSig
            && Mathf.Abs(scrollY - _lastFullApplyScrollY) < ScrollSettledEpsilon
            && TryAllChildrenSettled(
                content,
                n,
                firstVisibleIndex,
                normalizedSlotCount,
                centerSlotId,
                maxSlotDistance,
                R,
                L,
                shapeStrength,
                rangePaddingSlots,
                slotXOffsetById))
            return;

        if (_completedHeavyApplyFrame == Time.frameCount)
            return;

        if (needGridRebuild)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            _cachedGeomSignature = geomSig;
            EnsureBasePositionCacheSize(n);
            for (int i = 0; i < n; i++)
            {
                RectTransform ch = content.GetChild(i) as RectTransform;
                _layoutBaseXCache[i] = ch != null ? ch.anchoredPosition.x : 0f;
                _layoutBaseYCache[i] = ch != null ? ch.anchoredPosition.y : 0f;
            }
        }

        _seenIds.Clear();
        for (int i = 0; i < n; i++)
        {
            RectTransform child = content.GetChild(i) as RectTransform;
            if (child == null) continue;

            float layoutBaseX = _layoutBaseXCache[i];
            float layoutBaseY = _layoutBaseYCache[i];

            float slotPosition = (i - firstVisibleIndex) + 1f;
            ComputeTrueArcOffsetForSlot(
                slotPosition,
                centerSlotId,
                maxSlotDistance,
                R,
                L,
                shapeStrength,
                rangePaddingSlots,
                slotXOffsetById,
                normalizedSlotCount,
                out float xOffset,
                out float yOffset);

            float targetX = layoutBaseX + xOffset;
            float targetY = layoutBaseY + yOffset;
            Vector2 pos = child.anchoredPosition;
            int cid = child.gameObject.GetInstanceID();
            _seenIds.Add(cid);

            pos.x = targetX;
            pos.y = targetY;
            _lastSmoothedArcXByChildId[cid] = targetX;

            child.anchoredPosition = pos;
            child.localRotation = Quaternion.identity;
        }

        PruneStaleState();

        _lastFullApplyFullSig = fullSig;
        _lastFullApplyScrollY = scrollY;
        _completedHeavyApplyFrame = Time.frameCount;
    }

    private void EnsureBasePositionCacheSize(int n)
    {
        if (_layoutBaseXCache.Count < n)
        {
            while (_layoutBaseXCache.Count < n)
                _layoutBaseXCache.Add(0f);
        }
        else if (_layoutBaseXCache.Count > n)
            _layoutBaseXCache.RemoveRange(n, _layoutBaseXCache.Count - n);

        if (_layoutBaseYCache.Count < n)
        {
            while (_layoutBaseYCache.Count < n)
                _layoutBaseYCache.Add(0f);
        }
        else if (_layoutBaseYCache.Count > n)
            _layoutBaseYCache.RemoveRange(n, _layoutBaseYCache.Count - n);
    }

    private static int ComputeGeomSignature(RectTransform content, int n, float cellH, float spacingY)
    {
        unchecked
        {
            int w = Mathf.RoundToInt(content.rect.width * 100f);
            int h = Mathf.RoundToInt(content.rect.height * 100f);
            int ch = Mathf.RoundToInt(cellH * 100f);
            int sp = Mathf.RoundToInt(spacingY * 100f);
            return n ^ (w << 1) ^ (h << 2) ^ (ch << 3) ^ (sp << 4);
        }
    }

    private static int ComputeArcSignature(
        int normalizedSlotCount,
        float circleRadiusPx,
        float chordHalfWidthPx,
        float arcShapeStrength,
        float visibleRangePaddingSlots,
        List<float> slotXOffsetById,
        float horizontalSmoothTime,
        float horizontalMaxSpeed)
    {
        unchecked
        {
            int s = normalizedSlotCount
                ^ (Mathf.RoundToInt(circleRadiusPx * 2f) << 11)
                ^ (Mathf.RoundToInt(chordHalfWidthPx * 4f) << 13)
                ^ (Mathf.RoundToInt(arcShapeStrength * 200f) << 10)
                ^ (Mathf.RoundToInt(visibleRangePaddingSlots * 100f) << 9)
                ^ (Mathf.RoundToInt(horizontalSmoothTime * 1000f) << 16)
                ^ (Mathf.RoundToInt(horizontalMaxSpeed * 0.05f) << 20);
            if (slotXOffsetById != null)
            {
                int c = slotXOffsetById.Count;
                s ^= c << 24;
                for (int i = 0; i < c && i < 16; i++)
                    s ^= Mathf.RoundToInt(slotXOffsetById[i] * 50f) << (i & 7);
            }

            return s;
        }
    }

    private bool TryAllChildrenSettled(
        RectTransform content,
        int n,
        float firstVisibleIndex,
        int normalizedSlotCount,
        int centerSlotId,
        int maxSlotDistance,
        float circleRadiusPx,
        float chordHalfWidthPx,
        float arcShapeStrength,
        float visibleRangePaddingSlots,
        List<float> slotXOffsetById)
    {
        if (_layoutBaseXCache.Count != n)
            return false;

        float R = Mathf.Max(circleRadiusPx, chordHalfWidthPx + 0.5f);
        float L = Mathf.Max(1f, chordHalfWidthPx);

        for (int i = 0; i < n; i++)
        {
            RectTransform child = content.GetChild(i) as RectTransform;
            if (child == null) return false;

            float layoutBaseX = _layoutBaseXCache[i];
            float slotPosition = (i - firstVisibleIndex) + 1f;
            ComputeTrueArcOffsetForSlot(
                slotPosition,
                centerSlotId,
                maxSlotDistance,
                R,
                L,
                arcShapeStrength,
                visibleRangePaddingSlots,
                slotXOffsetById,
                normalizedSlotCount,
                out float xOffset,
                out float yOffset);

            float targetX = layoutBaseX + xOffset;
            float targetY = _layoutBaseYCache[i] + yOffset;
            float curX = child.anchoredPosition.x;
            float curY = child.anchoredPosition.y;
            if (Mathf.Abs(curX - targetX) > SettledPosEpsilon)
                return false;
            if (Mathf.Abs(curY - targetY) > SettledPosEpsilon)
                return false;
        }

        return true;
    }

    private void PruneStaleState()
    {
        if (_lastSmoothedArcXByChildId.Count <= _seenIds.Count) return;
        List<int> stale = null;
        foreach (int key in _lastSmoothedArcXByChildId.Keys)
        {
            if (_seenIds.Contains(key)) continue;
            if (stale == null) stale = new List<int>();
            stale.Add(key);
        }
        if (stale == null) return;
        for (int i = 0; i < stale.Count; i++)
            _lastSmoothedArcXByChildId.Remove(stale[i]);
    }

    /// <summary>
    /// True circular arc in UI space: chord vertical span ±L, radius R, bulge toward -X.
    /// Offsets are relative to straight grid baseline; custom X is lerped by continuous slot id.
    /// </summary>
    private static void ComputeTrueArcOffsetForSlot(
        float slotPosition,
        int centerSlotId,
        int maxSlotDistance,
        float circleRadiusPx,
        float chordHalfWidthPx,
        float arcShapeStrength,
        float visibleRangePaddingSlots,
        List<float> slotXOffsetById,
        int normalizedSlotCount,
        out float xOffset,
        out float yOffset)
    {
        xOffset = 0f;
        yOffset = 0f;

        float L = Mathf.Max(1f, chordHalfWidthPx);
        float R = Mathf.Max(circleRadiusPx, L + 0.5f);
        float u = maxSlotDistance > 0 ? (slotPosition - centerSlotId) / maxSlotDistance : 0f;
        float uClamped = Mathf.Clamp(u, -1f, 1f);

        float h = Mathf.Sqrt(Mathf.Max(0f, R * R - L * L));
        float gamma = Mathf.Asin(Mathf.Clamp(L / R, -1f, 1f));
        float theta = uClamped * gamma;
        float xArc = h - R * Mathf.Cos(theta);
        float yArc = R * Mathf.Sin(theta);

        float shape = Mathf.Clamp(arcShapeStrength, 0.05f, 1.5f);
        float edgeFade = ComputeEdgeFade(slotPosition, normalizedSlotCount, visibleRangePaddingSlots);
        float custom = SampleSlotXOffsetContinuous(slotPosition, slotXOffsetById, normalizedSlotCount);

        xOffset = (xArc * shape + custom) * edgeFade;
        yOffset = (yArc - uClamped * L) * shape * edgeFade;
    }

    private static float SampleSlotXOffsetContinuous(
        float slotIdFloat,
        List<float> slotXOffsetById,
        int normalizedSlotCount)
    {
        if (slotXOffsetById == null || slotXOffsetById.Count == 0)
            return 0f;

        float sid = Mathf.Clamp(slotIdFloat, 1f, normalizedSlotCount);
        float idx = sid - 1f;
        int last = slotXOffsetById.Count - 1;
        if (idx <= 0f)
            return slotXOffsetById[0];
        if (idx >= last)
            return slotXOffsetById[last];

        int i0 = Mathf.Clamp(Mathf.FloorToInt(idx), 0, last - 1);
        float t = idx - i0;
        return Mathf.Lerp(slotXOffsetById[i0], slotXOffsetById[i0 + 1], t);
    }

    private static float ComputeEdgeFade(float slotPosition, int normalizedSlotCount, float visibleRangePaddingSlots)
    {
        if (normalizedSlotCount <= 0)
            return 0f;
        const float blendWidth = 0.45f;
        float paddedStart = 1f - visibleRangePaddingSlots;
        float paddedEnd = normalizedSlotCount + visibleRangePaddingSlots;
        float left = SmoothStep01((slotPosition - (paddedStart - blendWidth)) / blendWidth);
        float right = SmoothStep01(((paddedEnd + blendWidth) - slotPosition) / blendWidth);
        return left * right;
    }

    private static float SmoothStep01(float x)
    {
        float t = Mathf.Clamp01(x);
        return t * t * (3f - 2f * t);
    }
}
