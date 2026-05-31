using System.Collections.Generic;

using UnityEngine;



/// <summary>教學戰勝利獎勵：御三家各 1 張加入收藏（僅首次通關；重溫入門不重複發）。</summary>

public static class TutorialBattleRewardService

{

    public readonly struct VictoryCardPreview

    {

        public readonly int CardId;

        public readonly string DisplayName;

        public readonly Sprite ArtSprite;

        public readonly Card Card;



        public VictoryCardPreview(int cardId, string displayName, Sprite artSprite, Card card)

        {

            CardId = cardId;

            DisplayName = displayName;

            ArtSprite = artSprite;

            Card = card;

        }

    }



    public static readonly int[] VictoryCardIds =

    {

        MonsterSkillIds.King,

        MonsterSkillIds.Queen,

        MonsterSkillIds.Militia

    };



    /// <summary>本次教學戰勝利是否仍應發放御三家（首次通關為 true）。</summary>

    public static bool ShouldGrantIntroTrioForActivePlayer() =>

        !TutorialProgressState.IsIntroTrioRewardGrantedForActivePlayer();



    /// <summary>若尚未領過則發放御三家並寫入旗標；回傳是否本次有發放。</summary>

    public static bool TryGrantIntroTrioReward(PlayerData playerData = null)

    {

        int slot = PlayerData.GetActivePlayerSlotOrDefault();

        if (TutorialProgressState.IsIntroTrioRewardGranted(slot))

            return false;



        playerData = playerData != null ? playerData : PlayerData.ResolveCanonical();

        if (playerData == null)

        {

            Debug.LogError("TutorialBattleRewardService: PlayerData not found.");

            return false;

        }



        playerData.LoadPlayerData();

        for (int i = 0; i < VictoryCardIds.Length; i++)
            playerData.AddCollection(VictoryCardIds[i], 1);

        // 先存收藏；旗標須在 Save 之後寫入（Save 會重建 CSV，且須帶入御三家以寫入畢業列）。
        playerData.SavePlayerData();
        TutorialProgressState.SetIntroTrioRewardGranted(slot, true);
        TutorialProgressState.SetTutorialBattleCompleted(slot, true);
        if (!TutorialProgressState.IsTutorialPlotCompleted(slot))
            TutorialProgressState.SetTutorialPlotCompleted(slot, true);
        TutorialProgressState.PersistAcademyIntroGraduated(slot);

        Debug.Log("TutorialBattleRewardService: granted King, Queen, Militia to collection (first time).");

        return true;

    }



    public static void GrantVictoryCards(PlayerData playerData = null) =>

        TryGrantIntroTrioReward(playerData);



    public static string FormatRewardCardNames(CardStore cardStore)

    {

        if (cardStore == null)

            return "國王 王后 民兵";



        string king = ResolveCardName(cardStore, MonsterSkillIds.King, "國王");

        string queen = ResolveCardName(cardStore, MonsterSkillIds.Queen, "王后");

        string militia = ResolveCardName(cardStore, MonsterSkillIds.Militia, "民兵");

        return king + "  " + queen + "  " + militia;

    }



    public static void FillVictoryCardPreviews(CardStore store, List<VictoryCardPreview> outPreviews)

    {

        outPreviews.Clear();

        for (int i = 0; i < VictoryCardIds.Length; i++)

        {

            int id = VictoryCardIds[i];

            Card card = store != null ? store.GetCardById(id) : null;

            string name = card != null && !string.IsNullOrWhiteSpace(card.cardName) ? card.cardName.Trim() : FallbackName(id);

            outPreviews.Add(new VictoryCardPreview(id, name, ResolveCardArtSprite(card, id), card));

        }

    }



    public static Sprite ResolveCardArtSprite(Card card, int cardId = -1)

    {

        if (card != null)

        {

            Sprite art = card.ResolveCardArtSprite();

            if (art != null) return art;

            art = card.ResolveDeckThumbSprite();

            if (art != null) return art;

            if (!string.IsNullOrWhiteSpace(card.artworkResourcePath))

            {

                art = Resources.Load<Sprite>(card.artworkResourcePath.Trim());

                if (art != null) return art;

            }

            if (!string.IsNullOrWhiteSpace(card.deckThumbResourcePath))

            {

                art = Resources.Load<Sprite>(card.deckThumbResourcePath.Trim());

                if (art != null) return art;

            }

        }



        int id = card != null ? card.id : cardId;

        if (id >= 0)

        {

            Sprite byId = Resources.Load<Sprite>("CardArt/" + id);

            if (byId != null) return byId;

        }



        return null;

    }



    private static string ResolveCardName(CardStore store, int id, string fallback)

    {

        Card card = store != null ? store.GetCardById(id) : null;

        return card != null && !string.IsNullOrWhiteSpace(card.cardName) ? card.cardName.Trim() : fallback;

    }



    private static string FallbackName(int id)

    {

        if (id == MonsterSkillIds.King) return "國王";

        if (id == MonsterSkillIds.Queen) return "王后";

        if (id == MonsterSkillIds.Militia) return "民兵";

        return "卡牌";

    }

}


