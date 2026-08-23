# card_game Local-to-Cloud Preflight

## Project facts

- Repository: `https://github.com/Minoru1017/card_game`
- Unity: `2023.1.22f1`
- Windows path: `E:\School\Grade_5_2\card-game`
- Delivery: `cursor/...` feature branch → push → draft PR
- Final Scene, Prefab, UI, audio, gameplay feel, performance, Android, and Windows build checks remain local.

## Paths that must remain local

- `.cursor/mcp.json`
- Unity license and credentials
- `.env` and private keys
- `Library/`
- `Temp/`
- `Logs/`
- `obj/`
- Local build output

## Unity asset checks

- Never delete or regenerate existing `.meta` files.
- Move or rename an asset together with its `.meta`.
- Verify GUID references before deleting or moving assets.
- Do not hand-edit large Scene or Prefab YAML without a narrow, reviewable reason.
- Confirm large binaries match `.gitattributes`.

## LFS incident pattern

This repository previously produced:

```text
GH008: Your push referenced unknown Git LFS objects
```

The recovery sequence was:

1. Confirm the object IDs exist on the Windows machine with `git lfs ls-files --all --long`.
2. Upload all reachable objects with `git lfs push --all origin <branch>`.
3. If GitHub still reports specific IDs, upload those IDs explicitly.
4. Normalize raw blobs that should be LFS pointers using `git add --renormalize -- <explicit paths>`.
5. Confirm `git lfs status` reports matching Git and LFS hashes.
6. Commit the normalization separately and push.

Never solve GH008 by deleting required assets or removing LFS tracking.

## Handoff prompt template

```text
Repository: Minoru1017/card_game
Branch: <cursor/feature-branch>
Handoff commit: <sha>

Task:
<specific change>

Acceptance criteria:
- <criterion>

Already completed:
- <work and test result>

Cloud verification:
- <commands>

Do not modify:
- Unity version, package versions, unrelated Scenes/Prefabs, existing .meta/GUID

Must remain pending for Windows Unity Editor:
- Scene/Prefab/Inspector, UI layout, animation, audio timing, gameplay feel,
  performance, and platform builds.

Complete on the feature branch, commit, push, and update the draft PR.
Do not merge or push main.
```
