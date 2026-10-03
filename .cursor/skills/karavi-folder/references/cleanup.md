# Section 2 — Delete temporary info (temp folders, caches, logs)

Mechanics behind `/karavi-folder clean`. Loaded only when this section runs.

## Purpose

Runs, builds, deploy checks, and diagnostics litter the repo with temporary
files. In this structure all temporaries live in the four `karavi.temp.*`
folders, so cleanup is simple and focused. This section removes them safely
while **never touching source, config, history, or READMEs**.

## Safety rules (never violated)

- **Preserve whitelist (never delete):**
  - `karavi/karavi.history/` (all `history.*.md`)
  - `karavi/karavi.deploy.config/` (and `karavi.scripts.command`, `karavi.scripts.tools`)
  - `karavi/karavi.plans.prompt/`, `karavi/karavi.assets/`, `karavi/karavi.mockup/`,
    `karavi/karavi.doc/`, `karavi/karavi.BusinessModel.Doc/`,
    `karavi/karavi.Customer.doc/`, `karavi/karavi.OnlineContent/`
  - All `README.md`, all `.gitkeep`, all committed source.
- **Git-clean:** the `karavi.temp.*` contents are gitignored and never committed;
  after cleanup `git status` has no temporary/generated files under `karavi/`.
- **Only the four `karavi.temp.*` folders are deletable** — everything else under
  `karavi/` is preserved.
- **Travel guard:** resolve every deletion path and refuse anything that
  resolves outside the repo root, or to a preserved path, with exit code 3.
- **Dry-run first:** `--what-if` lists every target and deletes nothing. The
  agent shows this plan and proceeds to real deletion only with explicit
  approval (or when the user already approved on the invocation).

## Logs/status cleanup (default)

Targets temporary junk in `karavi.temp.logs/` and `karavi.temp.status/`. Delete
files matching:

- `*.out.txt`, `*.err.txt` (captured run output)
- `_tmp-*.ps1`, `_tmp-*.py`, `_fix-*.py` (one-off polish/temp scripts)
- `*launch.ps1`, `*loop.ps1`, `*run.ps1` (throwaway runner scripts)
- `*.pid` (process liveness markers)
- Token/state dumps: `.browser-check-state.json`, `*.token.txt`,
  `http-*.json`, `*-state.json`, `*-smoke-*.json`
- Old status HTML/JSON reports in `karavi.temp.status/`
- Old stamp/debug files: `*-2026*.json`, `*-2026*.txt` where clearly transient

**Preserve inside temp folders:** any tracked/`.gitkeep` sentinel is kept, and a
real audit record is kept if it should last (check before deleting folders).

## Deep cleanup (`--deep`)

Everything in default **plus** build/deploy output and caches:

- `karavi/karavi.temp.build/` (build output)
- `karavi/karavi.temp.deploy/` (staged release output)
- Repo-level output: `publish/`, `artifacts/`, `.run-logs/`
- Generated HTML reports: `LastRunInfo.html`, `Deploy_Summary.html`
- Per-stack caches: `**/bin/`, `**/obj/`, `**/.dart_tool/`, `.next/`,
  `dist/`, `out/`, `node_modules/.cache/`

## Per-stack cache notes

| Stack | Cache/build dirs to clean (deep) |
|---|---|
| .NET | `**/bin/`, `**/obj/` |
| Dart/Flutter | `**/.dart_tool/`, `**/build/` |
| Next.js / Node | `.next/`, `node_modules/.cache/`, `dist/`, `out/` |
| Vite / web | `dist/`, `out/` |

Never delete `node_modules/` itself, package-lock/yarn.lock, or committed source.

## Procedure

1. Resolve repo root (same as Section 1).
2. Build the target set for the requested level (default or Deep).
3. Generate a `--what-if` plan; remove any preserved/out-of-root targets.
4. Show the plan; require explicit approval before real deletion (unless
   pre-approved).
5. Execute deletion with `Remove-Item -Recurse -Force`, skipping with
   `-ErrorAction SilentlyContinue` on locked files.
6. Report counts: `X files, Y dirs removed`.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Cleanup complete. |
| 1 | Repo root not found. |
| 2 | User declined after the dry-run preview. |
| 3 | A target was on the preserve list / resolved outside the repo — refused. |