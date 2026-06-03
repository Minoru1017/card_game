/// <summary>宗教派系怪物 id（主教戰技·宗教連攜判定）。與 CardList 聖職線對齊。</summary>
public static class MonsterSkillReligion
{
    public static bool IsReligiousMonsterId(int monsterId)
    {
        switch (monsterId)
        {
            case MonsterSkillIds.Bishop:
            case 15: // 聖女
            case 16: // 宗教審判官
            case MonsterSkillIds.Nun:
            case 18: // 聖院騎士
            case 19: // 聖盾軍
            case 20: // 聖盾
            case 21: // 傳教士
            case 22: // 教徒
                return true;
            default:
                return false;
        }
    }
}
