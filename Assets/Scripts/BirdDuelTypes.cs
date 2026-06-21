using System;

/// <summary>鬥鳥暖身賽共用型別（規格：Docs/鬥鳥手勢小遊戲企劃.md）。</summary>
public enum BirdGesture
{
    Peck,   // 啄擊：進攻、打斷對方築巢
    Wing,   // 振翅：防守、閃開對方啄擊
    Nest,   // 築巢：佈局、成功反制振翅取得看破
    Pass    // PASS：保守防禦，踩準鼓點時低分穩住
}

public enum BirdBeatOutcome
{
    Perfect, // 鳥勢正確且踩準鼓點中心
    Good,    // 鳥勢正確但稍早／稍晚
    Guard,   // PASS 且踩準鼓點，低分防禦
    Miss     // 鳥勢錯誤、太早、太晚或未按
}

public enum BirdDuelResult
{
    Win,
    Draw,
    Lose
}

/// <summary>單一鼓點的判定結果。</summary>
public struct BirdBeatJudgement
{
    public BirdBeatOutcome outcome;
    public int scoreDelta;
    public int insightDelta;
    public bool isBestCounter;

    /// <summary>對手築巢但未被啄擊成功打斷（含 PASS／失誤），用於降低最終情報等級。</summary>
    public bool letNestThrough;
}
