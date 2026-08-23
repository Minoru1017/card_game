# card_game Windows Unity Integration Checklist

## Required environment

- Repository: `Minoru1017/card_game`
- Local path: `E:\School\Grade_5_2\card-game`
- Unity Editor: `2023.1.22f1`
- Base branch: normally `main`
- Delivery branch: `cursor/...`

## Phase A — Git and LFS

- [ ] `git status --short` reviewed before branch switch.
- [ ] Exact Cloud branch fetched.
- [ ] Existing local branch updated with `git pull --ff-only`, or a new tracking branch created.
- [ ] `git log -1 --oneline` matches the Cloud handoff SHA.
- [ ] `git rev-list --left-right --count @{upstream}...HEAD` is `0 0`.
- [ ] `git lfs pull`, `git lfs status`, and `git lfs fsck` succeed.
- [ ] No `should have been pointers` warning remains.
- [ ] No machine-local or secret files are tracked.

## Phase B — Diff review

- [ ] Changed files match the requested scope.
- [ ] New Unity assets include their `.meta`.
- [ ] No existing `.meta` or GUID was regenerated.
- [ ] Scene, Prefab, ScriptableObject, package, and ProjectSettings changes are intentional.
- [ ] Experimental code is isolated unless activation was explicitly requested.
- [ ] Documentation describes old/new behavior and activation state.

## Phase C — Unity baseline

- [ ] Unity `2023.1.22f1` opens the project.
- [ ] Console has no compilation errors with experimental defines disabled.
- [ ] LFS assets import normally.
- [ ] Existing production flow still starts.
- [ ] Relevant save, reward, retry, replay, and scene-return behavior is checked.

## Phase D — Experimental validation

- [ ] Only the documented define is temporarily enabled.
- [ ] Unity recompiles without errors.
- [ ] Named EditMode or PlayMode tests are visible.
- [ ] Test pass/fail count is recorded.
- [ ] Experimental define is removed after testing.
- [ ] Unity recompiles cleanly after removal.

For A-1 Farm V2:

```text
Define: CARDGAME_A1_FARM_V2
Tests: CardGame.Experimental.Tests.A1.A1FarmStateMachineV2Tests
Expected initial suite: 6 EditMode tests
Runtime state: isolated; no adapter; legacy A-1 remains active
```

## Phase E — Clean handoff

- [ ] `git status --short` has no output.
- [ ] Feature branch is pushed and synchronized.
- [ ] Draft PR contains actual Windows test evidence.
- [ ] Visual, audio, gameplay-feel, performance, and platform-build gaps are stated.
- [ ] User explicitly approves any merge.

## Failure handling

### Branch already exists

Use:

```powershell
git switch <branch>
git pull --ff-only origin <branch>
```

Do not run `git switch --track` again.

### Unknown Git LFS objects

Return to a machine that owns the objects and upload them. Do not remove assets or LFS rules to bypass GitHub.

### Local changes block branch switch

Stop and identify every path. Do not force checkout or destructive reset.

### Unity test does not appear

Check:

- Required define is active for the current platform.
- Test asmdef has the correct define constraints and Editor platform.
- Unity Test Framework package is available.
- Console contains no assembly compilation error.
