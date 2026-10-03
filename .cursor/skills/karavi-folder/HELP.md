# karavi-folder — Help

راهنمای جامع کاربر برای مهارت `karavi-folder`. این مهارت اسکلت استاندارد `karavi/` را
به‌صورت **کامل (Full - ۱۸ فولدر به‌صورت پیش‌فرض)** می‌سازد، ساختارهای قدیمی و نام‌های قبلی
را شناسایی و **مهاجرت/تغییر نام** می‌دهد، و فایل‌های موقت (`karavi.temp.*`) و کش‌ها را به‌صورت
امن پاک‌سازی می‌کند.

Complete user guide for the `karavi-folder` skill: scaffolds the standard `karavi/`
workspace tree (Full structure by default: 18 folders), migrates and renames legacy
folder trees, and safely cleans temporary data.

---

## ۱) این مهارت چه کاری انجام می‌دهد؟

دو عملیات کلیدی:

| بخش | عملیات | توضیحات |
|---|---|---|
| ۱ | **مقداردهی اولیه، ساخت و مهاجرت (`init` / `create`)** | ساخت ساختار کامل (۱۸ فولدر پیش‌فرض) + تغییر نام و انتقال خودکار فولدرهای قدیمی به استاندارد جدید |
| ۲ | **پاک‌سازی اطلاعات موقت (`clean`)** | پاک کردن امن لاگ‌ها، گزارش‌های وضعیت، خروجی build/deploy موقت و کش‌ها |

### اصول تغییرناپذیر
- **پیش‌فرض کامل (Full):** ساختار فولدرها به‌صورت پیش‌فرض کامل است (۱۸ فولدر).
- **مهاجرت بدون از دست رفتن داده:** فولدرها و فایل‌های قبلی موجود در `karavi/` شناسایی شده و بدون حذف داده، به ساختار استاندارد جدید منتقل و تغییر نام می‌یابند.
- **حفظ ۱۰۰٪ فایل‌های مهم:** سورس، تاریخچه تغییرات (`karavi.history`)، تنظیمات (`karavi.deploy.config`)، پرامپت‌ها و مستندات هرگز پاک نمی‌شوند.
- **فقط در ریپوی فعلی:** هیچ‌گاه فولدری از پروژه دیگر کپی یا ایمپورت نمی‌شود.
- **Idempotent (امن در اجرای مجدد):** اجرای چندباره هیچ مشکلی ایجاد نمی‌کند.

---

## ۲) فولدرهای استاندارد (پیش‌فرض: کامل / Full — ۲۰ فولدر و زیرفولدر)
| # | مسیر فولدر | کاربرد |
|---|---|---|
| ۱ | `karavi/karavi.plans.prompt` | پرامپت‌ها، قوانین و پلن‌های تمامی ایجنت‌ها و دستیارها (Cursor/Claude/سایر) |
| ۲ | `karavi/karavi.history` | تاریخچه تغییرات روزانه پروژه (`history.YYYY-MM-DD.md`) — هرگز حذف نمی‌شود |
| ۳ | `karavi/karavi.deploy.config` | تنظیمات deploy و FTP این مخزن (هاست‌ها، پورت‌ها، سکرت‌های محلی) |
| ۴ | `karavi/karavi.scripts.command` | دستورات اجرایی اپراتور و هوش مصنوعی (deploy، clean، history.write و...) |
| ۵ | `karavi/karavi.scripts.tools` | اسکریپت‌های کمکی و ابزارها (verify gates، مسیر‌یاب‌ها و...) |
| ۶ | `karavi/karavi.temp.logs` | لاگ‌های موقت و خروجی اجرای دستورات (gitignored) — پاک‌شدنی |
| ۷ | `karavi/karavi.temp.status` | گزارش‌های وضعیت اجرای موقت (HTML/JSON) (gitignored) — پاک‌شدنی |
| ۸ | `karavi/karavi.temp.deploy` | فایل‌های آماده deploy موقت (gitignored) — پاک‌شدنی |
| ۹ | `karavi/karavi.temp.build` | خروجی‌های موقت فرآیند build (gitignored) — پاک‌شدنی |
| ۱۰ | `karavi/karavi.assets/brand` | دارایی‌های برند، لوگوها و راهنماهای گرافیکی |
| ۱۱ | `karavi/karavi.assets/icons` | آیکون‌ها، SVGها و faviconها |
| ۱۲ | `karavi/karavi.assets/screenshots` | اسکرین‌شات‌ها و تصاویر رابط کاربری |
| ۱۳ | `karavi/karavi.assets/templates` | قالب‌های متنی، مستندات و کدهای پروژه |
| ۱۴ | `karavi/karavi.mockup` | موکاپ‌ها، وایرفریم‌ها و طرح‌های اولیه UI |
| ۱۵ | `karavi/karavi.doc` | مستندات فنی، معماری و راهنماهای پروژه |
| ۱۶ | `karavi/karavi.BusinessModel.Doc` | مستندات مدل کسب‌وکار، استراتژی و اهداف تجاری |
| ۱۷ | `karavi/karavi.Customer.doc` | مستندات شناخت مشتری، پرسونای کاربر و بازخوردها |
| ۱۸ | `karavi/karavi.OnlineContent/SociaMediaContent` | محتواها، بنرها و پست‌های شبکه‌های اجتماعی |
| ۱۹ | `karavi/karavi.OnlineContent/WordPressContent` | مقالات، صفحات و محتواهای سایت وردپرس |
| ۲۰ | `karavi/karavi.OnlineContent/LinkedinConetnt` | مقالات و محتواهای تخصصی لینکدین |

---

## ۳) مهاجرت خودکار ساختارهای قبلی (Migration)

اگر قبلاً در شاخه `karavi/` فولدرهایی با نام‌های قدیمی یا متفرقه ساخته شده باشد، مهارت `karavi-folder` هنگام اجرای `init` یا `create` آنها را به‌صورت خودکار تغییر نام داده و به محل جدید منتقل می‌کند:

| نام قبلی در `karavi/` | مقصد استاندارد جدید |
|---|---|
| `plans`, `prompt`, `prompts`, `karavi.plans`, `karavi.prompt` | `karavi.plans.prompt` |
| `history`, `histories`, `karavi.histories` | `karavi.history` |
| `deploy`, `config`, `deploy.config`, `karavi.deploy` | `karavi.deploy.config` |
| `commands`, `scripts.command`, `karavi.commands`, `scripts/command` | `karavi.scripts.command` |
| `tools`, `scripts.tools`, `karavi.tools`, `scripts/tools` | `karavi.scripts.tools` |
| `logs`, `temp.logs`, `karavi.logs` | `karavi.temp.logs` |
| `status`, `temp.status`, `karavi.status` | `karavi.temp.status` |
| `temp.deploy`, `karavi.deploy.temp` | `karavi.temp.deploy` |
| `build`, `temp.build`, `karavi.build` | `karavi.temp.build` |
| `assets`, `karavi.asset`, `assets/{brand,icons,...}` | `karavi.assets/{brand,icons,screenshots,templates}` |
| `mockup`, `mockups`, `karavi.mockups` | `karavi.mockup` |
| `doc`, `docs`, `karavi.docs`, `documentation` | `karavi.doc` |
| `business`, `businessmodel`, `karavi.business`, `BusinessModel.Doc` | `karavi.BusinessModel.Doc` |
| `customer`, `customers`, `karavi.customer`, `Customer.doc` | `karavi.Customer.doc` |
| `social`, `socialmedia`, `karavi.social`, `SocialMediaContent`, `karavi.SociaMediaContent`, `SociaMediaContent` | `karavi.OnlineContent/SociaMediaContent` |
| `wordpress`, `WordPressContent`, `wordpresscontent` | `karavi.OnlineContent/WordPressContent` |
| `linkedin`, `LinkedinContent`, `LinkedinConetnt`, `linkedincontent` | `karavi.OnlineContent/LinkedinConetnt` |

---

## ۴) دستورها (Commands)

| دستور | اثر |
|---|---|
| `/karavi-folder init` | **مقداردهی اولیه کامل (۲۰ فولدر/زیرفولدر پیش‌فرض) + مهاجرت و تغییر نام فولدرهای قبلی** |
| `/karavi-folder init --core` | مقداردهی اولیه فقط ۹ فولدر پایه + مهاجرت |
| `/karavi-folder create` | ساخت ساختار کامل و مهاجرت (همانند init) |
| `/karavi-folder clean` | پاک‌سازی `karavi.temp.logs` و `karavi.temp.status` |
| `/karavi-folder clean --deep` | پاک‌سازی کامل هر ۴ فولدر temp + کش‌ها + خروجی‌های سطح ریپو |
| `/karavi-folder clean --what-if` | پیش‌نمایش پاک‌سازی بدون حذف هیچ فایلی |
| `/karavi-folder help` | نمایش این راهنما |

### دستورات معادل فارسی
```text
/karavi-folder شروع               # شروع اولیه کامل و مهاجرت فولدرهای قدیمی
/karavi-folder ساخت               # ساخت ساختار کامل
/karavi-folder پاکسازی             # پاک کردن لاگ‌ها و وضعیت موقت
/karavi-folder پاکسازی --عمیق      # پاک‌سازی کامل temp و کش‌ها
/karavi-folder پاکسازی --پیش‌نمایش # پیش‌نمایش پاک‌سازی
```

### دستور همیشگی برای هر ریپو (Always-Use Habit)
```text
/karavi-folder init
```

### اجرای مستقیم از طریق PowerShell
```powershell
# مقداردهی اولیه و مهاجرت ساختار کامل
& "karavi/karavi.scripts.command/karavi-folder.init.ps1"

# ساخت و مهاجرت
& "karavi/karavi.scripts.command/karavi-folder.create.ps1"

# پاک‌سازی عادی
& "karavi/karavi.scripts.command/karavi-folder.clean.ps1"

# پاک‌سازی عمیق و پیش‌نمایش
& "karavi/karavi.scripts.command/karavi-folder.clean.ps1" -Deep
& "karavi/karavi.scripts.command/karavi-folder.clean.ps1" -WhatIf
```

---

## ۵) پاک‌سازی — چه چیزی حذف می‌شود و چه چیزی هرگز حذف نمی‌شود؟

### `clean` (پیش‌فرض)
فقط فایل‌های موقت در `karavi.temp.logs` و `karavi.temp.status`:
`*.out.txt`، `*.err.txt`، `_tmp-*`، `_fix-*`، `*.pid`، توکن/state دامپ‌ها، فایل‌های HTML/JSON موقت.

### `clean --deep`
همه‌ی موارد بالا **به‌علاوه**:
- محتوای `karavi.temp.build/` و `karavi.temp.deploy/`
- خروجی‌های سطح ریپو: `publish/`, `artifacts/`, `.run-logs/`, `LastRunInfo.html`, `Deploy_Summary.html`
- کش‌های بیلد و زبان‌ها: `**/bin/`, `**/obj/`, `**/.dart_tool/`, `.next/`, `dist/`, `out/`, `node_modules/.cache/`

### هرگز حذف نمی‌شوند (Preserve Whitelist)
- سورس‌کد پروژه
- تمام تاریخچه `karavi.history/`
- فایل‌های تنظیمات `karavi.deploy.config/`
- پلن‌ها و پرامپت‌های `karavi.plans.prompt/`
- دستورات و اسکریپت‌ها `karavi.scripts.*`
- مستندات `karavi.doc/`, `karavi.BusinessModel.Doc/`, `karavi.Customer.doc/`, `karavi.OnlineContent/`
- دارایی‌ها و موکاپ‌ها `karavi.assets/`, `karavi.mockup/`
- تمام فایل‌های `README.md` و `.gitkeep`

---

## ۶) سوالات رایج (FAQ)

- **اگر فولدرهای قبلی در `karavi/` داشته باشم، محتوای آن‌ها پاک می‌شود؟**
  خیر! دستور `init` ساختار قبلی را شناسایی کرده و فایل‌ها را با حفظ کامل به فولدر استاندارد جدید انتقال می‌دهد.
- **پیش‌فرض ساختار چند فولدر است؟**
  پیش‌فرض ساختار **کامل (Full - ۱۸ فولدر)** است تا تمام نیازهای معماری، مستندات، موکاپ و تاریخچه پروژه پوشش داده شود.
- **تفاوت `init` و `create` چیست؟**
  هر دو ساختار کامل را می‌سازند و مهاجرت را انجام می‌دهند؛ `init` نقطه ورود اصلی برای راه‌اندازی و بازسازی است.
- **آیا اجرای دوباره `init` خطر دارد؟**
  خیر، کاملاً idempotent است و روی فولدرهای استاندارد موجود هیچ اثر منفی ندارد.
