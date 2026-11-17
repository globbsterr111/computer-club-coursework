namespace Club.Data;

internal static class Db
{
    public static string GetConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("CLUB_CONNECTION");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var path = Path.Combine(AppContext.BaseDirectory, "connection.txt");
        if (File.Exists(path))
            return File.ReadAllText(path).Trim();

        throw new InvalidOperationException("Не задана переменная CLUB_CONNECTION или connection.txt");
    }
}
