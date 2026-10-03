# karavi-folder

Pipeline / caretaker skill that initializes, migrates, and maintains the standard
**`karavi/` workspace** in a repository.

It performs two primary operations:

1. **Initialize & Create main folders** (`init` / `create`) — Scaffold the canonical
   `karavi/` tree (**Full by default: 20 folders & subfolders**) and automatically **detect, rename,
   and migrate** any existing or legacy folders inside `karavi/` to the new standard.
2. **Delete temporary info** (`clean`) — Remove temporary logs, status reports, build/deploy
   output, and stack caches while strictly preserving source, history, and configs.

> **This is a pipeline skill, not a reference skill.** Invoke it as
> `/karavi-folder init` when you want the assistant to initialize or migrate the `karavi/`
> folder in the *current* repository.

---

## Canonical Folders (Full Structure by Default — 20 Folders & Subfolders)

| Folder | Purpose |
|---|---|
| `karavi.plans.prompt` | Prompts, rules, and plans for all agents |
| `karavi.history` | Daily change history (`history.YYYY-MM-DD.md`) |
| `karavi.deploy.config` | Deploy & FTP configuration for this repository |
| `karavi.scripts.command` | Operator/agent commands (deploy, run all, clean, ...) |
| `karavi.scripts.tools` | Tooling helpers (verify gates, paths, ...) |
| `karavi.temp.logs` | Temporary local logs (gitignored) |
| `karavi.temp.status` | Temporary status reports (gitignored) |
| `karavi.temp.deploy` | Temporary release staging output (gitignored) |
| `karavi.temp.build` | Temporary build output (gitignored) |
| `karavi.assets/{brand,icons,screenshots,templates}` | Brand assets, icons, screenshots, and templates |
| `karavi.mockup` | Mockups and design reference files |
| `karavi.doc` | Technical, architectural, and operator docs |
| `karavi.BusinessModel.Doc` | Business model and commercial documentation |
| `karavi.Customer.doc` | Customer feedback and pre-execution planning |
| `karavi.OnlineContent/SociaMediaContent` | Social media content and campaign assets |
| `karavi.OnlineContent/WordPressContent` | WordPress posts, pages, and website content |
| `karavi.OnlineContent/LinkedinConetnt` | LinkedIn articles, posts, and professional networking content |

---

## Legacy Folder Migration

When running `init` or `create`, any existing folders inside `karavi/` with legacy or variant names
(e.g., `docs`, `prompts`, `history`, `deploy`, `scripts/command`, `logs`, `status`, `build`, `assets`,
`mockup`, `business`, `customer`, `social`, `karavi.SociaMediaContent`, `OnlineContent`, `wordpress`, `linkedin`) are **automatically detected, renamed, and relocated**
into the new standard structure without deleting or losing any content.

---

## Always-Use Habit

Initialize or update the karavi structure in **every** repository:

```bash
/karavi-folder init
```

Persian: `/karavi-folder شروع` یا `/karavi-folder ایجاد و بازسازی`. Idempotent, safe, and repairs existing legacy structures.

---

## Installation

```bash
npx skills add https://github.com/akaravi/Karavi.Skills --skill karavi-folder
```
---

## Invocation

| Command | What it runs |
|---|---|
| `/karavi-folder init` | Full initialization (20 folders/subfolders) + migrate legacy folders (Default) |
| `/karavi-folder init --core` | Core initialization (9 folders) + migrate legacy folders |
| `/karavi-folder create` | Full structure creation & migration (Default) |
| `/karavi-folder clean` | Clear temp.logs + temp.status |
| `/karavi-folder clean --deep` | Also clear temp.deploy + temp.build + stack caches |
| `/karavi-folder clean --what-if` | Dry-run preview, deletes nothing |

---

## When to Use

- "شروع اولیه ساختار karavi" / "مقداردهی اولیه karavi" → `init`
- "تغییر نام و انتقال فولدرهای قدیمی karavi به ساختار جدید" → `init`
- "ایجاد فولدرهای اصلی karavi" → `init` / `create`
- "پاک کن اطلاعات موقت / لاگ‌ها / کش karavi" → `clean`
- "Initialize standard karavi workspace" → `init`
- "Migrate legacy karavi folders to new standard" → `init`

---

## Safety

This skill **never deletes** source, config without secrets, `karavi.history/`,
`karavi.deploy.config/`, documentation, or README files. Only the four `karavi.temp.*` folders
and caches are deletable. It is idempotent and supports dry-run preview (`--what-if`).

---

## Reference

- [`HELP.md`](./HELP.md) — complete user guide (Persian/English, FAQ, troubleshooting)
- [`SKILL.md`](./SKILL.md) — entry point / decision core & AI directives
- [`references/folders.md`](./references/folders.md) — Section 1 mechanics & migration map
- [`references/cleanup.md`](./references/cleanup.md) — Section 2 mechanics
- [`scripts/`](./scripts/) — PowerShell automation helpers (`init.ps1`, `create.ps1`, `clean.ps1`)

---

## License

Apache-2.0
