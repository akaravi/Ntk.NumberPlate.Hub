# Section 1 — Initialize, Scaffold & Migrate the `karavi/` Workspace

Mechanics behind `/karavi-folder init` and `/karavi-folder create`.

---

## Purpose & Scope

`karavi/` is the single focal point in **every** repository for the operator and AI agents:
planning, technical & business documentation, history, scripts, tools, logs, status, build, and deploy.

### Non-Negotiable Defaults
1. **Full Structure by Default:** Every initialization creates all **20 canonical folders & subfolders** (Core + Extended). Core-only is available only if explicitly requested (`--core`).
2. **Automatic Migration & Renaming:** If the repository already contains folders under `karavi/` with legacy or variant names, they are **automatically detected, renamed, and relocated** into the new standard structure without deleting or losing any files.
3. **Local to Current Repo:** Freshly scaffolded inside the current repository root. Never imports from or copies another project.

---

## Canonical Folder Structure (Full — Default: 20 Folders & Subfolders)
| # | Canonical Path | Description |
|---|---|---|
| 1 | `karavi/karavi.plans.prompt` | Prompts, reusable JSON rules, and Agent plans for **all** assistants (Cursor, Claude, Antigravity, OpenCode, Codex, Cline) in one place. Store `Karavi.NNN.plan.md` flat here. |
| 2 | `karavi/karavi.history` | Change history of the project. One file per day: `history.YYYY-MM-DD.md`. Never deleted. |
| 3 | `karavi/karavi.deploy.config` | Deploy & FTP configuration for this repo: `production-hosts.json`, `deploy-targets.json`, `local-dev-ports.json`, `Deploy_FTP.info`, secrets. Public JSON tracked; real credentials gitignored. |
| 4 | `karavi/karavi.scripts.command` | Operator and agent **command entry points** (deploy, run all, clean, history.write, ...). |
| 5 | `karavi/karavi.scripts.tools` | Tooling **helpers** (verify structure, path resolvers, build pieces, `verify-gates.json`, ...). Reused by scripts.command. |
| 6 | `karavi/karavi.temp.logs` | **Temporary** local logs and captured run output (gitignored). Cleared by `clean`. |
| 7 | `karavi/karavi.temp.status` | **Temporary** execution/deploy status reports (HTML/JSON) (gitignored). Cleared by `clean`. |
| 8 | `karavi/karavi.temp.deploy` | **Temporary** release output staged, ready to deploy (gitignored). Cleared by `clean --deep`. |
| 9 | `karavi/karavi.temp.build` | **Temporary** build output (gitignored). Cleared by `clean --deep`. |
| 10 | `karavi/karavi.assets/brand` | Brand assets, logos, color palettes, and styling guidelines. |
| 11 | `karavi/karavi.assets/icons` | Project icon sets, SVGs, and favicon assets. |
| 12 | `karavi/karavi.assets/screenshots` | UI screenshots, design previews, and workflow captures. |
| 13 | `karavi/karavi.assets/templates` | Document and code templates for the workspace. |
| 14 | `karavi/karavi.mockup` | UI mockups, wireframes, and design reference files. |
| 15 | `karavi/karavi.doc` | General technical, architectural, and operator documentation. |
| 16 | `karavi/karavi.BusinessModel.Doc` | Business model, revenue plans, commercial strategy, and requirements. |
| 17 | `karavi/karavi.Customer.doc` | Customer personas, user research, feedback, and pre-execution planning. |
| 18 | `karavi/karavi.OnlineContent/SociaMediaContent` | Social media posts, banners, marketing materials, and campaign content. |
| 19 | `karavi/karavi.OnlineContent/WordPressContent` | WordPress posts, pages, and website content. |
| 20 | `karavi/karavi.OnlineContent/LinkedinConetnt` | LinkedIn articles, posts, and professional networking content. |

---

## Legacy Folder Migration & Renaming (مهاجرت و تغییر نام فولدرها)

When `init` or `create` runs, the AI agent and the scripts scan `karavi/` for any legacy/variant directories and relocate them to the standard canonical paths.

### Migration Mapping Table

| Existing / Legacy Folder Name in `karavi/` | Target Canonical Folder |
|---|---|
| `plans`, `prompt`, `prompts`, `karavi.plans`, `karavi.prompt`, `karavi.prompts`, `plans.prompt`, `prompts.plan` | `karavi/karavi.plans.prompt` |
| `history`, `histories`, `karavi.histories`, `change-history`, `log-history` | `karavi/karavi.history` |
| `deploy`, `config`, `deploy.config`, `karavi.deploy`, `karavi.config`, `deploy-config` | `karavi/karavi.deploy.config` |
| `commands`, `scripts.command`, `karavi.commands`, `karavi.scripts.command`, `scripts/command` | `karavi/karavi.scripts.command` |
| `tools`, `scripts.tools`, `karavi.tools`, `karavi.scripts.tools`, `scripts/tools` | `karavi/karavi.scripts.tools` |
| `scripts` (general/unsplit) | Contents moved into `karavi/karavi.scripts.command` & `karavi/karavi.scripts.tools` |
| `logs`, `log`, `temp.logs`, `karavi.logs`, `temp/logs` | `karavi/karavi.temp.logs` |
| `status`, `temp.status`, `karavi.status`, `temp/status` | `karavi/karavi.temp.status` |
| `temp.deploy`, `karavi.deploy.temp`, `temp/deploy` | `karavi/karavi.temp.deploy` |
| `build`, `temp.build`, `karavi.build`, `temp/build` | `karavi/karavi.temp.build` |
| `assets`, `karavi.asset`, `assets/{brand,icons,...}` | `karavi/karavi.assets/{brand,icons,screenshots,templates}` |
| `mockup`, `mockups`, `karavi.mockups`, `ui-mockups` | `karavi/karavi.mockup` |
| `doc`, `docs`, `karavi.docs`, `documentation`, `karavi.documentation` | `karavi/karavi.doc` |
| `BusinessModel`, `business`, `businessmodel`, `karavi.business`, `karavi.businessmodel`, `BusinessModel.Doc` | `karavi/karavi.BusinessModel.Doc` |
| `customer`, `customers`, `karavi.customer`, `Customer`, `Customer.doc` | `karavi/karavi.Customer.doc` |
| `social`, `socialmedia`, `karavi.social`, `karavi.socialmedia`, `SocialMediaContent`, `karavi.SociaMediaContent`, `SociaMediaContent` | `karavi/karavi.OnlineContent/SociaMediaContent` |
| `wordpress`, `WordPressContent`, `wordpresscontent` | `karavi/karavi.OnlineContent/WordPressContent` |
| `linkedin`, `LinkedinContent`, `LinkedinConetnt`, `linkedincontent` | `karavi/karavi.OnlineContent/LinkedinConetnt` |

---

## AI Execution Procedure (گام‌های اجرایی هوش مصنوعی)

When executing `/karavi-folder init` or `/karavi-folder create`:

1. **Locate Repo Root:**
   Find the root containing `.git` or `karavi/`.
2. **Scan Existing `karavi/`:**
   Inspect all child directories under `karavi/`.
3. **Execute Migration:**
   - For each legacy directory found, check if the canonical target exists.
   - If the canonical target does not exist, rename/move the directory directly.
   - If the canonical target already exists, move each item inside the legacy directory into the canonical target, preserving all files. If a filename collision occurs, rename the incoming file with a `.legacy-*` suffix rather than overwriting.
   - Remove the empty legacy folder once emptied.
4. **Scaffold Missing Folders (Full by default):**
   Ensure all 20 canonical folders/subfolders exist. Create any folder that is missing.
5. **Place Sentinel `.gitkeep`:**
   Add `.gitkeep` inside empty tracking/temp folders so git tracks directory structure.
6. **Wire `.gitignore`:**
   Ensure the `# --- karavi ---` block is present in the repo's `.gitignore`.
7. **Report:**
   Output a summary of migrated folders and newly created paths.

---

## `.gitignore` Specification

Every karavi workspace must have this block in the root `.gitignore`:

```gitignore
# --- karavi ---
karavi/karavi.temp.logs/
karavi/karavi.temp.status/
karavi/karavi.temp.deploy/
karavi/karavi.temp.build/
karavi/karavi.deploy.config/Deploy_FTP.info
karavi/karavi.deploy.config/Deploy_TestUsers.info
karavi/karavi.deploy.config/deploy.secrets.json
```

---

## Verification Checklist

- [ ] All 20 canonical folders & subfolders exist under `karavi/`.
- [ ] No old/legacy unmapped folders remain in `karavi/`.
- [ ] Existing history, documentation, prompts, and configs have been migrated intact.
- [ ] `.gitignore` contains the `# --- karavi ---` block.
- [ ] `git status` is clean of temporary/generated files under `karavi/`.
