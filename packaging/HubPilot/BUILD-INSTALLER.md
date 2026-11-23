# Kandoo Store Windows Installer

## این چیست؟

نصب‌کننده مستقل ویندوز برای نرم‌افزار فروشگاهی کندو، ساخته‌شده با
[Inno Setup](https://jrsoftware.org/isinfo.php) (`KandooHub.Installer.iss`).
نصب‌کننده خروجی self-contained `win-x64` برنامه MAUI را بسته‌بندی می‌کند
(بدون نیاز به .NET runtime روی دستگاه هدف) و در `Program Files\Kandoo` نصب می‌کند.

## چیدمان نصب (هدف: layout v2، PM-04)

```
{app}\app\KandooStore.App.exe   + runtime خود آن
{app}\hub\Kandoo.Hub.exe        + runtime خود آن — در این ریپو نیست
```

دو runtime جدا هرگز در یک پوشه مخلوط نمی‌شوند (یک exe وابسته به framework
کنار hostfxr.dll خودکفا، پیام «install .NET» می‌داد). میانبرها و `[Run]`
به `{app}\app\` اشاره می‌کنند.

> **وضعیت: PARTIAL** — Hub در این ریپو نیست؛ نصب‌کننده فعلاً فقط برنامه
> MAUI را نصب می‌کند. وقتی Hub جداگانه عرضه شد، خروجی publish آن در
> `publish\hub` قرار می‌گیرد و بخش `[Files]` اسکریپت یک ورودی hub می‌گیرد.
> تست واقعی نصب‌کننده روی Windows مالک هنوز انجام نشده است (R-01).

## ساخت

روی Windows با Inno Setup نصب‌شده:

```powershell
# 1. Publish برنامه ویندوز (خودکفا).
dotnet publish src/KandooStore.App/KandooStore.App.csproj `
    -c Release -f net8.0-windows10.0.19041.0 -r win-x64 --self-contained true `
    -p:KandooWindowsOnly=true -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true `
    -o packaging\HubPilot\publish\app

# 2. Compile نصب‌کننده.
iscc packaging\HubPilot\KandooHub.Installer.iss

# 3. نتیجه.
packaging\HubPilot\Output\Kandoo-Setup-x64.exe
```

در CI، workflow `.github/workflows/installer.yml` همین کار را می‌کند و
`Kandoo-Setup-x64.exe` را به‌عنوان artifact آپلود می‌کند. نصب سایلنت:

```powershell
Kandoo-Setup-x64.exe /SILENT
```

## محتویات

- `KandooHub.Installer.iss` — اسکریپت Inno Setup.
- `BUILD-INSTALLER.md` — این فایل.
- `README.txt` — توضیحات همراه نصب‌کننده.

## نکته‌ها

- پوشه `publish\` خروجی بیلد است و commit نمی‌شود؛ CI آن را می‌سازد.
- هیچ secret (مثل `Hub__OwnerBootstrapProof`) در بسته یا ریپو قرار نمی‌گیرد؛
  bootstrap proof فقط در متغیر محیطی زمان اجرا داده می‌شود.
- داده فروشگاه (دیتابیس SQLite) در پوشه داده برنامه ذخیره می‌شود، نه در پوشه نصب.
