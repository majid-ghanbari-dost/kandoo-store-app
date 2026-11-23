# یادداشت ادغام — از ریپوی مادر Kandoo به kandoo-store-app (فارسی)

**تاریخ:** ۲۲ نوامبر ۲۰۲۶ (۱ آذر ۱۴۰۵)
**منبع:** ریپوی مادر `majid-ghanbari-dost/Kandoo`، شاخه `main`
**Source SHA (تأییدشده با GitHub API):** `8059f5f42ae5d2abb16134dc9dccc0ae6057ad18`
— کامیت «docs: design assets index, M-005 (PO feedback, C-96 screens v2) [skip ci]»،
۲۰۲۶-۱۱-۲۲T11:36:54Z. این SHA جدیدتر از `7cdba446` در هندآف و حسابرسی است؛
دو کامیت مستنداتی بعد از حسابرسی (M-004 و M-005) به main اضافه شده‌اند و
تغییر کد نداشته‌اند.
**روش (تأییدشده توسط مالک، ۱۵:۱۴):** فقط خواندن امن — متن فایل‌ها از GitHub API
و از فایل ZIP آپلودشده مالک (`/tmp/kandoo-main-zip/Kandoo-main/`) خوانده شد.
هیچ clone، هیچ دستور git داخل محتوای قدیمی، هیچ اجرای اسکریپت/باینری قدیمی،
هیچ کپی کورکورانه workflow یا اسکریپت نبود. همه فایل‌های جدید با دست در این
ریپو نوشته شده‌اند. هیچ تغییری در هیچ ریپوی قبلی داده نشده است.

## ۱. چه چیزی ادغام شد (فایل‌به‌فایل)

### قراردادهای Core — از `src/Kandoo.Core/` ریپوی مادر

| فایل جدید در این ریپو | منبع قدیمی | وضعیت |
|---|---|---|
| `src/KandooStore.Core/Domain/Money.cs` (Rials + QuantityMillis) | `Kandoo.Core/Rials.cs`، `QuantityMillis.cs` | DONE — منطق همان است |
| `Domain/JalaliDates.cs` | `Kandoo.Core/JalaliDateConverter.cs` | DONE |
| `Domain/ProductNames.cs` | `Kandoo.Core/ProductNameNormalizer.cs` | DONE |
| `Domain/CanonicalJson.cs` | `Kandoo.Core/CanonicalJson.cs` (ساده‌شده) | DONE — هسته کانونی + SHA-256 |
| `Domain/OperationLog.cs` | `Kandoo.Core/OperationLog.cs` | DONE — با شناسه‌های string |
| `Domain/DraftLockManager.cs` | `Kandoo.Core/DraftLockManager.cs` | PARTIAL — موتور قوانین؛ هنوز به InvoiceService وصل نشده |
| `Domain/PhoneNumbers.cs` | `Kandoo.Core/PhoneNormalizer.cs` | DONE با تطبیق — قانون مصنوعی «+» قدیمی برای موبایل واقعی ایرانی مناسب نبود؛ این نسخه «09…» را به «+98…» تبدیل می‌کند |
| `Services/ProductCatalogRules.cs` | `Kandoo.Core/ProductCatalog.cs` | DONE — بررسی تکراری؛ در `ProductService.CreateAsync` وصل شده |
| `Models/Invoice.cs` + `InvoiceService.cs` (snapshot + Revoke/Supersede/CancelDraft) | `Kandoo.Core/Invoice.cs`، `InvoiceService.cs` | DONE — `IssuedPayloadJson`/`IssuedContentSha256` فقط در ISSUE ساخته می‌شوند؛ وضعیت‌های Revoked/Superseded/Extracted/Validated اضافه شدند |
| `Models/ReviewItem.cs` + `Services/ReviewQueueService.cs` | `Kandoo.Hub/ModeBReviewService.cs` (مفهومی) | DONE |
| `Services/CustomerAuthService.cs` (OTP + ISmsSender stub) | `Kandoo.Cloud/SmsContracts.cs`، `CustomerAccountContracts.cs` | STUB — پیامک واقعی (Faraz) نیست؛ احراز هویت پیش‌فرض خاموش است |
| `Services/HubClient.cs` (WaitForHub + OwnerSession + GetFreeTcpPort) | `Kandoo.App/SetupWizardPage.xaml.cs` (C-87) | PARTIAL — Hub exe در این ریپو نیست |
| `Services/Holoo/HolooColumnMap.cs` | `Kandoo.HolooConnector/HolooMapper.cs` | DONE — نگاشت ۱۱ شیء |
| `Services/Holoo/HolooReadOnlyConnector.cs` | تصمیم D-M + PR #16/#17/#18/#19 (توضیحات PR) | PARTIAL — Shadow Store، schema version، incremental sync؛ اتصال زنده نیست |

### صفحات اپ — از `src/Kandoo.App/` ریپوی مادر (مفهومی، بازنویسی با دست)

| صفحه جدید | منبع قدیمی | وضعیت |
|---|---|---|
| `Pages/ReturnsPage` | C-90 (ORDER 16) | DONE — رویداد ReturnIn در دفتر |
| `Pages/DevicesPage` | C-90 | DONE — دستگاه جاری + Outbox |
| `Pages/ReviewQueuePage` | C-91 ReviewQueuePage | DONE |
| `Pages/CustomerInvoicesPage` | C-92 | STUB — OTP با stub؛ پیامک واقعی نیست |
| `Pages/RoleSelectionPage` | `RoleSelectionPage.xaml.cs` | DONE — Windows فقط Store |
| `Pages/InvoiceDetailsPage` | `InvoiceDetailsPage.xaml.cs` | DONE — فقط خواندنی + SHA snapshot + تاریخ جلالی |
| `Pages/ProductEditorPage` | `ProductEditorPage.xaml.cs` | DONE — هشدار تکراری نزدیک |

### بیلد و نصب‌کننده — از `docs/PM_LOG.md` (PM-03/PM-04/PM-05) و `packaging/`

| مورد | منبع | وضعیت |
|---|---|---|
| `src/KandooStore.App/global.json` (SDK 8.0.100) | `src/Kandoo.App/global.json` | DONE |
| `KandooStore.App.csproj`: `KandooWindowsOnly` + `ir.kandoo.app` + نسخه ۱٫۱٫۰ | `Kandoo.App.csproj` | DONE |
| `.github/workflows/build.yml` (working-directory، maui-windows، ‎`-p:KandooWindowsOnly=true`) | PM-03 + `ci.yml`/`kandoo-app.yml` | DONE — بازنویسی با دست |
| `packaging/HubPilot/KandooHub.Installer.iss` + `BUILD-INSTALLER.md` + `README.txt` | `packaging/HubPilot/` | PARTIAL — layout v2؛ پوشه hub تا عرضه جداگانه Hub خالی است |
| `.github/workflows/installer.yml` | جدید برای این ریپو | PARTIAL — تست واقعی روی Windows مالک نشده |

## ۲. PRهای باز #12 تا #19 — چرا خط‌به‌خط merge نشدند

- **#12 (C-95)**، **#13 (C-90)**، **#14 (C-91)**، **#15 (C-92)**: هرکدام ~۱۵۰ کامیت و
  ~۸۰۰۰ خط افزوده روی ~۹۰ فایل، با base قدیمی `bb5ad32b` (قبل از merge ORDER 15).
  این‌ها کپی‌های انباشته‌شده (stack) از همان کار هستند، نه diffهای کوچک و تمیز؛
  وضعیت mergeability آن‌ها `unknown` است و محتوای اصلی‌شان (SetupWizard،
  RoleSelection، ReviewQueue، InvoiceDetails، ProductEditor) همین حالا در
  `main` ریپوی مادر وجود دارد و در بالا ادغام شده است. merge کورکورانه آن‌ها
  کل ریپو را دو بار کپی می‌کرد.
- **#16 (C-93)** + **#17/#18/#19 (fix-1/2/3)**: fixها `clean` و روی base
  `arena/4bb6cd51-kandoo` چیده شده‌اند؛ مفاهیم‌شان (schema version، incremental
  sync، shadow store) در ماژول Holoo بالا پیاده شده است. اتصال زنده به SQL Server
  در این ریپو نیست.
- **نتیجه:** هیچ diffای از PRها خط‌به‌خط کپی نشد؛ فقط قراردادها و مفاهیم
  تأییدشده از `main` و توضیحات PRها استفاده شد.

## ۳. اختلاف‌های مشاهده‌شده (ثبت‌شده، نه پنهان)

1. **OTP:** سیاست قدیمی Cloud (`CustomerOtpPolicy.Default`) حداکثر ۵ تلاش اشتباه
   بود؛ هندآف §۷٫۲ و ORDER 16 می‌گویند حداکثر ۳ تلاش. این ریپو قانون هندآف (۳) را
   اعمال می‌کند.
2. **PhoneNormalizer قدیمی** شماره مصنوعی با «+» می‌خواست؛ این ریپو موبایل واقعی
   ایرانی («09…» ← «+98…») را نرمال می‌کند.
3. **Hub port:** در `main` قدیمی، SetupWizard از پورت ثابت `localhost:5000` استفاده
   می‌کند (C-87)؛ پورت آزاد تصادفی و kill کردن Hub قبلی، C-95/PR #12 است و در
   `main` نیست. `HubClient.GetFreeTcpPort()` مکانیزم C-95 را آماده کرده است.
4. **فونت Vazirmatn:** در ریپوی مادر `Resources/Fonts/Vazirmatn-*.ttf` (OFL) هست،
   ولی فایل باینری کپی نشد؛ `MauiProgram` فعلاً فونت سفارشی ثبت نمی‌کند. قبل از
   انتشار، فونت‌ها از ریپوی مادر اضافه و `ConfigureFonts` ثبت شود.
5. **Android emulator:** در ریپوی مادر job «Android 10 emulator launch + 15s liveness»
   قرمز است (`continue-on-error`) چون روی GitHub-hosted runner KVM نیست. در این
   ریپو هم همان job به‌صورت best-effort و `continue-on-error` هست و هرگز PASS
   معرفی نمی‌شود (R-02).

## ۴. چه چیزی تأیید نشد

- **بیلد:** `dotnet` در این VM نصب نیست؛ هیچ بیلدی اجرا نشد. تأیید بیلد فقط با
  اولین اجرای CI در گیت‌هاب ممکن است (workflow جدید با pin SDK 8 و workload
  maui-windows نوشته شده است).
- **CI سبز، mergeability PRها، تست نصب‌کننده روی Windows واقعی، SMS واقعی،
  فاکتور واقعی، اتصال زنده Holoo، E2E اندروید:** هیچ‌کدام از راه دور قابل تأیید
  نیستند و «انجام‌شده» محسوب نمی‌شوند.
- **Cloud/PHP و Python:** در این ریپو نیستند؛ ریپوهای جداگانه `kandoo-cloud` و
  `kandoo-invoice-extractor` کار خودشان را دارند.

## ۵. وضعیت نهایی این ریپو (DONE / PARTIAL / STUB)

- **DONE:** محصولات + تکراری‌یابی، مشتریان، فاکتور دیجیتال با snapshot و هش،
  دفتر موجودی + برگشت، گزارش‌ها، نقش‌ها، راه‌اندازی اولیه، صف بازبینی،
  صفحات دستگاه/نقش/جزئیات فاکتور، نگاشت Holoo، بیلد CI ویندوز+اندروید،
  نصب‌کننده (اسکریپت)، **قفل پیش‌نویس (DraftLockManager به InvoiceService
  وصل شد — بخش ۶، مورد ۱)**، **همگام‌سازی نمایشی FAKE (بخش ۶، مورد ۴)**،
  **OTP نمایشی FAKE (بخش ۶، مورد ۳)**، **Hub نمایشی FAKE (بخش ۶، مورد ۲)**،
  **Holoo نمایشی FAKE (بخش ۶، مورد ۶)**، **فونت سیستمی (بخش ۶، مورد ۵)**.
- **PARTIAL:** Holoo (بدون اتصال زنده)، نصب‌کننده (بدون تست واقعی)،
  Hub واقعی (فایل اجرایی در این ریپو نیست؛ FakeLocalHub جای آن در دمو است).
- **FAKE (نمایشی، نه واقعی):** Hub نمایشی، همگام‌سازی نمایشی، OTP نمایشی،
  Holoo نمایشی — همه در README (بخش «موارد FAKE») و در کد با «FAKE» علامت‌گذاری شده‌اند.

## ۶. تکمیل نهایی (دمو) — ۲۲ نوامبر ۲۰۲۶، نسخه ۱٫۲٫۰

طبق دستور مالک ۲۲ نوامبر ۲۰۲۶ (۱۹:۵۰): «نسخه نهایی و تمام‌شده را ارائه بده؛
نقاط مبهم مثل هاستینگ، سرور یا پلتفرم ارسال پیامک فعلاً با اطلاعات ساختگی
پر شود؛ مهم بقیه کار است.» موارد زیر در همین ریپو و به‌صورت محلی انجام شد؛
هیچ push به GitHub انجام نشده است.

1. **قفل پیش‌نویس — DONE.** `DraftLockManager` به `InvoiceService` وصل شد:
   `CreateDraftAsync` شماره مشتری را نرمال می‌کند و قفل در حافظه
   `(store_id, customer_phone)` با timeout ‏۵ دقیقه می‌گیرد (در کنار قفل
   پایدار جدول `draft_locks`)؛ `AddLineAsync` با تغییر پذیرفته‌شده
   (افزودن ردیف، با deviceId مالک قفل) قفل را تمدید می‌کند؛
   `IssueAsync`/`CancelDraftAsync` قفل را آزاد می‌کنند
   (دلیل Issue/Cancel). heartbeat غیرفعال همچنان تمدید نمی‌کند.
2. **Hub نمایشی — DONE (FAKE).** `Services/FakeLocalHub.cs` جدید: شبیه‌سازی
   درون‌حافظه Hub بدون شبکه و بدون Hub exe — سلامت همیشه true، نشست مالک
   «FAKE-session-…»، پذیرش batch با idempotency بر اساس operation_id
   (تکرار با محتوای متفاوت OPERATION_ID_CONFLICT). `HubClient` واقعی
   (HTTP) دست نخورده باقی مانده است؛ FakeLocalHub جایگزین آن نیست.
   در `MauiProgram` ثبت و در `DevicesPage` نمایش داده می‌شود.
3. **OTP نمایشی — DONE (FAKE).** `FakeOtpProvider : ISmsSender` جدید
   (پیام‌ها فقط در حافظه، شناسه «FAKE-sms-…»، idempotency مثل stub قبلی) و
   حالت دمو در `CustomerAuthService`: `DemoMode` (پیش‌فرض خاموش، فقط
   Manager روشن می‌کند، فقط در حافظه) — وقتی روشن است کد ثابت **123456**
   است و در UI نمایش داده می‌شود؛ وقتی خاموش است کد تصادفی است و
   `DemoLastCode` همیشه null می‌ماند. احراز هویت (`Enabled`) همچنان
   به‌صورت پیش‌فرض خاموش است و حالت دمو آن را روشن نمی‌کند. هر دو
   کلید در `SettingsPage` قابل تغییرند (فقط Manager در لایه سرویس).
4. **همگام‌سازی نمایشی — DONE (FAKE).** `Services/FakeSyncProvider.cs`
   جدید: ردیف‌های Outbox را در فایل لاگ محلی JSON
   (`kandoo-fake-sync.jsonl` در پوشه داده برنامه) می‌نویسد؛ هر خط شامل
   `"provider":"FAKE-SYNC"` و `"real_cloud":false` است؛ idempotency بر
   اساس operation_id حتی بین اجراها (خواندن operation_idهای قبلی از فایل).
   `OutboxService.SyncNowAsync` حالا `FakeSyncResult` برمی‌گرداند و
   ردیف‌های نوشته‌شده را Sent می‌کند؛ بدون FakeSyncProvider همچنان
   NotSupportedException می‌دهد. دکمه همگام‌سازی نمایشی در `DevicesPage` است.
5. **فونت — DONE (سیستمی).** هیچ ارجاعی به Vazirmatn در کد/XAML/csproj
   نبود؛ `MauiProgram` هیچ فونت سفارشی ثبت نمی‌کند و از فونت سیستمی
   استفاده می‌شود. این تصمیم در `MauiProgram.cs` و README ثبت شد؛
   فونت Vazirmatn (OFL) در ریپوی مادر هست ولی باینری آن کپی نشده است و
   قبل از انتشار برنددار باید اضافه و با ConfigureFonts ثبت شود.
6. **Holoo نمایشی — DONE (FAKE).** `Services/Holoo/FakeHolooSource.cs`
   جدید: ۳ محصول و ۲ مشتری نمونه ساختگی (کد «FAKE-HOLOO-…»، شماره
   «0900000000x») در Shadow Store نمایشی؛ موجودی unverified است؛ هیچ
   اتصال، رشته اتصال یا رمزی در کار نیست و هیچ نوشتن‌ای به Holoo انجام
   نمی‌شود. دکمه بارگذاری در `SettingsPage` است. اتصال زنده (SyncLiveAsync)
   همچنان پیاده نشده است.
7. **نسخه:** `ApplicationDisplayVersion` به ۱٫۲٫۰ و `ApplicationVersion`
   به ۳ افزایش یافت؛ برچسب نسخه در `SettingsPage` هم به‌روز شد.

### هنوز به تست واقعی نیاز دارد (با CI سبز اثبات نمی‌شود)

نصب‌کننده Windows روی Windows واقعی مالک؛ Hub واقعی؛ پیامک واقعی Faraz؛
اتصال زنده Holoo با حساب read-only و رضایت مدیر فروشگاه؛ نصب APK روی
گوشی واقعی و emulator liveness؛ فاکتور واقعی و E2E کامل.
