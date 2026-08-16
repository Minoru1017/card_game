/// <summary>A-1 島嶼老人劇情文案（企劃發想.md §A-1.3）。</summary>
public static class SideQuestA1PlotCopy
{
    public static class Voice
    {
        public const string Harbor0 = "A-1_V0";
        public const string Harbor1 = "A-1_V1";
        public const string Harbor2 = "A-1_V2";
        public const string Island0 = "A-1_V3";
        public const string Island1 = "A-1_V4";
        public const string Island2 = "A-1_V5";
        public const string IslandSealedHint = "A-1_V6";
        public const string Unseal1 = "A-1_V7";
        public const string Unseal3 = "A-1_V8";
        public const string Unseal4 = "A-1_V9";
        public const string Return0 = "A-1_V10";
    }

    public static readonly string[] HarborLaunchLines =
    {
        "潮間島？草奶奶不愛見生面孔。",
        "學院的？別穿制服還好。上了船別四處喊。",
        "坐穩。退潮只這一陣。"
    };

    public const string HarborChoicePrompt = "還去嗎？";

    public const string VoyageCaption =
        "舢板輕晃……退潮把港灣遠遠推開。\n鹽味漸濃，島影浮在灰綠的潮間帶上。";

    public const string IslandNarration =
        "島小得轉身就看見海。田在斜坡上，分成三畦。";

    public const string IslandGrandmaOpen = "……來都來了。會握鋤嗎？";

    public const string IslandGrandmaOrder =
        "上畦先收 **黑麥**。中畦**別種**——讓它歇。下畦等收完再動。";

    public const string IslandSealedSpellHint =
        "別硬撬你包袱裡那東西。地耗盡了，咒也揭不開。";

    public const string UnsealGrandma1 =
        "蠟不是鎖，是怕潮。**洗、曬、再貼**——跟三畦一輪一樣。";

    public const string UnsealNarration =
        "你將 **潮根滷** 抹在蠟封上。鹽霜化開，露出「**潮印**」二字。";

    public const string UnsealGrandma2 =
        "**檜**抄的祝禱，終於能歸檔了。別告訴學院是我教的。";

    public const string UnsealGrandma3 = "報酬在籃裡。船要來了。";

    public const string ReturnDefault = "回來了？臉色比去的時候穩。";

    public const string ReturnWithSeed = "草奶奶給了豆種？那你自己種，別在碼頭上曬。";

    public static class FarmInterject
    {
        public const string RyeCompactFail = "海風吹種。壓實。";
        public const string RyeHarvestDone = "夠烤一張餅。中畦，松土就好。";
        public const string FallowNetDone = "歇地不是偷懶。等潮退，鹽會自己走。";
        public const string PurslanePick = "燈芯要這種嫩的。你留一叢也行。";
        public const string BeanWaterDone = "豆根還地。人欠的，也要還。";
        public const string BeanHarvestDone = "莢裡的滷水，拿去洗蠟。";
    }
}
