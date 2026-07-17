/// <summary>對戰開局模式（與 <see cref="BattleLaunchContext"/> bool 旗標並存，逐步取代分散 if-else）。</summary>
public enum BattleModeKind
{
    None = 0,
    IntroTutorial,
    HarborTraining,
    FreeBattle,
    M12PhaseA,
    M12PhaseB,
    M13Weather,
    M13RivalDuel,
}
