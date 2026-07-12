using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BackpackCardInspectPanel : MonoBehaviour
{
    // --- layout / card binding ---

    private void ApplyCard(Card card)
    {
        if (card == null) return;

        TMP_FontAsset font = host.BackpackInspectResolveFont();
        if (font == null) font = UiFontResolver.ResolveUiFont();
        ApplyTypographyFonts(font);
        ApplyTypographySpacing();

        string title = string.IsNullOrWhiteSpace(card.cardName) ? "未命名卡牌" : card.cardName.Trim();
        if (titleTmp != null) titleTmp.text = title;

        if (subtitleTmp != null)
        {
            string en = card.cardNameEnglish != null ? card.cardNameEnglish.Trim() : string.Empty;
            subtitleTmp.text = string.IsNullOrEmpty(en) ? string.Empty : en;
            subtitleTmp.gameObject.SetActive(!string.IsNullOrEmpty(en));
        }

        if (typeTmp != null)
            typeTmp.text = BuildTypeRarityLine(card);

        if (deckBarTmp != null)
            deckBarTmp.text = host.BackpackInspectDeckInclusionText(card.id);

        ApplyMasteryBar(card);

        if (card is MonsterCard monster)
            previewStage = CardSkillProficiencyService.GetUnlockedStage(monster.id);
        else
            previewStage = CardSkillRevealStage.FullC;

        ApplyStatChips(card);
        if (skillTmp != null) skillTmp.text = BuildSkillRich(card);

        InvalidateSkillLayoutCache();
        RefreshStageTabVisuals();
        ApplyArt(card);
        StartCoroutine(CoRefreshInfoScrollLayout());
    }

    private void ApplyMasteryBar(Card card)
    {
        ProficiencyBarViewModel bar = CardSkillProficiencyService.GetProficiencyBarForCard(card);
        bool show = bar.show && masteryBarRt != null;

        if (masteryBarRt != null)
            masteryBarRt.gameObject.SetActive(show);
        if (!show) return;

        if (masteryLabelTmp != null)
        {
            masteryLabelTmp.text = bar.label;
            masteryLabelTmp.ForceMeshUpdate();
        }

        if (masteryStatusTmp != null)
        {
            masteryStatusTmp.text = bar.statusText;
            masteryStatusTmp.ForceMeshUpdate();
        }

        CardProficiencyDebugReset.ApplyBackpackMasteryFill(masteryFillRt, bar.fill01);
    }

    /// <summary>Debug 清空熟練度後刷新目前詳情列。</summary>
    public void RefreshMasteryBarIfOpen()
    {
        if (!IsOpen || currentCard == null) return;
        ApplyMasteryBar(currentCard);
        RefreshStageTabVisuals();
        if (skillTmp != null)
        {
            skillTmp.text = BuildSkillRich(currentCard);
            InvalidateSkillLayoutCache();
        }
        StartCoroutine(CoRefreshInfoScrollLayout());
    }

    private void RefreshStageTabVisuals()
    {
        CardSkillRevealStage[] stages =
        {
            CardSkillRevealStage.LockedA,
            CardSkillRevealStage.BasicB,
            CardSkillRevealStage.FullC
        };
        string[] labels = { "A 階段", "B 階段", "C 階段" };

        for (int i = 0; i < stageTabBgImages.Length; i++)
        {
            if (stageTabBgImages[i] == null) continue;
            bool selected = previewStage == stages[i];
            stageTabBgImages[i].color = selected
                ? BackpackInspectUiColors.TabSelectedBg
                : BackpackInspectUiColors.TabIdleBg;
            if (stageTabLabelTmps[i] != null)
            {
                stageTabLabelTmps[i].text = labels[i];
                stageTabLabelTmps[i].color = selected
                    ? BackpackInspectUiColors.TabSelectedText
                    : BackpackInspectUiColors.TabIdleText;
            }
        }
    }

    private void InvalidateSkillLayoutCache() => skillLayoutCache.valid = false;

    private IEnumerator CoRefreshInfoScrollLayout()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (infoContentRt == null) yield break;

        float fullW = infoContentRt.rect.width;
        if (fullW <= 8f)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            fullW = infoContentRt.rect.width > 8f ? infoContentRt.rect.width : 520f;
        }

        ApplyInfoScrollLayoutPass(fullW);
    }

    private void ApplyInfoScrollLayoutPass(float fullW)
    {
        if (fullW <= 8f) fullW = 520f;

        float padH = BackpackInspectVisualStyle.Typography.InfoPaddingH;
        float gap = BackpackInspectVisualStyle.Typography.BlockGap;
        float padTop = BackpackInspectVisualStyle.Typography.InfoPaddingTop;
        float padBottom = 28f;

        float y = padTop;

        float headerLeftW = fullW * HeaderLeftAnchorMax;
        float headerLeftH = LayoutInfoColumn(0f, padH, gap, headerLeftW, titleTmp, subtitleTmp, typeTmp);
        headerLeftH = Mathf.Max(headerLeftH, StageTabRowHeight);
        PlaceColumnBand(headerLeftRt, y, headerLeftH);
        PlaceColumnBand(headerRightRt, y, StageTabRowHeight);

        float headerH = Mathf.Max(headerLeftH, StageTabRowHeight);
        y += headerH + gap;

        PlaceBand(deckBarRt, y, DeckBarHeight);
        y += DeckBarHeight + gap;

        PlaceBand(statStripRt, y, StatStripHeight);
        y += StatStripHeight + gap;

        if (masteryBarRt != null && masteryBarRt.gameObject.activeSelf)
        {
            PlaceBand(masteryBarRt, y, MasteryBarHeight);
            y += MasteryBarHeight + gap;
        }

        float skillH = LayoutSkillBlock(fullW, padH, y, padBottom);
        PlaceBand(skillSectionRt, y, skillH);
        y += skillH + padBottom;
        infoContentRt.sizeDelta = new Vector2(0f, y);

        if (infoScroll != null)
            infoScroll.verticalNormalizedPosition = 1f;
        RefreshScrollHint();
    }

    private float LayoutSkillBlock(float columnWidth, float padH, float contentYOffset, float contentPadBottom)
    {
        if (skillTmp == null) return 0f;

        if (TryApplySkillLayoutCache(columnWidth, padH))
            return skillLayoutCache.sectionH;

        float innerW = Mathf.Max(80f, columnWidth - padH * 2f);
        float preferredH = MeasureAndLayoutSkillText(innerW, padH);
        float contentH = preferredH + SkillScrollPadV * 2f;
        float maxScrollH = ResolveSkillScrollMaxHeight(contentYOffset, contentPadBottom);
        skillScrollActive = contentH > maxScrollH + 1f;
        float sectionH = skillScrollActive ? maxScrollH : contentH;

        if (skillScrollContentRt != null)
        {
            skillScrollContentRt.sizeDelta = new Vector2(0f, contentH);
            LayoutRebuilder.ForceRebuildLayoutImmediate(skillScrollContentRt);
        }

        if (skillScroll != null)
        {
            skillScroll.vertical = skillScrollActive;
            skillScroll.enabled = true;
            skillScroll.StopMovement();
            skillScroll.velocity = Vector2.zero;
            skillScroll.verticalNormalizedPosition = 1f;
        }

        StoreSkillLayoutCache(columnWidth, preferredH, contentH, sectionH);
        return sectionH;
    }

    private bool TryApplySkillLayoutCache(float columnWidth, float padH)
    {
        if (!skillLayoutCache.valid || currentCard == null) return false;
        if (skillLayoutCache.cardId != currentCard.id || skillLayoutCache.stage != previewStage) return false;
        if (Mathf.Abs(skillLayoutCache.columnWidth - columnWidth) > 1f) return false;

        skillScrollActive = skillLayoutCache.scrollActive;
        ApplySkillTextRect(padH, skillLayoutCache.preferredH);
        if (skillScrollContentRt != null)
            skillScrollContentRt.sizeDelta = new Vector2(0f, skillLayoutCache.contentH);
        if (skillScroll != null)
        {
            skillScroll.vertical = skillScrollActive;
            skillScroll.verticalNormalizedPosition = 1f;
        }
        return true;
    }

    private void StoreSkillLayoutCache(float columnWidth, float preferredH, float contentH, float sectionH)
    {
        if (currentCard == null) return;
        skillLayoutCache = new SkillLayoutCache
        {
            cardId = currentCard.id,
            stage = previewStage,
            columnWidth = columnWidth,
            preferredH = preferredH,
            contentH = contentH,
            sectionH = sectionH,
            scrollActive = skillScrollActive,
            valid = true
        };
    }

    private void ApplySkillTextRect(float padH, float preferredH)
    {
        if (skillTmp == null) return;
        RectTransform rt = skillTmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -SkillScrollPadV);
        rt.sizeDelta = new Vector2(-padH * 2f, preferredH);
    }

    private float MeasureAndLayoutSkillText(float innerW, float padH)
    {
        skillTmp.alignment = TextAlignmentOptions.TopLeft;
        skillTmp.ForceMeshUpdate(true);

        float preferredH = skillTmp.GetPreferredValues(skillTmp.text, innerW, 0f).y;
        if (skillTmp.textBounds.size.y > 1f)
            preferredH = Mathf.Max(preferredH, skillTmp.textBounds.size.y);
        preferredH = Mathf.Max(34f, preferredH + 8f);

        ApplySkillTextRect(padH, preferredH);
        skillTmp.ForceMeshUpdate(true);
        return preferredH;
    }

    private float ResolveSkillScrollMaxHeight(float contentYOffset, float contentPadBottom)
    {
        float viewportH = SkillScrollMaxHeightCap;
        if (infoScroll != null && infoScroll.viewport != null)
        {
            float measured = infoScroll.viewport.rect.height;
            if (measured > 80f)
                viewportH = measured;
        }

        float remaining = viewportH - contentYOffset - contentPadBottom;
        float fromRemaining = remaining > SkillScrollMinMaxHeight
            ? remaining
            : Mathf.Max(80f, remaining);
        return Mathf.Clamp(
            Mathf.Min(SkillScrollVisibleMax, fromRemaining),
            SkillScrollMinMaxHeight,
            SkillScrollMaxHeightCap);
    }

    private void RefreshScrollHint()
    {
        if (hintTmp == null) return;
        if (skillScrollActive)
            hintTmp.text = "戰技說明可上下滑動 · 左右滑動切換卡牌";
        else
            hintTmp.text = "上下滑動閱讀詳情 · 左右滑動切換卡牌";
    }

    private static void PlaceColumnBand(RectTransform col, float yTop, float height)
    {
        if (col == null) return;
        col.anchoredPosition = new Vector2(0f, -yTop);
        col.sizeDelta = new Vector2(0f, height);
    }

    private static void PlaceBand(RectTransform band, float yTop, float height)
    {
        if (band == null) return;
        band.anchorMin = new Vector2(0f, 1f);
        band.anchorMax = new Vector2(1f, 1f);
        band.pivot = new Vector2(0.5f, 1f);
        band.anchoredPosition = new Vector2(0f, -yTop);
        band.sizeDelta = new Vector2(0f, height);
    }

    private static float LayoutInfoColumn(
        float yStart,
        float padH,
        float gap,
        float columnWidth,
        params TextMeshProUGUI[] blocks)
    {
        float y = yStart;
        if (blocks == null) return y;

        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] == null || !blocks[i].gameObject.activeInHierarchy) continue;
            y = PlaceTextBlockInColumn(blocks[i], y, columnWidth, padH) + gap;
        }

        return y > yStart ? y - gap : yStart;
    }

    private static float PlaceTextBlockInColumn(TextMeshProUGUI tmp, float yTop, float columnWidth, float padH)
    {
        if (tmp == null) return yTop;
        float innerW = Mathf.Max(80f, columnWidth - padH * 2f);
        float height = Mathf.Max(34f, tmp.GetPreferredValues(tmp.text, innerW, 0f).y + 6f);
        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -yTop);
        rt.sizeDelta = new Vector2(-padH * 2f, height);
        return yTop + height;
    }

    private void ApplyArt(Card card)
    {
        if (artImage == null) return;

        Sprite sprite = ResolveSprite(card, sourceDisplay);
        bool has = sprite != null;
        artImage.sprite = sprite;
        artImage.enabled = has;
        artImage.color = has ? Color.white : BackpackInspectVisualStyle.FrameInner;
        CardDisplay.SyncCardArtRarityOverlay(artImage, has ? card : null);
    }

    private static string BuildTypeRarityLine(Card card)
    {
        if (card is MonsterCard monster)
            return $"怪物牌 · {CombatRoleUtility.GetDisplayName(monster.combatRole)}  {card.rarity}";
        string type = card is SpellCard ? "法術牌" : "卡牌";
        return $"{type}  {card.rarity}";
    }

    private void ApplyStatChips(Card card)
    {
        if (statChipTmps[0] == null) return;

        string atk;
        string hp;
        if (card is MonsterCard m)
        {
            atk = m.attack.ToString();
            hp = m.healthPointMax.ToString();
        }
        else
        {
            atk = "無";
            hp = "無";
        }

        int owned = host.BackpackInspectCollectionCount(card.id);
        string rarity = card.rarity.ToString();
        string[] labels = { "攻擊力", "生命值", "持有數", "稀有度" };
        string[] values = { atk, hp, owned.ToString(), rarity };

        for (int i = 0; i < statChipTmps.Length; i++)
        {
            if (statChipTmps[i] == null) continue;
            statChipTmps[i].text = labels[i] + " " + values[i];
        }
    }

    private void ApplyTypographySpacing()
    {
        if (subtitleTmp != null)
            subtitleTmp.lineSpacing = BackpackInspectVisualStyle.Typography.SubtitleLineSpacing;
        if (skillTmp != null)
        {
            skillTmp.lineSpacing = BackpackInspectVisualStyle.Typography.BodyLineSpacing;
            skillTmp.paragraphSpacing = BackpackInspectVisualStyle.Typography.BodyParagraphSpacing;
        }
    }

    private string BuildSkillRich(Card card)
    {
        var sb = new StringBuilder(512);

        if (card is SpellCard spell)
        {
            sb.Append(BackpackInspectVisualStyle.WrapSubtitleRich("法術效果"));
            string effect = string.IsNullOrWhiteSpace(spell.effect) ? "此法術暫無效果描述。" : spell.effect.Trim();
            sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectVisualStyle.Typography.BodyOnSkill));
            sb.Append(effect);
            sb.Append("</color>");
            return sb.ToString();
        }

        if (card is MonsterCard monster)
        {
            if (!MonsterSkillRegistry.HasSkillTrack(monster.id))
            {
                sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectVisualStyle.Typography.BodyOnSkill));
                sb.Append("此卡尚無戰技說明");
                sb.Append("</color>");
                return sb.ToString();
            }

            if (MonsterSkillRegistry.TryGetSkillName(monster.id, out string skillName))
            {
                sb.Append(BackpackInspectVisualStyle.WrapSubtitleRich(skillName));
                sb.Append('\n');
            }

            CardSkillRevealStage unlocked = CardSkillProficiencyService.GetUnlockedStage(monster.id);
            AppendStageRich(sb, monster.id, previewStage, unlocked);
            return sb.ToString().TrimEnd();
        }

        return string.Empty;
    }

    private static void AppendStageRich(
        StringBuilder sb,
        int monsterId,
        CardSkillRevealStage stage,
        CardSkillRevealStage unlocked)
    {
        string title = BackpackInspectVisualStyle.StageTitleZh(stage);
        bool open = (int)unlocked >= (int)stage;
        Color accent = BackpackInspectVisualStyle.StageAccent(stage);

        sb.Append(BackpackInspectVisualStyle.ColorTag(open ? accent : BackpackInspectVisualStyle.InkDim));
        sb.Append(title);
        sb.Append(open ? "  已解放" : "  未解放");
        sb.Append("</color>\n");

        if (!open)
        {
            sb.Append(MonsterSkillRegistry.GetLockedStagePlaceholder(stage));
        }
        else if (MonsterSkillRegistry.TryGetSkillStageBodyRich(monsterId, stage, out string rich))
        {
            sb.Append(rich);
        }
        else
        {
            sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectVisualStyle.Typography.BodyMuted));
            sb.Append("尚無此階段文案");
            sb.Append("</color>");
        }
    }

    private static Sprite ResolveSprite(Card card, CardDisplay display)
    {
        if (card == null) return null;

        if (display != null)
        {
            if (display.backgroundImage != null && display.backgroundImage.sprite != null)
                return display.backgroundImage.sprite;
            Sprite s = display.card?.ResolveCardArtSprite();
            if (s != null) return s;
            s = display.card?.ResolveDeckThumbSprite();
            if (s != null) return s;
        }

        Sprite art = card.ResolveCardArtSprite();
        if (art != null) return art;
        art = card.ResolveDeckThumbSprite();
        if (art != null) return art;

        CardArtLibrary library = CardArtLibrary.Instance;
        if (library != null)
        {
            art = library.GetArtwork(card.id);
            if (art != null) return art;
            art = library.GetDeckThumb(card.id);
            if (art != null) return art;
        }

        return null;
    }

    private void RefreshPageHint()
    {
        bool many = cardIds.Count > 1;
        if (pageTmp != null)
            pageTmp.text = many && currentIndex >= 0 ? $"{currentIndex + 1} / {cardIds.Count}" : string.Empty;
        if (hintTmp != null)
            RefreshScrollHint();
    }

    private void RebuildCardList()
    {
        cardIds.Clear();
        host.BackpackInspectFillCollectionIds(cardIds);
        cardIds.Sort();
    }

    private Card ResolveCard(int id)
    {
        Card c = host.BackpackInspectGetCard(id);
        if (c != null) return c;
        if (sourceDisplay != null && sourceDisplay.card != null && sourceDisplay.card.id == id)
            return sourceDisplay.card;
        return currentCard != null && currentCard.id == id ? currentCard : null;
    }

    private void ApplyTypographyFonts(TMP_FontAsset font)
    {
        ApplyFont(titleTmp, font);
        ApplyFont(subtitleTmp, font);
        ApplyFont(typeTmp, font);
        ApplyFont(deckBarTmp, font);
        ApplyFont(masteryLabelTmp, font);
        ApplyFont(masteryStatusTmp, font);
        for (int i = 0; i < statChipTmps.Length; i++)
            ApplyFont(statChipTmps[i], font);
        ApplyFont(skillTmp, font);
        ApplyFont(pageTmp, font);
        ApplyFont(hintTmp, font);
        for (int i = 0; i < stageTabLabelTmps.Length; i++)
            ApplyFont(stageTabLabelTmps[i], font);
    }

    private static void ApplyFont(TextMeshProUGUI tmp, TMP_FontAsset font)
    {
        if (tmp == null || font == null) return;
        tmp.font = font;
        tmp.outlineWidth = 0f;
    }
}
