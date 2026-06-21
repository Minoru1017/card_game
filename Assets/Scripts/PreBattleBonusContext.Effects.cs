using UnityEngine;

public static partial class PreBattleBonusContext
{
    private static BirdDuelBonusEffects BuildEffectsInternal()
    {
        BirdDuelBonusEffects e = DefaultEffects();
        for (int i = 0; i < playerBonuses.Count; i++)
            ApplyOne(ref e, playerBonuses[i]);
        ApplyOne(ref e, EnemyBuff);
        return e;
    }

    private static void ApplyOne(ref BirdDuelBonusEffects e, BirdDuelBonusId id)
    {
        switch (id)
        {
            case BirdDuelBonusId.MorningPractice: e.PlayerHpDelta += 3; break;
            case BirdDuelBonusId.ExtraCard: e.OpeningExtraDraw += 1; break;
            case BirdDuelBonusId.SteadyStance: e.EnemyDamageMultiplier *= 0.85f; break;
            case BirdDuelBonusId.Tailwind: e.OpeningWeather = BirdDuelOpeningWeather.Gale; break;
            case BirdDuelBonusId.InsightOpening:
                e.RevealEnemyHandCount = Mathf.Max(e.RevealEnemyHandCount, 1); break;

            case BirdDuelBonusId.DeepRest: e.PlayerHpDelta += 6; break;
            case BirdDuelBonusId.FirstStrike: e.UnlockPlayerOpeningAttack = true; break;
            case BirdDuelBonusId.DoubleDraw: e.OpeningExtraDraw += 2; break;
            case BirdDuelBonusId.Regroup: e.PlayerExtraDrawPerTurn += 1; break;
            case BirdDuelBonusId.Suppress: e.EnemyDamageMultiplier *= 0.8f; break;
            case BirdDuelBonusId.InsightFull:
                e.RevealEnemyHandCount = Mathf.Max(e.RevealEnemyHandCount, 2);
                e.EnemyDamageMultiplier *= 0.9f;
                break;

            case BirdDuelBonusId.Providence: e.OpeningWeather = BirdDuelOpeningWeather.Fog; break;
            case BirdDuelBonusId.FullDraw:
                e.PlayerHpDelta += 6;
                e.OpeningExtraDraw += 2;
                e.UnlockPlayerOpeningAttack = true;
                break;
            case BirdDuelBonusId.LastStand:
                e.PlayerHpAbsolute = 12;
                e.OpeningExtraDraw += 2;
                e.UnlockPlayerOpeningAttack = true;
                e.OpeningWeather = BirdDuelOpeningWeather.Gale;
                break;

            case BirdDuelBonusId.CourtDecree:
                e.UnlockPlayerOpeningAttack = true;
                e.RevealEnemyHandCount = Mathf.Max(e.RevealEnemyHandCount, 1);
                break;
            case BirdDuelBonusId.RoyalPhalanx:
                e.PlayerHpDelta += 4;
                e.EnemyDamageMultiplier *= 0.88f;
                break;
            case BirdDuelBonusId.VanguardRecon:
                e.OpeningExtraDraw += 1;
                e.RevealEnemyHandCount = Mathf.Max(e.RevealEnemyHandCount, 2);
                break;
            case BirdDuelBonusId.CrownGuard:
                e.PlayerHpDelta += 3;
                e.OpeningWeather = BirdDuelOpeningWeather.Fog;
                break;
            case BirdDuelBonusId.WarDrumCharge:
                e.UnlockPlayerOpeningAttack = true;
                e.OpeningExtraDraw += 1;
                break;

            case BirdDuelBonusId.PrayerVigil:
                e.RevealEnemyHandCount = Mathf.Max(e.RevealEnemyHandCount, 2);
                e.OpeningWeather = BirdDuelOpeningWeather.Fog;
                break;
            case BirdDuelBonusId.VeiledSight:
                e.RevealEnemyHandCount = Mathf.Max(e.RevealEnemyHandCount, 3);
                break;
            case BirdDuelBonusId.QuietRegroup:
                e.PlayerHpDelta += 3;
                e.PlayerExtraDrawPerTurn += 1;
                break;
            case BirdDuelBonusId.GalePsalm:
                e.OpeningWeather = BirdDuelOpeningWeather.Gale;
                e.OpeningExtraDraw += 1;
                break;
            case BirdDuelBonusId.SacredShield:
                e.PlayerHeroShieldActive = true;
                break;
            case BirdDuelBonusId.HiddenPath:
                e.PlayerRarityDrawMaxRound = 3;
                break;

            case BirdDuelBonusId.EnemyMorale: e.EnemyHpDelta += 2; break;
            case BirdDuelBonusId.EnemyDraw: e.EnemyExtraOpeningDraw += 1; break;
            case BirdDuelBonusId.EnemyOffense: e.EnemyDamageMultiplier *= 1.1f; break;
        }
    }
}
