# کندو — نرم‌افزار فروشگاهی (Kandoo Store App)

> **وضعیت:** نسخه **۱٫۲٫۰ نهایی (دمو)** — ادغام‌شده از ریپوی مادر Kandoo ‏(main ‏@ `8059f5f4`،
> ۲۲ نوامبر ۲۰۲۶) با روش خواندن امن (API + فایل ZIP مالک)؛ هیچ clone و هیچ
> اجرای کد قدیمی نبود. جزئیات کامل در `docs/MERGE-NOTES-FA.md`.
> **موارد مبهم (هاست/سرور/پیامک) با داده نمایشی FAKE پر شده‌اند** — طبق دستور
> مالک ۲۲ نوامبر ۲۰۲۶ (۱۹:۵۰): بقیه کار تمام شده است و نقاط مبهم فعلاً
> نمونه‌اند، نه واقعی. هر مورد FAKE در این README و در کد با «FAKE» علامت‌گذاری شده است.
> **تأیید بیلد:** اولین اجرای CI در GitHub Actions (بیلد ویندوز + APK اندروید) است؛
> تا سبز شدن CI، بیلد به‌صورت محلی تأیید نشده است (dotnet در ماشین نویسنده نیست).

## کندو چیست؟

کندو نرم‌افزار مدیریت فروشگاه‌های کوچک و محلی است: تعریف محصول، مدیریت مشتری،
صدور فاکتور دیجیتال و گزارش فروش و موجودی — همه به‌صورت **محلی (Local-first)**
روی دستگاه فروشگاه، بدون وابستگی به اینترنت یا سرور ابری در نسخه اول.

یک کدبیس واحد با **‎.NET MAUI (‎.NET 8)‎** برای **Windows** و **Android**.
رابط کاربری فارسی و راست‌به‌چپ (fa-IR).

## MVP نسخه ۱ — چه چیزی هست

| قابلیت | توضیح |
|---|---|
| محصولات | نام، نوع، رنگ، کد عددی کوتاه (Tag)، قیمت، قیمت خرید (اختیاری)، موجودی، گروه/زیرگروه (دو سطح، اختیاری)، فیلدهای سفارشی؛ تشخیص تکراری دقیق و هشدار نام نزدیک (قانون ProductCatalog ریپوی مادر) |
| مشتریان | نام، شماره موبایل (نرمال‌شده به `+98…`)، سابقه خرید (فاکتورهای صادرشده) |
| فاکتور دیجیتال | پیش‌نویس ← صدور (ISSUE)؛ فاکتور صادرشده تغییرناپذیر است؛ شماره نمایشی فقط در صدور موفق تخصیص می‌یابد؛ snapshot کانونی + هش SHA-256 در صدور ثبت می‌شود؛ ابطال/جایگزینی فقط از Issued |
| موجودی | فقط به‌صورت دفتر رویداد (ledger)؛ هیچ ویرایش مستقیم و بی‌ردپای موجودی نیست؛ هر عملیات `operation_id` + `device_id` دارد و replay آن بی‌اثر است؛ برگشت کالا (ReturnIn) |
| نقش‌ها | فروشنده (فاکتور)، اپراتور (فاکتور + محصول)، مدیر (همه + مشاهده سود فقط برای مدیر)؛ نقش اپلیکیشن Store/Customer/Both (در Windows فقط Store) |
| راه‌اندازی اولیه | ویزارد گرافیکی (GUI) در اولین اجرا؛ ساخت دیتابیس و کاربر مالک؛ بدون ویرایش JSON یا ساخت دستی token؛ Hub نمایشی درون‌حافظه (FakeLocalHub — FAKE) تا برنامه بدون Hub exe به‌صورت end-to-end در حالت دمو کار کند |
| گزارش‌ها | فروش (۳۰ روزه)، پرفروش‌ترین‌ها، کم‌موجودی‌ها، سود (فقط مدیر) |
| صف بازبینی | اقلام استخراج‌شده Mode B (Python/Holoo) با Gate ‏AUTO/REVIEW/REJECT؛ مورد ردشده هرگز صادر نمی‌شود |
| داده | SQLite محلی؛ همگام‌سازی نمایشی (FakeSyncProvider — FAKE): Outbox در فایل لاگ محلی JSON نوشته می‌شود، هیچ ارسال واقعی به Cloud انجام نمی‌شود |
| قفل پیش‌نویس | DraftLockManager به InvoiceService وصل شده است: قفل `(store_id, customer_phone)` با timeout ‏۵ دقیقه؛ تمدید فقط با تغییر پذیرفته‌شده (افزودن ردیف)؛ آزادسازی در صدور/لغو |
| OTP مشتری (دمو) | احراز هویت به‌صورت پیش‌فرض خاموش؛ در حالت نمایشی (روشن‌کردنی فقط توسط Manager) کد ثابت 123456 (FAKE) است و در UI نمایش داده می‌شود؛ پیامک واقعی (Faraz) ارسال نمی‌شود |
| Holoo (دمو) | ماژول فقط‌خواندنی + منبع نمایشی FakeHolooSource (FAKE): ۳ محصول و ۲ مشتری نمونه ساختگی در Shadow Store؛ موجودی آن‌ها unverified است؛ اتصال زنده نیست |
| فونت | وزیرمتن (Vazirmatn) در سبک‌ها — فایل‌های `.ttf` در ریپو نیستند و ثبت `AddFont` کامنت شده است؛ اگر وزیرمتن روی دستگاه نصب باشد استفاده می‌شود، وگرنه فونت سیستمی جایگزین می‌شود (جزئیات در `docs/DESIGN-FA.md`) |
| طراحی | هویت بصری کندو از پروژه‌ی طراحی مالک (`docs/design-canvas/` — ۴۷ فایل): عسلی `#F2A900` دکمه‌ی اصلی، قهوه‌ای تیره `#2B1D0E` متن، کرم `#FFF8E7` پس‌زمینه، موم `#F5E9C8` کارت، سبز `#256B43` موفقیت، قهوه‌ای بازبینی `#9A3B0A` هشدار (نه خطا)؛ **لوگوی اصلی: شش‌ضلعی طلایی براق لایه‌ای** (`docs/brand/Glossy_Golden_Hexagon_Layers.png` — انتخاب مالک، ۲۰۲۶-۱۱-۲۲)، آیکون و اسپلش هر دو پلتفرم Windows و Android؛ نگاشت بورد → صفحه در `docs/DESIGN-FA.md` |
| تاریخ و نام | تاریخ جلالی (Asia/Tehran)؛ نرمال‌سازی نام فارسی (ی/ک) |

## چه چیزی در نسخه ۱ نیست یا ناقص است

- ❌ استخراج فاکتور با Python / OCR — نیست (ریپوی جداگانه `kandoo-invoice-extractor`)
- ❌ Cloud با PHP — نیست (ریپوی جداگانه `kandoo-cloud`)
- ⚠️ اتصال Holoo — فقط ماژول فقط‌خواندنی (نگاشت + Shadow Store + incremental sync)؛ اتصال زنده به SQL Server نیست و credential ندارد؛ داده نمونه با FakeHolooSource (FAKE) بارگذاری می‌شود
- ⚠️ پیامک / SMS (Faraz) — پیامک واقعی ارسال نمی‌شود؛ در حالت نمایشی (دمو) کد ثابت 123456 (FAKE) در UI نمایش داده می‌شود و احراز هویت به‌صورت پیش‌فرض خاموش است
- ⚠️ همگام‌سازی واقعی با Cloud — نیست؛ فقط همگام‌سازی نمایشی (FAKE) در فایل لاگ محلی
- ⚠️ Hub واقعی — فایل اجرایی Hub در این ریپو نیست؛ Hub نمایشی درون‌حافظه (FakeLocalHub — FAKE) جای آن را در حالت دمو می‌گیرد
- ❌ چاپ فاکتور — در v1 فقط نمایش دیجیتال
- ❌ دستیار هوشمند داخلی — نیست

## موارد FAKE (نمایشی) — طبق دستور مالک، با داده ساختگی پر شده‌اند

همه این‌ها به‌وضوح با «FAKE» در کد و UI علامت‌گذاری شده‌اند و هرگز نباید
با نسخه واقعی اشتباه گرفته شوند:

| مورد | مقدار/رفتار نمایشی | جایگزین واقعی |
|---|---|---|
| Hub | FakeLocalHub درون‌حافظه؛ نشست مالک «FAKE-session-…»؛ پذیرش batch فقط در حافظه | فایل اجرایی Hub + HubClient (HTTP به localhost:5000) |
| همگام‌سازی | FakeSyncProvider؛ Outbox در فایل `kandoo-fake-sync.jsonl` در پوشه داده برنامه، با idempotency بر اساس operation_id | Cloud واقعی (kandoo-cloud) + replicator واقعی |
| OTP مشتری | FakeOtpProvider؛ در حالت دمو کد ثابت 123456؛ احراز هویت و حالت دمو به‌صورت پیش‌فرض خاموش‌اند | Faraz SMS + OTP تصادفی (هرگز در UI نمایش داده نمی‌شود) |
| Holoo | FakeHolooSource؛ ۳ محصول و ۲ مشتری نمونه با کد «FAKE-HOLOO-…» و شماره «0900000000x»؛ موجودی unverified | اتصال read-only به SQL Server با حساب مالک و رضایت مدیر فروشگاه |
| هاست/سرور | هیچ هاست یا سروری در این نسخه نیست؛ همه‌چیز محلی (Local-first) است | VPS/هاست اشتراکی پس از launch readiness (تصمیم مالک) |
| فونت | فونت سیستمی؛ Vazirmatn ثبت نشده است | فونت Vazirmatn از ریپوی مادر (OFL) قبل از انتشار برنددار |

## هنوز به تست واقعی نیاز دارد (با CI سبز اثبات نمی‌شود)

- نصب‌کننده Windows (`Kandoo-Setup-x64.exe`) روی Windows واقعی مالک — ویزارد، راه‌اندازی اولیه، بستن/اجرای دوباره
- Hub واقعی (فایل اجرایی) و اتصال کلاینت به آن
- پیامک واقعی Faraz (الگوها، کلید، شماره خط) — فقط با اقدام دستی مالک
- اتصال زنده Holoo به دیتابیس واقعی فروشگاه — فقط با حساب read-only و رضایت مدیر فروشگاه
- Android: نصب APK روی گوشی واقعی و emulator liveness (در CI قرمز/بهترین تلاش است)
- فاکتور واقعی و E2E کامل فروشگاه → Cloud → مشتری

این پروژه ادغام کارهای انجام‌شده ریپوی مادر کندو در یک ریپوی جدید و جداست؛
ریپوهای قبلی هیچ تغییری نکرده‌اند. جزئیات ادغام در `docs/MERGE-NOTES-FA.md`.

## قواعد غیرقابل نقض (در کد اعمال شده‌اند)

1. فاکتور صادرشده immutable است — هر تلاش برای ویرایش، خطا می‌دهد (`InvoiceService.EnsureDraft`).
2. `DisplayNumber` فقط داخل تراکنش موفق ISSUE تخصیص می‌یابد؛ پیش‌نویس و عملیات ناموفق شماره مصرف نمی‌کنند.
3. قفل پیش‌نویس بر اساس `(store_id, customer_phone)` با timeout ‏۵ دقیقه.
4. موجودی فقط از مجموع رویدادهای `StockEvent` تغییر می‌کند؛ `operation_id` یکتا و replay بی‌اثر است.
5. سود و قیمت خرید فقط برای Manager قابل مشاهده است.
6. هیچ secret، رمز واقعی یا شماره تلفن واقعی در مخزن نیست؛ داده نمونه به‌وضوح «دمو» علامت‌گذاری شده است.

## ساخت و نصب

### پیش‌نیاز (برای بیلد محلی)
- ‎.NET 8 SDK‎ — نسخه دقیق در `src/KandooStore.App/global.json` پین شده است
  (`8.0.100`؛ چون `dotnet` پیش‌فرض روی windows-latest SDK 11 است — PM-03)
- MAUI workload: `dotnet workload install maui-windows` (برای ویندوز) / `maui` (برای اندروید)
- برای Android: JDK 17

### بیلد محلی
```bash
cd src/KandooStore.App
# Windows (فقط هدف ویندوز — هرگز -p:TargetFrameworks جمع را استفاده نکنید)
dotnet publish KandooStore.App.csproj -c Release -f net8.0-windows10.0.19041.0 -r win-x64 --self-contained true -p:KandooWindowsOnly=true -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true
# Android APK
dotnet publish KandooStore.App.csproj -c Release -f net8.0-android -p:AndroidPackageFormat=apk
```

### نصب از طریق CI (روش پیشنهادی)
هر push به `main` در GitHub Actions بیلد می‌شود و این artifactها را تولید می‌کند:
- **Windows:** `kandoo-windows-x64` — پوشه publish (خروجی self-contained)
- **Android:** `kandoo-android-apk` — فایل APK
- **نصب‌کننده ویندوز:** workflow `installer.yml` فایل `Kandoo-Setup-x64.exe` را می‌سازد
  (PARTIAL — تست واقعی روی Windows مالک هنوز انجام نشده است)

از تب Actions آخرین run سبز را باز کنید، artifact را دانلود کنید:
- **Windows:** پوشه را باز و فایل اجرایی را اجرا کنید؛ در اولین اجرا ویزارد راه‌اندازی باز می‌شود.
- **Android:** فایل APK را روی گوشی نصب کنید (اجازه نصب از منابع ناشناس لازم است).

## ساختار پروژه

```
KandooStore.sln
src/
  KandooStore.App/        # .NET MAUI — Windows + Android (UI فارسی RTL)
    global.json           # pin SDK 8.0.100 (PM-03)
    MauiProgram.cs        # ثبت DI: DatabaseService و سرویس‌های Core
    App.xaml(.cs)         # ورود: SetupPage اگر راه‌اندازی نشده، وگرنه AppShell
    AppShell.cs           # ناوبری Shell
    MainPage.xaml(.cs)    # خانه
    Pages/                # Products, Customers, Invoices, NewInvoice, InvoiceDetails,
                          # ProductEditor, Returns, ReviewQueue, CustomerInvoices,
                          # RoleSelection, Devices, Reports, Settings, Setup
    Platforms/Android/    # MainActivity, MainApplication, AndroidManifest
    Platforms/Windows/    # App.xaml(.cs), Package.appxmanifest
    Resources/Styles/     # پالت عسلی/قهوه‌ای کندو (docs/DESIGN-FA.md)
    Resources/Images/     # لوگوی اصلی براق (kandoo_logo.png) + جایگزین تخت اندازه‌های کوچک (appicon*.svg, splash.svg)
  KandooStore.Core/       # کتابخانه net8.0 — منطق دامنه، بدون وابستگی به UI
    Models/               # Product, Customer, StoreUser, Invoice, InvoiceLine, StockEvent, DraftLock, OutboxEntry, ReviewItem
    Domain/               # Rials, QuantityMillis, JalaliDates, PhoneNumbers, ProductNames, CanonicalJson, OperationLog, DraftLockManager
    Services/             # DatabaseService, ProductService, InvoiceService, InventoryLedger, ReportService, OutboxService, SeedData, ProductCatalogRules, ReviewQueueService, CustomerAuthService, HubClient, FakeLocalHub (FAKE), FakeSyncProvider (FAKE), FakeOtpProvider (FAKE)
    Services/Holoo/       # HolooColumnMap, HolooReadOnlyConnector (فقط‌خواندنی، اختیاری), FakeHolooSource (FAKE)
packaging/HubPilot/       # نصب‌کننده Inno Setup (layout v2) + BUILD-INSTALLER.md
.github/workflows/build.yml      # بیلد Windows + Android در GitHub Actions (PM-03)
.github/workflows/installer.yml  # ساخت Kandoo-Setup-x64.exe
docs/ARCHITECTURE-FA.md          # معماری به زبان فارسی
docs/MERGE-NOTES-FA.md           # یادداشت ادغام از ریپوی مادر (منبع SHA، DONE/PARTIAL/STUB)
```

## شروع کار (اولین اجرا)

1. برنامه را نصب و اجرا کنید.
2. ویزارد راه‌اندازی: نام فروشگاه، نام و موبایل مالک، رمز عبور مدیر را وارد کنید.
3. (اختیاری) از تنظیمات، «بارگذاری داده نمونه (دمو)» را بزنید تا با داده آزمایشی آشنا شوید.
4. محصول تعریف کنید، مشتری اضافه کنید و اولین فاکتور را صادر کنید.

## English summary

**Kandoo Store App** v1.2.0 (final demo) — a local-first store-management app for small local shops, built with **.NET MAUI (.NET 8)** from a single codebase for **Windows + Android**, Persian RTL UI. Merged from the parent Kandoo repo (`main` @ `8059f5f4`) by safe API-only reading (no clone, no execution of old code); no old repo was modified. Products (short numeric tag, 2-level groups, custom fields, exact/near-duplicate detection), customers with purchase history, digital invoices (Draft → ISSUE only; issued invoices are immutable, with a canonical-JSON snapshot + SHA-256; display numbers assigned only on successful ISSUE; 5-minute draft lock per store+phone wired into InvoiceService via DraftLockManager, extended only on accepted change; revoke/supersede only from Issued), inventory as an event ledger (idempotent, `operation_id` + `device_id`, returns), roles Seller/Operator/Manager (profit visible to Manager only) plus app roles Store/Customer/Both (Windows is Store-only), local SQLite, first-run GUI setup wizard with owner bootstrap, review queue for Mode B extraction (AUTO/REVIEW/REJECT), Jalali dates, Iranian phone normalisation. Ambiguous points (hosting/server/SMS) are filled with clearly-marked FAKE demo data per the owner's 2026-11-22 instruction: FakeLocalHub (in-memory, no network), FakeSyncProvider (outbox written to a local JSON log file, idempotent by operation_id, no real cloud), FakeOtpProvider (fixed demo code 123456, shown in UI only in demo mode; auth off by default), FakeHolooSource (synthetic sample products/customers, read-only, stock unverified, no live connection); system font is used (Vazirmatn not bundled). CI (GitHub Actions, SDK 8 pinned in `global.json`, `maui-windows` workload, `-p:KandooWindowsOnly=true`) builds Windows (win-x64) and an Android APK and uploads them as artifacts; a separate installer workflow builds `Kandoo-Setup-x64.exe` (Inno Setup, layout v2 — PARTIAL, untested on a real Windows machine). No secrets or real data in the repo; seed data is clearly marked demo. Still needs real testing: real Windows installer, real Hub, real Faraz SMS, real Holoo DB, real Android device/emulator.
