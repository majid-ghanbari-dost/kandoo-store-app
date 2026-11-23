# حسابرسی فقط‌خواندنی ریپوهای موجود کندو (Kandoo)

**تاریخ حسابرسی:** ۲۲ نوامبر ۲۰۲۶ (۱ آذر ۱۴۰۵) — ساعت ~۱۱:۳۲ UTC
**روش:** فقط خواندن با GitHub MCP (`list_commits`, `list_pull_requests`, `get_file_contents`) + هندآف جامع `Kandoo_Comprehensive_Handoff_FA_2026-11-22.md` + `docs/PM_LOG.md` در ریپوی مادر.
**هیچ تغییری، push، merge یا ساخت ریپو انجام نشده است.** هیچ فایلی در ریپوهای موجود دست نخورده است.

> نکته مهم درباره دسترسی: دو ریپو (`kandoo-cloud-php` و `kandoo-internal-engine-v1`) از طریق GitHub App موزه ۴۰۴ دادند. طبق `PM_LOG.md` ریپوی PHP «private» است؛ پس ۴۰۴ به‌معنای حذف‌شدن نیست، بلکه یعنی App روی آن ریپوها نصب/انتخاب نشده است. وضعیت آن دو ریپو در این سند فقط از روی هندآف و PM_LOG نقل شده و مستقیم تأیید نشده است (بخش «تأیید نشد» را ببینید).

---

## ۱. خلاصه یک‌خطی هر ریپو

| ریپو | main head (آخرین کامیت) | PR باز | وضعیت کلی |
|---|---|---|---|
| `Kandoo` (C# مادر) | `7cdba446` — ۲۰۲۶-۱۱-۲۲ | ۸ | ORDER 15 در main ادغام شد؛ C-90 تا C-95 و C-93 همه در PR باز، هیچ‌کدام merge نشده‌اند |
| `Kandoo-Digital-Invoice` (Python) | `180b279b` — ۲۰۲۶-۱۱-۱۶ | ۲ | ORDER 16 در PR #16 باز؛ main از ۱۶ نوامبر به‌روز نشده |
| `kandoo-cloud-php` (PHP) | قابل‌خواندن نیست (۴۰۴) | قابل‌خواندن نیست | طبق هندآف: ORDER 15 baseline در main، ORDER 16 در PR #3 به‌صورت Draft/Partial |
| `kando-internal-ai-v01` | `3427ae6a` — ۲۰۲۶-۱۱-۱۲ | ۷ | Build 0.2 تا 0.6 فقط در شاخه‌های PR، هیچ‌کدام merge نشده؛ fixture-only |
| `kandoo-internal-engine-v1` | قابل‌خواندن نیست (۴۰۴) | قابل‌خواندن نیست | طبق هندآف: Phase A کامل، اتصال به Hub نشده |

---

## ۲. ریپو به ریپو

### ۲.۱ `Kandoo` — مادر C# / .NET

**main:**
- Head: `7cdba4461c8adb8ccf149c459e0b068f3079d496` — ۲۰۲۶-۱۱-۲۲T07:04:15Z — «docs: PM->Manager GitHub message channel, M-003 [skip ci]»
- قبل از آن: `0616c63d` (PM_LOG — تصمیم baseline PHP)، `2c576167` (افزودن PM_LOG)، `bb9174ec` (Operating Rules v2)، `5500a7a8` — Merge PR #11 در ۲۰۲۶-۱۱-۲۱: «ORDER 15 close-out — Windows installer, first-run bootstrap fix».
- درخت ریشه (۱۴ مورد): `Kandoo.sln`، `README.md`، `RUN-LOCAL.md`، `AGENT_HANDOFF.md`، `docs/`، `packaging/`، `scripts/`، `spikes/`، `src/`، `tests/`، `tools/`، `.github/`.
- `src/`: `Kandoo.App`، `Kandoo.App.Launcher`، `Kandoo.Capture`، `Kandoo.Cloud`، `Kandoo.Core`، `Kandoo.HolooConnector`، `Kandoo.HolooDiscovery`، `Kandoo.Hub` — و ۷ پروژه تست متناظر در `tests/`.
- `docs/`: `PM_LOG.md`، `OPERATING-RULES-v2.md`، `MASTER_HANDOFF.md`، `DECISIONS.md`، `cloud-api-contract.md`، `openapi.yaml`، `sync-protocol.md`، `orders/`، `reports/`، `vectors/`، `messages/`.

**PRهای باز (۸ مورد، همه ۲۰۲۶-۱۱-۲۱، هیچ‌کدام merge نشده):**

| PR | عنوان / ادعا | شاخه head |
|---|---|---|
| #12 | C-95: جایگزینی WinForms launcher با MAUI SetupWizardPage | `arena/kandoo-c95-maui-launcher` (`c76046aa`) |
| #13 | C-90: Windows store pilot — Returns، صفحات دستگاه/مشتری/تنظیمات | `arena/kandoo-c90-windows-store` (`15aadc62`) |
| #14 | C-91: Android store app — Invoices list، pairing، محصولات، صدور، review queue | `arena/kandoo-c91-android-store` (`39bca968`) |
| #15 | C-92: Customer mode — OTP، فاکتورهای مشتری، role switch | `arena/kandoo-c92-customer-mode` (`992b0a01`) |
| #16 | C-93: سخت‌سازی connector read-only هولو | `arena/kandoo-c93-holoo-connector` (`e73b1c91`) |
| #17 | C-93 fix-1: `HolooSchemaVersion.Known()` | `arena/4bb6cd51-kandoo-c93fix1` (`0d9226b7`) |
| #18 | C-93 fix-2: `HolooIncrementalSync` | `arena/4bb6cd51-kandoo-c93fix2` (`e5705c87`) |
| #19 | C-93 fix-3: `HolooShadowStore` | `arena/4bb6cd51-kandoo-c93fix3` (`f1349e9a`) |

نکته: PRهای #17 تا #19 پایه‌شان `arena/4bb6cd51-kandoo` است، نه `main` — یعنی روی هم چیده شده‌اند و ترتیب merge مهم است.

**انجام‌شده (در main):** ادغام PR #11 (ORDER 15 close-out، نصب‌کننده ویندوز، اصلاح first-run bootstrap)؛ هاب محلی، Core، قرارداد Cloud، Capture، ساختار نصب‌کننده v2 (`{app}\hub\Kandoo.Hub.exe` و `{app}\app\Kandoo.App.exe` طبق هندآف).

**ناتمام:** C-95 (wizard جدید)، C-90 (پایلوت ویندوز)، C-91 (اندروید فروشگاه)، C-92 (حالت مشتری + OTP)، C-93 + سه fix (اتصال هولو)، C-94 stretch (طبق هندآف §۱۲، آخرین اولویت).

**مسدود / نیازمند مالک:** تست واقعی نصب‌کننده روی Windows مالک انجام نشده (artifact `kandoo-hub-c87-setup-exe` طبق PM_LOG)؛ سرویس Windows یا startup task دائمی برای Hub هنوز به‌عنوان قابلیت قطعی ثبت نشده؛ job «Android 10 emulator launch + 15s liveness» در PM_LOG قرمز گزارش شده (`continue-on-error`) و «first verbatim error» هنوز از agent طلبکار است — حداکثر سه push اصلاحی، بعد BLOCKED؛ شاخه PM با نام `pm/order20-installer-layout` باید در stack وظایف C# ادغام شود.

### ۲.۲ `Kandoo-Digital-Invoice` — Python

**main:**
- Head: `180b279b6c692f841cab739577ad4fedeefb77c3` — ۲۰۲۶-۱۱-۱۶T05:50:00Z — Merge PR #15 (Order 11, E-39 تا E-43). یعنی main از ۱۶ نوامبر به‌روز نشده است.
- درخت ریشه (۱۸ مورد): `src/`، `tests/`، `contracts/`، `schemas/`، `specs/`، `registers/`، `deliveries/`، `docs/`، `scripts/`.
- `src/`: `capture`، `reconstruction`، `extraction`، `normalization`، `derivation`، `validation`، `validation_domain`، `canonicalization`، `canonical_assembly`، `digital_invoice`، `issuance`، `identity_resolution`، `product_candidate`، `customer_linking`، `duplicate_flows`، `corpus`، `calibration`، `holoo_spike`، `image_extraction_adapter`، `cloud_contract`، `pipeline` و ده‌ها `run_smoke_*.py`.

**PRهای باز (۲ مورد):**
- **#16 — ORDER 16: E-70 تا E-74**، شاخه `arena/d0587e71-kandoo-digital-invoice`، head `1e620cc1f990`، ساخته ۲۰۲۶-۱۱-۲۱، به‌روز ۲۰۲۶-۱۱-۲۲T07:13:10Z. متن PR ادعا می‌کند: sync commit `08ac0c1be6d9351c92401a8ec377a2c62e83aa29` تعارض با `origin/main` (`180b279b`) را حل کرده و وضعیت «OPEN / CLEAN / MERGEABLE» است؛ CIها: Python run `44923848427`، harness `44923848473`، Windows build `44923848428` — همه SUCCESS طبق متن PR. نتایج ادعایی: E-70 synthetic PASS، E-71 سه layout ساختگی PASS (`EXTRACTED-but-wrong=0`)، E-72 verifier PASS، E-73 fail-closed PASS، E-74 full harness PASS با ۲٬۰۷۶ تست و سه layout عددی ۵/۵.
- **#1 — chore(governance): WP-1.3 artifact received**، ساخته ۲۰۲۶-۱۰-۱۰ — «independent verification passed, source integration pending»؛ یعنی تحویل WP-1.3 (Retention & Recovery) دریافت و راستی‌آزمایی شده (۲۹۱ فایل، manifest مطابقت دارد، ۹۰ تست، regression ۱۵۶۶ تست) ولی **ادغام سورس آن هنوز pending است**.

**انجام‌شده (ادعایی، فقط synthetic):** E-70 تا E-74 در PR #16 با شواهد synthetic؛ قرارداد frozen تغییر نکرده است.

**ناتمام / مسدود:**
- اختلاف هندآف با متن PR: هندآف (و PM_LOG، PM-11) می‌گوید PR #16 هنوز باید تعارض با `main` را حل کند؛ متن PR می‌گوید حل شده و mergeable است. **من mergeability و وضعیت CI را مستقل تأیید نکردم** — ابزار MCP وضعیت CI/check ندارد. قبل از هر کپی/ادغام در ریپوی جدید، باید mergeability و CI سبز همان head بررسی شود.
- E-70/E-73 به ابزار محلی مالک وابسته‌اند (`tools/local_onboarding_eval.py` یا معادل آن) — هنوز باز است؛ طبق PM-06 داده واقعی فاکتور نباید از PC مالک خارج شود، فقط شمارش تجمیعی.
- دقت روی فاکتور واقعی اثبات نشده: سابقه B2-3 (تاریخی): ۴ تصویر واقعی، ۴ مورد REVIEW، ۰ مورد READY، همه با `InvoiceTextParseError`.
- E-63 همچنان `NO GO / DEFERRED`، E-75 deferred، E-65 فقط draft غیرمصوب، هیچ تگ `o16-*` ساخته نشده است.

### ۲.۳ `kandoo-cloud-php` — Cloud PHP

**مستقیم تأیید نشد.** هر سه فراخوانی (`list_commits`، `list_pull_requests`، `get_file_contents`) با ۴۰۴ برگشت خورد. PM_LOG می‌گوید این ریپو private است؛ پس ۴۰۴ یعنی دسترسی App داده نشده، نه حذف ریپو. موارد زیر فقط از هندآف و PM_LOG نقل می‌شود:

- **انجام‌شده (طبق هندآف §۷.۴ و PM-09):** baseline ORDER 15 با PR #4 در ۲۰۲۶-۱۱-۲۲ در `main` ادغام شد؛ merge commit `ad1efce6948f7e3ac4cb26df473507b56808dad4`؛ baseline روی commit `2724969fd85475c777016d54d61abe3789a6773a` و tag `o15-P-25`؛ شامل P-20، P-20b، P-21، P-22، P-23، P-24، P-25 و P-27. P-27 (DeviceBearer) پذیرفته شده است.
- **ناتمام:** PR #3 با عنوان «ORDER 16 PHP consolidated work — draft»، شاخه `arena/f729618f-kandoo-cloud-php`، **Draft/Partial و نباید merge شود**. P-31 تا حدی پیاده شده (OTP، list/detail/logout، shared vectors؛ CI روی PHP 8.1/8.5 و MariaDB 10.6 سبز طبق هندآف)؛ P-30 checklist ناقص (diagnostics/heartbeat/backup/restore)؛ P-32 شروع نشده؛ P-33 پیشنهاد pattern و privacy، هزینه هر SMS باز؛ P-34 بررسی امنیت اولیه، load واقعی host و mitigationها باز. هیچ tag مربوط به ORDER 16 ساخته نشده.
- **مسدود:** P-31 در وضعیت `PENDING-CROSS-LANE` است — ترتیب: shared vectors ← بازبینی مستقل C# lane و PM ← پذیرش رسمی قرارداد/addendum ← Android E2E در انتها. پذیرش P-20 تا P-25 نیازمند demo روی دستگاه مالک است (CI سبز پذیرش نیست). SMS واقعی (Faraz) ارسال/اثبات نشده؛ sandbox واقعی وجود ندارد؛ تست زنده فقط دستی و با اطلاعات محرمانه خارج از Git. میزبان Production انتخاب/خرید نشده؛ متن حقوقی نیازمند بازبینی وکیل است.

### ۲.۴ `kando-internal-ai-v01` — دستیار داخلی

**main:**
- Head: `3427ae6a0f064368673440b2f038256026206f06` — ۲۰۲۶-۱۱-۱۲T17:14:31Z — «docs(handoff): correct status and governance authority in v1.46».
- درخت ریشه (۹ مورد): `Kandoo.InternalAI.sln`، `README.md`، `src/`، `tests/`. `src/`: `Kandoo.InternalAI.Application`، `.Core`، `.Host`، `.Infrastructure`.

**PRهای باز (۷ مورد، همه ۲۰۲۶-۱۱-۱۲، به‌صورت زنجیره‌ای روی هم، هیچ‌کدام merge نشده):**

| PR | Build | ادعا |
|---|---|---|
| #1 | 0.2.1 | Persian query evaluation و ambiguity-safe parsing |
| #2 | 0.2.2 | Ambiguity-safe product clarification |
| #3 | 0.3 | Dynamic store catalog contracts و fixture retrieval |
| #4 | 0.4 | اتصال catalog به Assistant HTTP flow |
| #5 | 0.5 | Versioned Mapping Proposal Export |
| #6 | 0.6 | Proposal-bound Human Mapping Review — ۶۶/۶۶ تست، CI run `43263970614` Success |
| #7 | 0.7 (Draft) | Trust & Authorization Architecture Challenge — **فقط مستندات**، CI run `43264627599` Success |

**انجام‌شده:** Build 0.2 تا 0.6 در شاخه‌های PR (نه در main)، فقط با fixture/synthetic.
**ناتمام / مسدود:** هیچ اتصالی به Hub زنده ندارد؛ داده واقعی فروشگاه ندارد؛ LLM خارجی ندارد؛ write ندارد؛ durable decision registry و revocation ندارد؛ host احراز هویت/authorization کامل ندارد و نباید روی شبکه عمومی expose شود. Build 0.7 فقط challenge مستنداتی است و پیاده‌سازی آن تأیید PO را می‌خواهد.

### ۲.۵ `kandoo-internal-engine-v1` — موتور Rule-based

**مستقیم تأیید نشد** (هر سه فراخوانی ۴۰۴ — دسترسی App داده نشده؛ حذف‌شدن اثبات نمی‌شود). فقط از هندآف §۸.۲:

- .NET 8 / C# 12، Persian Normalizer، intent detector rule-based، GuideStore فایل‌محور Markdown + YAML، StatsExecutor با `IStatsDataSource`، CLI demo؛ ۱۳ intent ثابت (۵ STATIC_GUIDE + ۸ LIVE_STATS).
- **انجام‌شده (گزارش‌شده):** Phase A کامل.
- **ناتمام:** Phase B فقط abstraction و InMemory؛ `HubSqliteStatsDataSource` انجام نشده (نیازمند read API رسمی Hub)؛ Phase C اتصال Windows/Android آینده است.
- **تصمیم باز:** هیچ تصمیم رسمی برای ادغام یا انتخاب نهایی بین این موتور و `kando-internal-ai-v01` در شواهد پیدا نشد — دو موتور مستقل‌اند و نباید یکی تلقی شوند.

---

## ۳. جدول انجام‌شده / ناتمام / مسدود (خلاصه)

| ریپو | انجام‌شده | ناتمام | مسدود / نیازمند مالک |
|---|---|---|---|
| Kandoo | ORDER 15 در main (PR #11)؛ Hub/Core/Cloud/Capture؛ ساختار نصب‌کننده | C-95، C-90، C-91، C-92، C-93 + ۳ fix، C-94 | Android emulator liveness قرمز؛ تست واقعی نصب‌کننده؛ merge شاخه `pm/order20-installer-layout` |
| Kandoo-Digital-Invoice | pipeline capture→canonical؛ E-70 تا E-74 synthetic در PR #16 | ابزار ارزیابی محلی E-70/E-73؛ WP-1.3 source integration (PR #1)؛ E-63/E-75 | دقت فاکتور واقعی؛ تأیید مستقل mergeability/CI PR #16 |
| kandoo-cloud-php | ORDER 15 baseline در main (P-20 تا P-25، P-27) | P-30 تا P-34 (ORDER 16)؛ P-31 ناقص | P-31 PENDING-CROSS-LANE؛ SMS واقعی Faraz؛ demo مالک؛ host تولیدی؛ متن حقوقی |
| kando-internal-ai-v01 | Build 0.2 تا 0.6 در PR (fixture-only) | اتصال Hub؛ registry/revocation؛ auth کامل | Build 0.7 نیازمند تأیید PO؛ نباید expose عمومی شود |
| kandoo-internal-engine-v1 | Phase A (گزارش‌شده) | HubSqliteStatsDataSource؛ Phase C | نیازمند read API رسمی Hub؛ تصمیم انتخاب/ادغام با AI-v01 |

---

## ۴. کارهای لازم برای «پروژه کامل» در ریپوهای جدید و جدا

اصل: **کپی از روی وضعیت موجود، نه تغییر ریپوهای قبلی.** هر ریپوی جدید از یک snapshot مشخص (SHA زیر) کپی می‌شود و کار ناتمام همان‌جا ادامه می‌یابد. ریپوهای قبلی دست نمی‌خورند.

### ۴.۱ لِین C# (اولویت ۱)
1. بررسی و merge کردن PRهای #12 تا #19 به‌ترتیب وابستگی (اول #12، بعد #13/#14/#15، بعد #16 با #17 تا #19) با CI سبز در هر قدم + tag `o16-<task>`.
2. رفع خطای Android 10 emulator liveness با خطای verbatim (حداکثر ۳ push اصلاحی، بعد BLOCKED).
3. ادغام `pm/order20-installer-layout` در stack.
4. تست واقعی نصب‌کننده `Kandoo-Setup-x64.exe` روی Windows مالک (wizard، bootstrap، ورود، بستن/اجرای دوباره).
5. تکمیل C-92 و زنجیره P-31 (بازبینی vectors، پذیرش قرارداد، Android E2E).
6. C-93: اتصال واقعی به Holoo فقط با رضایت مدیر فروشگاه و حساب read-only؛ stock واردشده تا تأیید، unverified.
7. C-94 (stretch) در آخر.

### ۴.۲ لِین Python (اولویت ۲)
1. تأیید مستقل mergeability و CI سبز head فعلی PR #16 (`1e620cc1f990`)؛ در صورت قرمز، merge `origin/main` (نه rebase) و حل تعارض.
2. تکمیل ابزار ارزیابی محلی E-70/E-73 و اجرای آن روی PC مالک (فقط aggregate، بدون خروج داده واقعی).
3. ادغام سورس WP-1.3 (PR #1) یا رد رسمی آن.
4. تصمیم درباره E-63/E-65/E-75 (deferred/draft) و ثبت آن.

### ۴.۳ لِین PHP (اولویت ۳)
1. همگام‌سازی شاخه ORDER 16 با `origin/main` تا PR #3 فقط ORDER 16 را نشان دهد (طبق M-003).
2. تکمیل P-30 تا P-34 با گزارش مستقل هرکدام؛ P-31 تا پایان cross-lane review در وضعیت `PENDING-CROSS-LANE` بماند.
3. تصمیم و تست دستی Faraz SMS (patternها، کلید، خط — secrets خارج از Git).
4. انتخاب میزبان Production فقط پس از launch readiness؛ بازبینی حقوقی متن‌ها.

### ۴.۴ دستیارها (اولویت ۴ — تصمیم مالک لازم است)
1. مالک اول تصمیم بگیرد: یکی از دو موتور انتخاب شود یا هر دو جدا بمانند (بدون این تصمیم، ادغام ممنوع).
2. بعد: اتصال موتور منتخب به Hub (read API رسمی)، durable registry، auth/authorization، و فقط بعد از آن Build 0.7.

### ۴.۵ کارهای فقط مالک (قابل‌واگذاری به agent نیست)
- تست نصب‌کننده روی Windows واقعی؛ اجرای ابزار ارزیابی محلی Python؛ demo پذیرش PHP؛ تست واقعی SMS؛ انتخاب/خرید host؛ تأیید متن حقوقی.

---

## ۵. ریپوهای جدید پیشنهادی (دقیقاً همین نام‌ها را بسازید)

خصوصی‌بودن اجباری نیست — فقط توصیه بود؛ اگر بخواهید می‌توانند عمومی باشند. در هر دو حالت، هیچ secret نباید در Git باشد. دسترسی Muse فقط به همین ریپوهای جدید داده شود، نه به ریپوهای قبلی.

| # | نام پیشنهادی ریپوی جدید | کپی از (snapshot) | شامل چه کاری |
|---|---|---|---|
| ۱ | `kandoo-store-app` | `Kandoo` @ `7cdba446` + PRهای #12 تا #19 | **قبلاً توسط مالک انتخاب شده (۲۰۲۶-۱۱-۲۲).** هسته فروشگاهی: Hub، ویندوز + اندروید، C-90 تا C-95، نصب‌کننده |
| ۲ | `kandoo-invoice-extractor` | `Kandoo-Digital-Invoice` @ `180b279b` + PR #16 (head `1e620cc1`) | Python: تکمیل ORDER 16، ابزار ارزیابی محلی، WP-1.3 |
| ۳ | `kandoo-cloud` | `kandoo-cloud-php` @ baseline `2724969f` (tag `o15-P-25`) + PR #3 | PHP Cloud: تکمیل P-30 تا P-34، Faraz SMS، پذیرش cross-lane |
| ۴ | `kandoo-assistant-ai` | `kando-internal-ai-v01` @ `3427ae6a` + PRهای #1 تا #7 | دستیار AI: Build 0.6 به main، بعد تصمیم Build 0.7 |
| ۵ | `kandoo-assistant-engine` | `kandoo-internal-engine-v1` @ (SHA از مالک — قابل‌خواندن نبود) | موتور rule-based: Phase B/C، اتصال Hub |

> اگر فقط «یک پروژه کامل فروشگاهی» می‌خواهید و دستیارها فعلاً لازم نیستند، ریپوهای ۴ و ۵ را نسازید — ریپوهای ۱ تا ۳ کافی‌اند. ریپوی ۱ به‌تنهایی MVP قابل نصب ویندوز + اندروید است؛ ریپوی ۲ فقط برای Mode B (همراهی با حسابداری موجود / هولو) و ریپوی ۳ فقط برای فاکتور عمومی و مشتری لازم است.

---

## ۶. چه چیزی را نتوانستم تأیید کنم

- **وضعیت CI و mergeability هیچ PRای** — ابزار MCP وضعیت CI/check ندارد؛ همه «سبز»ها نقل‌قول از متن PR یا هندآف‌اند، نه مشاهده مستقیم.
- **ریپوهای `kandoo-cloud-php` و `kandoo-internal-engine-v1`** — هیچ فایل، کامیت یا PRای مستقیم خوانده نشد (۴۰۴؛ App روی آن‌ها نصب نشده). تمام اطلاعات‌شان از هندآف و PM_LOG است. برای حسابرسی مستقیم آن‌ها، مالک باید دسترسی Muse را به همان ریپوهای قبلی هم بدهد (فقط خواندن) — یا SHA دقیق head آن‌ها را بدهد.
- **تعداد دقیق فایل‌ها** — فقط درخت ریشه و `src/` خوانده شد؛ شمارش بازگشتی کل فایل‌ها انجام نشد.
- **محتوای diff هیچ PRای** — فقط عنوان، متن PR و head SHA خوانده شد؛ بررسی خط‌به‌خط diff انجام نشده است.
- **تست فیزیکی** — نصب واقعی، SMS واقعی، فاکتور واقعی و اجرای روی دستگاه مالک هیچ‌کدام قابل‌تأیید از راه دور نیستند و در این حسابرسی «انجام‌شده» محسوب نشده‌اند.
