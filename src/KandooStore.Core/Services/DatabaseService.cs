using SQLite;
using KandooStore.Core.Models;

namespace KandooStore.Core.Services;

/// <summary>
/// دسترسی به پایگاه داده محلی SQLite (local-first).
/// فایل دیتابیس در پوشه داده برنامه ذخیره می‌شود؛ هیچ داده‌ای در v1 به Cloud ارسال نمی‌شود.
/// </summary>
public class DatabaseService
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection? _connection;

    public DatabaseService(string dbPath)
    {
        _dbPath = dbPath;
    }

    public string DbPath => _dbPath;

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        _connection = new SQLiteAsyncConnection(_dbPath);
        await _connection.CreateTableAsync<Product>();
        await _connection.CreateTableAsync<Customer>();
        await _connection.CreateTableAsync<StoreUser>();
        await _connection.CreateTableAsync<StoreSettings>();
        await _connection.CreateTableAsync<Invoice>();
        await _connection.CreateTableAsync<InvoiceLine>();
        await _connection.CreateTableAsync<InvoiceCounter>();
        await _connection.CreateTableAsync<StockEvent>();
        await _connection.CreateTableAsync<DraftLock>();
        await _connection.CreateTableAsync<OutboxEntry>();
        await _connection.CreateTableAsync<ReviewItem>();
        return _connection;
    }

    /// <summary>تنظیمات فروشگاه (تک‌ردیفه)؛ اگر ساخته نشده باشد null برمی‌گرداند.</summary>
    public async Task<StoreSettings?> GetStoreSettingsAsync()
    {
        var conn = await GetConnectionAsync();
        return await conn.Table<StoreSettings>().FirstOrDefaultAsync();
    }

    public async Task SaveStoreSettingsAsync(StoreSettings settings)
    {
        var conn = await GetConnectionAsync();
        var existing = await conn.Table<StoreSettings>().FirstOrDefaultAsync();
        if (existing is null)
            await conn.InsertAsync(settings);
        else
            await conn.UpdateAsync(settings);
    }
}
