---
name: cloud-to-local-integration
description: Safely integrates code created by a Cursor Cloud Agent back into the Windows card_game checkout, including branch synchronization, Git LFS repair, Unity compilation, experimental define tests, local validation, and PR evidence. Use only when the user explicitly asks to bring Cloud work home or validate it locally.
disable-model-invocation: true
metadata:
  project: card_game
  owner: Minoru1017
---

# Cloud to Local Integration

Use this skill when the user invokes `/cloud-to-local-integration` or explicitly asks to fetch, stitch, test, or validate Cloud Agent code on the Windows Unity project.

Read [references/windows-unity-integration.md](references/windows-unity-integration.md) before providing or executing commands.

## Safety rules

- Cloud work remains on its `cursor/...` feature branch until local validation is complete.
- Never overwrite unknown local edits. Inspect first and stop if unrelated modifications exist.
- Never use force push, destructive reset, automatic merge, branch deletion, or deployment.
- Never commit temporary Unity define symbols, generated cache, personal saves, secrets, or `.cursor/mcp.json`.
- Preserve all Unity `.meta` files and GUIDs.
- Do not claim Scene, UI, audio, animation, gameplay feel, performance, or build success until tested in the Windows Unity Editor or target device.
- Merge only after explicit user authorization and after the final diff is understood.

## Workflow

### 1. Cloud delivery gate

Before asking the user to fetch, confirm Cloud has:

- Used a `cursor/...` feature branch.
- Committed and pushed every intended source file and `.meta`.
- Uploaded all referenced Git LFS objects.
- Updated or created a draft PR.
- Listed tests run in Cloud and tests unavailable in Cloud.
- Documented experimental code, feature flags, and whether runtime wiring exists.

Do not hand off a commit that exists only inside the Cloud VM.

### 2. Protect the Windows working tree

From:

```text
E:\School\Grade_5_2\card-game
```

run:

```powershell
git branch --show-current
git status --short
git remote -v
```

If unrelated local changes appear, stop. Do not switch branches, stash, restore, or clean without first explaining what the changes are and obtaining a safe disposition.

### 3. Fetch and select the Cloud branch

Fetch the exact branch:

```powershell
git fetch origin <cloud-feature-branch>
```

If the local branch does not exist:

```powershell
git switch --track origin/<cloud-feature-branch>
```

If it already exists:

```powershell
git switch <cloud-feature-branch>
git pull --ff-only origin <cloud-feature-branch>
```

Do not treat `branch already exists` as a failure; choose the second path.

Verify:

```powershell
git log -1 --oneline
git rev-list --left-right --count @{upstream}...HEAD
```

### 4. Reconcile Git LFS before Unity

Run:

```powershell
git lfs install
git lfs pull
git lfs status
git lfs fsck
```

If Git reports files that `should have been pointers`:

1. List only the affected paths.
2. Confirm no unrelated working-tree edits.
3. Run `git add --renormalize -- <explicit paths>`.
4. Check `git diff --cached --stat` and `git lfs status`.
5. Confirm the Git and LFS object hashes match.
6. Commit as a separate LFS normalization commit.
7. Push the feature branch and verify the upload.

Do not use `git add .` for this repair.

### 5. Review the Cloud diff

Compare against the base branch:

```powershell
git diff --stat origin/main...HEAD
git diff --name-status origin/main...HEAD
```

Review:

- Unrelated files.
- `.cursor/mcp.json`, credentials, or local settings.
- Missing `.meta` files.
- Unexpected Scene, Prefab, ScriptableObject, package, or ProjectSettings changes.
- Binary files not managed according to `.gitattributes`.
- Experimental code accidentally connected to production flow.

### 6. Run Windows Unity validation in layers

Use Unity `2023.1.22f1`.

Baseline first:

1. Open the project with all experimental defines disabled.
2. Wait for import and compilation.
3. Confirm Console has no compile errors.
4. Confirm changed LFS assets import.
5. Run the existing production flow affected by the change.

Experimental tests second:

1. Add only the documented temporary Scripting Define Symbol.
2. Wait for recompilation.
3. Run the named EditMode or PlayMode tests.
4. Capture pass/fail counts and errors.
5. Remove the temporary define.
6. Recompile and confirm Console is clean.

Never commit the temporary define unless the feature is explicitly approved for activation.

### 7. Finish with a clean repository

Run:

```powershell
git status --short
git rev-list --left-right --count @{upstream}...HEAD
```

Required result:

- No temporary ProjectSettings changes.
- No generated cache or personal save data.
- No uncommitted LFS normalization.
- Local and remote feature branch synchronized.

Update the draft PR with actual Windows results, including Unity version, test count, legacy-flow result, LFS result, and items still unverified.

### 8. Merge gate

Before merge, state:

- Final files changed.
- Cloud checks.
- Windows Unity checks.
- Target-device checks still pending.
- Known behavior differences between old and new code.
- Whether new code is active, feature-flagged, or fully isolated.

Merge only after the user explicitly authorizes it. Do not infer approval from successful tests.

## Completion report

Report:

- Local branch and commit SHA.
- Ahead/behind result.
- LFS status.
- Unity Console result.
- Test pass/fail counts.
- Existing flow regression result.
- Final `git status`.
- PR link and remaining risks.
