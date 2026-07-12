# M-1-2 Phase A Exam Win Rate (段考 A 批次模擬)

- Player deck: M12PhaseDeckApplicator Phase A (15 cards)
- Enemy: mirror Phase A deck · Balanced AI · 段考A · HP 15 · max 12 rounds
- Exam pass: **win + militia + queen + king skills all triggered**
- Auto-play: HarborNormalWinRateSimPump / BattleAutoSimPlugin heuristics
- Horror presentation: **skipped** during batch (`BattleAutoSimPlugin.IsRunning`); damage freeze still applies
- Round cap: after round 12, winner by hero HP (tie = draw)

| Metric | Value |
|--------|-------|
| Games requested | 50 |
| Games finished | 50 |
| Wins | 5 |
| Losses | 45 |
| Draws | 0 |
| **Exam passes** (win + trio) | 0 |
| Wins without trio | 5 |
| Win rate | 10.0% |
| **Exam pass rate** | 0.0% |
| Games with militia skill | 22 |
| Games with queen skill | 10 |
| Games with king skill | 9 |
| Aborted (step limit) | 0 |
| Avg rounds (finished) | 4.3 |
| Base seed | 20260708 |

Generated: 2026-07-08 14:40:55
