# معماری کندو — MVP نسخه ۱ (فارسی)

## ۱. اصل کلی: Local-first

- مرجع داده، **دیتابیس SQLite محلی** روی دستگاه فروشگاه است (`kandoo.db3` در پوشه داده برنامه).
- برنامه بدون اینترنت کار می‌کند. در نسخه ۱ هیچ داده‌ای به Cloud ارسال نمی‌شود.
- مفهوم **Store Hub** (مرجع عملیاتی فروشگاه در شبکه محلی) در این نسخه با همان دیتابیس محلی
  پیاده شده است؛ Hub جداگانه (سرویس/سرور محلی) در نسخه‌های بعد اضافه می‌شود.
- جدول `Outbox` عملیات را برای همگام‌سازی آینده ثبت می‌کند؛ در نسخه ۱٫۲٫۰
  `OutboxService.SyncNowAsync` با FakeSyncProvider (FAKE) آن‌ها را در فایل
  لاگ محلی JSON می‌نویسد (idempotency بر اساس operation_id)؛ هیچ ارسال
  واقعی به Cloud انجام نمی‌شود.

## ۲. مدل داده

| جدول | توضیح |
|---|---|
| `products` | محصول: نام، نوع، رنگ، Tag یکتا، قیمت، قیمت خرید (اختیاری)، گروه/زیرگروه، فیلد سفارشی (JSON)، `BaseVersion` برای کنترل همزمانی |
| `customers` | مشتری: نام، موبایل (کلید ارتباط با فاکتور) |
| `store_users` | کاربر فروشگاه: نقش، هش رمز (هرگز رمز خام ذخیره نمی‌شود)، مالک |
| `store_settings` | تک‌ردیفه: نام فروشگاه، `StoreId`، `DeviceId`، وضعیت راه‌اندازی |
| `invoices` + `invoice_lines` | فاکتور و ردیف‌ها؛ قیمت واحد در لحظه صدور قفل می‌شود |
| `invoice_counters` | شمارنده صدور به‌ازای هر فروشگاه |
| `stock_events` | دفتر رویداد موجودی؛ `OperationId` یکتا، `DeviceId`، هش زنجیره‌ای |
| `draft_locks` | قفل پیش‌نویس `(StoreId, CustomerPhone)` با انقضای ۵ دقیقه |
| `outbox` | عملیات ثبت‌شده برای replication آینده (ارسال‌نشده) |

## ۳. قواعد غیرقابل نقض و محل اعمال آن‌ها

1. **فاکتور صادرشده تغییرناپذیر است** — `InvoiceService.EnsureDraft` در ابتدای هر ویرایش/صدور.
2. **`DisplayNumber` فقط در ISSUE موفق** — تخصیص شماره و افزایش `InvoiceCounter` فقط داخل
   تراکنش `IssueAsync` انجام می‌شود؛ پیش‌نویس `DisplayNumber = null` دارد.
3. **قفل پیش‌نویس** — `CreateDraftAsync` پیش‌نویس فعال دیگری برای همان
   `(StoreId, CustomerPhone)` را رد می‌کند؛ قفل پس از صدور آزاد و پس از ۵ دقیقه منقضی می‌شود.
   قفل در حافظه (`DraftLockManager`) به `InvoiceService` وصل شده است و فقط
   با تغییر پذیرفته‌شده (افزودن ردیف) تمدید می‌شود.
4. **موجودی = دفتر رویداد** — ستون `Product.Stock` فقط داخل تراکنش `InventoryLedger.AppendAsync`
   (و تراکنش صدور فاکتور) تغییر می‌کند؛ هیچ مسیر دیگری Stock را نمی‌نویسد.
   `OperationId` تکراری در `AppendAsync` بدون اثر برگردانده می‌شود (idempotent replay).
5. **نقش‌ها** — `Roles` در `Models/Enums.cs`: تعریف محصول فقط Operator/Manager،
   ویرایش قیمت ردیف فقط Operator/Manager، سود فقط Manager.
6. **همزمانی محصول** — ویرایش محصول `BaseVersion` را چک می‌کند؛ ناسازگار باشد خطا می‌دهد.

## ۴. لایه‌ها

```
KandooStore.App  (MAUI UI — صفحات فارسی RTL)
      │  فقط از طریق سرویس‌ها صدا می‌زند؛ مستقیم به SQLite دست نمی‌زند
      ▼
KandooStore.Core (Models + Services — بدون وابستگی به UI)
      │  SQLite از طریق sqlite-net-pcl
      ▼
SQLite محلی (kandoo.db3)
```

## ۵. چرخه صدور فاکتور

```
ایجاد پیش‌نویس (قفل ۵ دقیقه‌ای) → افزودن ردیف‌ها (قیمت قفل‌شده)
→ ISSUE در یک تراکنش:
   ۱) تخصیص DisplayNumber و افزایش شمارنده
   ۲) ثبت رویداد SaleOut و کسر موجودی برای هر ردیف
   ۳) آزادسازی قفل پیش‌نویس
   ۴) ثبت در Outbox
→ فاکتور Issued و immutable
```

## ۶. چه چیزی در v1 نیست

Python/extractor، PHP Cloud، SMS واقعی، چاپ، همگام‌سازی واقعی،
دستیار هوشمند. موارد مبهم با داده نمایشی FAKE پر شده‌اند (نسخه ۱٫۲٫۰):
FakeLocalHub (درون‌حافظه)، FakeSyncProvider (لاگ محلی JSON)،
FakeOtpProvider (کد ثابت 123456 فقط در حالت دمو)، FakeHolooSource
(نمونه‌های ساختگی، فقط‌خواندنی). هرکدام که واقعی شود، باید از مرزهای
همین سند عبور کند: قرارداد نسخه‌دار، `operation_id` یکتا، idempotency
و عدم تغییر فاکتور صادرشده.

## ۷. ادغام از ریپوی مادر (نسخه ۱٫۱٫۰، ۲۰۲۶-۱۱-۲۲)

منبع: ریپوی مادر `majid-ghanbari-dost/Kandoo`، main با SHA
`8059f5f42ae5d2abb16134dc9dccc0ae6057ad18` (تأییدشده با GitHub API؛ جدیدتر از
`7cdba446` در هندآف). ادغام فقط با خواندن متن فایل‌ها (GitHub API و فایل ZIP
آپلودشده مالک) انجام شد؛ هیچ clone، هیچ git و هیچ اجرای کد قدیمی نبود.
جزئیات کامل در `docs/MERGE-NOTES-FA.md`.

قراردادهایی که از آنجا آمده‌اند:

- **Rials / QuantityMillis** — پول فقط عدد صحیح ریال؛ مقدار فقط هزارم واحد.
- **CanonicalJson + SHA-256** — snapshot صادرشده فاکتور (`IssuedPayloadJson` و
  `IssuedContentSha256`) فقط در ISSUE ساخته می‌شود؛ فاکتور صادرشده Revoke یا
  Supersede می‌شود، نه ویرایش.
- **OperationLog** — replay همان `operation_id` با همان هش، پاسخ قبلی را بدون
  اثر تکراری برمی‌گرداند؛ محتوای متفاوت، `OPERATION_ID_CONFLICT` است.
- **DraftLockManager** — تمدید قفل فقط با تغییر پذیرفته‌شده؛ heartbeat غیرفعال
  تمدید نمی‌کند. (از نسخه ۱٫۲٫۰ به InvoiceService وصل شده است؛ قفل پایدار
  همچنان جدول `draft_locks` است.)
- **ProductCatalogRules** — نام تکراری دقیق رد، نام نزدیک هشدار، Tag تکراری رد.
- **JalaliDateConverter / PhoneNumbers / ProductNameNormalizer** — تاریخ جلالی
  با Asia/Tehran؛ موبایل ایرانی به `+98…`؛ نرمال‌سازی نام فارسی.
- **CustomerAuthService (FAKE — دمو)** — OTP شش‌رقمی، ۳۰۰ ثانیه، حداکثر ۳ تلاش اشتباه،
  ۳ صدور در ۱۵ دقیقه؛ پیامک واقعی پیاده نشده و احراز هویت به‌صورت پیش‌فرض خاموش است.
  در حالت نمایشی (فقط Manager، فقط در حافظه) کد ثابت 123456 است و در UI نمایش
  داده می‌شود؛ در حالت عادی کد تصادفی است و نمایش داده نمی‌شود.
- **Holoo (OPTIONAL, PARTIAL + FAKE دمو)** — نگاشت ۱۱ شیء، Shadow Store با stock unverified،
  همگام‌سازی افزایشی؛ اتصال زنده و credential در این ریپو نیست. FakeHolooSource
  داده نمونه ساختگی (FAKE) بارگذاری می‌کند.
- **HubClient (PARTIAL) + FakeLocalHub (FAKE)** — Hub محلی `http://localhost:5000` با OwnerBootstrapProof؛
  پورت آزاد تصادفی (C-95) به‌عنوان متد آماده است. Hub exe در این ریپو نیست؛
  FakeLocalHub درون‌حافظه جای آن را در حالت دمو می‌گیرد.
- **صف بازبینی Mode B** — جدول `review_items` با Gate ‏AUTO/REVIEW/REJECT؛
  مورد ردشده هرگز صادر نمی‌شود.
- **نصب‌کننده** — layout v2: ‏`{app}\app\KandooStore.App.exe`؛ پوشه hub تا
  زمان عرضه جداگانه Hub خالی می‌ماند (PARTIAL).

## ۸. تکمیل نهایی (دمو) — نسخه ۱٫۲٫۰، ۲۰۲۶-۱۱-۲۲

طبق دستور مالک، نقاط مبهم (هاست/سرور/پیامک) با داده نمایشی FAKE پر شدند:

- **FakeLocalHub** — Hub درون‌حافظه؛ سلامت همیشه true، نشست «FAKE-session-…»،
  پذیرش batch با idempotency بر اساس operation_id. HubClient واقعی (HTTP) دست نخورده است.
- **FakeSyncProvider** — Outbox در فایل `kandoo-fake-sync.jsonl` (پوشه داده برنامه)؛
  هر خط `"provider":"FAKE-SYNC"` و `"real_cloud":false` دارد؛ idempotency بین اجراها.
- **FakeOtpProvider + DemoMode** — کد ثابت 123456 فقط در حالت دمو؛ احراز هویت و
  حالت دمو به‌صورت پیش‌فرض خاموش‌اند و فقط Manager تغییرشان می‌دهد.
- **FakeHolooSource** — ۳ محصول و ۲ مشتری نمونه ساختگی، فقط‌خواندنی، stock unverified.
- **فونت** — فونت سیستمی؛ هیچ فونت سفارشی ثبت نمی‌شود (Vazirmatn کپی نشده است).
- **قفل پیش‌نویس** — DraftLockManager به InvoiceService وصل شد (تمدید فقط با تغییر پذیرفته‌شده).

هنوز به تست واقعی نیاز دارد: نصب‌کننده Windows روی Windows واقعی، Hub واقعی،
پیامک واقعی Faraz، اتصال زنده Holoo، نصب APK روی گوشی واقعی. جزئیات در `docs/MERGE-NOTES-FA.md` بخش ۶.
