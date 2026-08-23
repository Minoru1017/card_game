---
name: local-to-cloud-handoff
description: Safely transfers card_game work from the Windows repository to a Cursor Cloud Agent by checking Git state, secrets, Git LFS objects, remote synchronization, and the handoff prompt. Use only when the user explicitly asks to move or continue local work in Cloud.
disable-model-invocation: true
metadata:
  project: card_game
  owner: Minoru1017
---

# Local to Cloud Handoff

Use this skill when the user invokes `/local-to-cloud-handoff` or explicitly asks to move current local work to Cloud Agents.

The goal is not merely to open a Cloud Agent. The goal is to ensure every required source file and Git LFS object is available remotely, while local settings and secrets remain local.

Read [references/card-game-preflight.md](references/card-game-preflight.md) before running commands.

## Safety rules

- Work from a `cursor/...` feature branch. Never transfer uncommitted work by pushing directly to `main`.
- Do not use force push, destructive reset, automatic merge, branch deletion, or deployment.
- Do not commit `.cursor/mcp.json`, `.env`, credentials, Unity licenses, tokens, `Library/`, `Temp/`, `Logs/`, `obj/`, or build output.
- Do not assume a clean commit history means a clean working tree.
- Cloud can only see remote commits and remote Git LFS objects. A local stash is invisible to Cloud.
- Stop if the remote branch advanced unexpectedly, a merge conflict appears, or the source of local changes is unclear.

## Workflow

### 1. Identify the exact repository and task

Confirm:

- Repository root.
- Current branch and intended Cloud base branch.
- Scope of work being transferred.
- Whether any required files exist only on the Windows machine.
- Whether Unity visual or platform validation must remain a local follow-up.

For this project, the expected Windows path is:

```text
E:\School\Grade_5_2\card-game
```

### 2. Inspect before changing anything

Run:

```powershell
git branch --show-current
git status --short
git remote -v
git fetch origin <branch>
git rev-list --left-right --count HEAD...origin/<branch>
```

Classify every modified, staged, and untracked path. Exclude unrelated files rather than sweeping them into the handoff.

If currently on `main`, create an informative `cursor/...` feature branch before committing.

### 3. Protect machine-local and sensitive data

Check whether sensitive or local-only paths are tracked:

```powershell
git ls-files ".cursor/mcp.json" ".env" "*.key" "*.pem"
```

Do not print secret values. If a local configuration was accidentally staged, unstage it and add an appropriate ignore rule without deleting the user's only local copy.

### 4. Validate Unity assets and Git LFS

Run:

```powershell
git lfs install
git lfs status
git lfs fsck
git lfs ls-files --all --long
```

Check `.gitattributes` before adding large MP4, PSD, audio, model, or archive files.

If Git reports `should have been pointers`, do not blindly commit all changes. Normalize only the listed assets, verify that Git and LFS object hashes match, then commit the storage-format correction separately.

If GitHub rejects a Cloud branch with `GH008 unknown Git LFS objects`, upload from the machine that actually has those objects:

```powershell
git lfs push --all origin <source-branch>
```

If necessary, push the exact missing object IDs. Retry the Cloud branch push only after LFS upload succeeds.

### 5. Validate, commit, and push

Run the checks available without changing Unity serialization. Stage only task files and their required `.meta` files.

Create logical commits with concise messages, then push normally:

```powershell
git push -u origin <feature-branch>
```

Verify:

```powershell
git status --short
git rev-list --left-right --count @{upstream}...HEAD
git rev-parse HEAD
```

Required result:

- Working tree is clean, except explicitly documented local-only ignored files.
- Ahead/behind is `0 0`.
- Required LFS objects exist remotely.

### 6. Move the conversation to Cloud

Use **Move to Cloud** in the current Agent conversation, then verify:

- Repository is `Minoru1017/card_game`.
- Branch contains the pushed handoff commit.
- Cloud Agent run status is active.

Do not assume terminal state, local Unity cache, checkpoints, MCP configuration, secrets, or unpushed commits transfer with the conversation.

### 7. Send a complete handoff prompt

Include:

- Exact task and acceptance criteria.
- Repository, feature branch, and commit SHA.
- Completed work and remaining work.
- Important files and decisions.
- Tests already run and their results.
- Commands Cloud should run.
- Items that require Windows Unity Editor verification.
- Files or systems Cloud must not modify.

## Completion report

Report:

- Branch and commit SHA.
- Push and LFS status.
- Cloud Agent URL or run status.
- Tests completed.
- Local-only items not transferred.
- Windows Unity validation still required.
