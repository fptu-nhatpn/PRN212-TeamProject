using System.IO;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace HorseCare;

internal static class Database
{
    public static SqlConnection CreateConnection()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(settingsPath))
        {
            throw new InvalidOperationException($"Không tìm thấy file cấu hình: {settingsPath}");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
            || !connectionStrings.TryGetProperty("DefaultConnection", out var connectionStringElement)
            || string.IsNullOrWhiteSpace(connectionStringElement.GetString()))
        {
            throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection trong appsettings.json.");
        }

        return new SqlConnection(connectionStringElement.GetString());
    }
}
