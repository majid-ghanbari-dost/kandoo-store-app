using KandooStore.App.Pages;
using KandooStore.Core.Services;

namespace KandooStore.App;

public partial class App : Application
{
    private readonly DatabaseService _db;
    private readonly IServiceProvider _services;

    public App(DatabaseService db, IServiceProvider services)
    {
        InitializeComponent();
        _db = db;
        _services = services;
    }

    protected override async void OnStart()
    {
        base.OnStart();
        // راه‌اندازی اولیه: اگر فروشگاه هنوز تنظیم نشده، ویزارد نصب نمایش داده می‌شود.
        var settings = await _db.GetStoreSettingsAsync();
        MainPage = settings is { SetupCompleted: true }
            ? new AppShell(_services)
            : new NavigationPage(new SetupPage(_db));
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Title = "کندو | Kandoo";
        return window;
    }
}
