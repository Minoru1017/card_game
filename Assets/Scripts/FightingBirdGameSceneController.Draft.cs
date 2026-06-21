using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class FightingBirdGameSceneController
{
    // ----------------------------------------------------------------- 加成抽選（roguelike 分支）

    private void BeginBonusDraft()
    {
        if (resultOverlayRoot != null)
            resultOverlayRoot.SetActive(false);

        chosenBonuses.Clear();
        selectedEnhancedBonus = BirdDuelBonusId.None;
        pendingEnemyBuff = BirdDuelBonusId.None;

        switch (lastResult)
        {
            case BirdDuelResult.Win:
            {
                System.Collections.Generic.List<BirdDuelBonusId> opts;
                if (PreBattleCdContext.ShouldUseCdDraftPool(lastResult))
                {
                    var whitelist = BirdDuelCdCatalog.ResolveWinDraftBonusIds(PreBattleCdContext.SelectedCdId);
                    opts = BirdDuelBonusCatalog.DrawDistinctFromIds(whitelist, 3);
                    if (opts.Count == 0)
                        opts = BirdDuelBonusCatalog.DrawDistinct(BirdDuelBonusPool.Enhanced, 3);
                }
                else
                    opts = BirdDuelBonusCatalog.DrawDistinct(BirdDuelBonusPool.Enhanced, 3);

                string cdName = BirdDuelCdCatalog.Get(PreBattleCdContext.SelectedCdId)?.DisplayName;
                string subtitle = PreBattleCdContext.ShouldUseCdDraftPool(lastResult) && !string.IsNullOrWhiteSpace(cdName)
                    ? "CD「" + cdName + "」偏向加成池"
                    : "選擇一項強化加成";
                ShowDraftChoices("鬥鳥勝利", subtitle, opts, pick =>
                {
                    selectedEnhancedBonus = pick;
                    chosenBonuses.Add(pick);
                    if (PreBattleDuelContext.HasHiddenTier)
                        ShowBossChoice();
                    else
                        FinishDraft(false);
                });
                break;
            }
            case BirdDuelResult.Draw:
            {
                var opts = BirdDuelBonusCatalog.DrawDistinct(BirdDuelBonusPool.Basic, 3);
                ShowDraftChoices("平手", "選擇一項基礎加成", opts, pick =>
                {
                    chosenBonuses.Add(pick);
                    FinishDraft(false);
                });
                break;
            }
            default:
            {
                // 敗北：保底 1 個基礎加成 ＋ 敵方小強化（風險模型 B）。
                BirdDuelBonusId basic = BirdDuelBonusCatalog.DrawOne(BirdDuelBonusPool.Basic);
                pendingEnemyBuff = BirdDuelBonusCatalog.DrawOne(BirdDuelBonusPool.EnemyBuff);
                if (basic != BirdDuelBonusId.None) chosenBonuses.Add(basic);
                ShowLossConfirm(basic, pendingEnemyBuff);
                break;
            }
        }
    }

    private void ShowBossChoice()
    {
        GameObject panel = BuildDraftPanel("鬥鳥全勝", "是否挑戰魔王級？高風險高報酬，額外獲得 1 個稀有加成。");

        Button bossBtn = CreateDraftButton(panel.transform, "BossBtn",
            "挑戰魔王級", "額外稀有加成＋難度升為魔王級", ColorPeck, new Vector2(-260f, DraftButtonBottomOffset));
        bossBtn.onClick.AddListener(() =>
        {
            BirdDuelBonusId rare = BirdDuelBonusCatalog.DrawOne(BirdDuelBonusPool.Rare);
            if (rare != BirdDuelBonusId.None) chosenBonuses.Add(rare);
            FinishDraft(true);
        });

        Button safeBtn = CreateDraftButton(panel.transform, "SafeBtn",
            "打所選難度", "帶著加成穩穩開打", ColorScoreFill, new Vector2(260f, DraftButtonBottomOffset));
        safeBtn.onClick.AddListener(() => FinishDraft(false));
    }

    private void ShowLossConfirm(BirdDuelBonusId basic, BirdDuelBonusId enemyBuff)
    {
        string body = "保底加成：" + DescribeBonus(basic) + "\n敵方強化：" + DescribeBonus(enemyBuff);
        GameObject panel = BuildDraftPanel("再接再厲", body);

        Button goBtn = CreateDraftButton(panel.transform, "GoBtn",
            "進入對戰", "帶著保底加成開打", ColorScoreFill, new Vector2(0f, DraftButtonBottomOffset));
        goBtn.onClick.AddListener(() => FinishDraft(false));
    }

    private void ShowDraftChoices(
        string title, string subtitle,
        System.Collections.Generic.List<BirdDuelBonusId> options,
        System.Action<BirdDuelBonusId> onPick)
    {
        GameObject panel = BuildDraftPanel(title, subtitle);

        int n = options.Count;
        const float cardW = 320f;
        const float gap = 36f;
        float totalW = n * cardW + (n - 1) * gap;
        float startX = -totalW * 0.5f + cardW * 0.5f;
        Color[] tints = { ColorWing, ColorNest, ColorScoreFill };

        for (int i = 0; i < n; i++)
        {
            BirdDuelBonusId id = options[i];
            BirdDuelBonusInfo info = BirdDuelBonusCatalog.Get(id);
            float x = startX + i * (cardW + gap);
            Button btn = CreateDraftButton(panel.transform, "Opt_" + id,
                info.DisplayName, info.Description, tints[i % tints.Length], new Vector2(x, DraftButtonBottomOffset));
            BirdDuelBonusId captured = id;
            btn.onClick.AddListener(() => onPick(captured));
        }
    }

    private void FinishDraft(bool challengeHiddenTier)
    {
        CloseDraftPanel();
        if (selectedEnhancedBonus != BirdDuelBonusId.None &&
            !chosenBonuses.Contains(selectedEnhancedBonus))
        {
            chosenBonuses.Add(selectedEnhancedBonus);
        }

        PreBattleBonusContext.Begin(new List<BirdDuelBonusId>(chosenBonuses), pendingEnemyBuff);
        ProceedToBattleAfterBirdDuel(challengeHiddenTier);
    }

    private void ProceedToBattleAfterBirdDuel(bool challengeHiddenTier)
    {
        if (preBattleMode && PreBattleDuelContext.IsHarborTraining && gameCanvas != null)
        {
            EnemyHeroProfile hero = EnemyHeroCatalog.ResolveForHarbor();
            int slot = PlayerData.GetActivePlayerSlotOrDefault();
            bool isRematch = HarborTrainingProgressState.HasMetHotBloodClassmate(slot);
            EnemyHeroPortraitBridgeUi.ShowPortraitB(
                gameCanvas,
                hero,
                isRematch,
                lastResult,
                null,
                () =>
                {
                    if (!isRematch)
                        HarborTrainingProgressState.MarkHotBloodClassmateMet(slot);
                    SceneLoader.ResumeBattleAfterBirdDuel(challengeHiddenTier, lastIntelText);
                });
            return;
        }

        SceneLoader.ResumeBattleAfterBirdDuel(challengeHiddenTier, lastIntelText);
    }

    private GameObject BuildDraftPanel(string title, string subtitle)
    {
        CloseDraftPanel();
        Transform parent = overlayRoot != null ? overlayRoot : uiRoot;
        if (parent == null) parent = transform;

        GameObject overlay = CreateImage("BonusDraftOverlay", parent,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.78f), true).gameObject;
        overlay.transform.SetAsLastSibling();

        Image panelImg = CreateImage("Panel", overlay.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ColorPanel, true);
        panelImg.rectTransform.sizeDelta = new Vector2(1180f, 560f);

        CreateText("DraftTitle", panelImg.transform, title, 58f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1100f, 76f), Color.white);
        TextMeshProUGUI sub = CreateText("DraftSubtitle", panelImg.transform, subtitle, 32f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1080f, 120f), ColorSubtitle);
        sub.enableWordWrapping = true;

        draftPanel = overlay;
        return panelImg.gameObject;
    }

    /// <summary>建立一張加成選項卡（名稱＋描述），錨定於面板底部中央偏移處。</summary>
    private Button CreateDraftButton(Transform panel, string name, string title, string desc, Color tint, Vector2 anchoredPos)
    {
        Image card = CreateImage(name, panel,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, tint, true);
        card.rectTransform.sizeDelta = new Vector2(320f, 200f);
        card.rectTransform.anchoredPosition = anchoredPos;

        Button btn = card.gameObject.AddComponent<Button>();
        btn.targetGraphic = card;

        TextMeshProUGUI titleText = CreateText("Title", card.transform, title, 38f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(300f, 60f), Color.white);
        titleText.raycastTarget = false;
        TextMeshProUGUI descText = CreateText("Desc", card.transform, desc, 26f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(296f, 110f),
            new Color(0.97f, 0.98f, 1f, 0.95f));
        descText.enableWordWrapping = true;
        descText.raycastTarget = false;
        return btn;
    }

    private string DescribeBonus(BirdDuelBonusId id)
    {
        if (id == BirdDuelBonusId.None) return "無";
        BirdDuelBonusInfo info = BirdDuelBonusCatalog.Get(id);
        return info.DisplayName + "（" + info.Description + "）";
    }

    private void CloseDraftPanel()
    {
        if (draftPanel != null)
        {
            Destroy(draftPanel);
            draftPanel = null;
        }
    }

    private void ReturnToHall()
    {
        if (Application.CanStreamedLevelBeLoaded(HallSceneName))
            SceneManager.LoadScene(HallSceneName);
        else
            Debug.LogWarning("FightingBirdGameSceneController: hall 場景不在 Build Settings，無法返回。");
    }
}
