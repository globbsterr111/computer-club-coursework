using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Club.Data;

internal static class ClubData
{
    private const string SessionQuery = """
        SELECT s.*, c.computer_name, t.tariff_name, CONCAT(cl.last_name, N' ', cl.first_name) AS client_name
        FROM dbo.v_session_costs s
        JOIN dbo.computers c ON c.computer_id = s.computer_id
        JOIN dbo.tariffs t ON t.tariff_id = s.tariff_id
        JOIN dbo.clients cl ON cl.client_id = s.client_id
        """;

    public static Task<DataTable> Computers() => Query("""
        SELECT c.computer_id, c.computer_name, c.zone_name, c.computer_status_id, cs.status_name,
            CASE WHEN s.session_id IS NULL THEN N'Свободен' ELSE N'Занят' END AS occupancy
        FROM dbo.computers c
        JOIN dbo.computer_statuses cs ON cs.computer_status_id = c.computer_status_id
        LEFT JOIN dbo.sessions s ON s.computer_id = c.computer_id AND s.end_time IS NULL
        ORDER BY c.computer_name
        """);

    public static Task<DataTable> Tariffs(bool includeArchived) => Query("""
        SELECT tariff_id, tariff_name, price_per_hour, is_active
        FROM dbo.tariffs
        WHERE is_active = 1 OR @p0 = 1
        ORDER BY tariff_name
        """, includeArchived ? 1 : 0);

    public static Task<DataTable> PaymentMethods() =>
        Query("SELECT payment_method_id, method_name FROM dbo.payment_methods ORDER BY payment_method_id");

    public static Task<DataTable> Sessions() => Query(SessionQuery + " ORDER BY s.session_id DESC");

    public static Task<DataTable> SessionsForClient(int clientId) =>
        Query(SessionQuery + " WHERE s.client_id = @p0 ORDER BY s.session_id DESC", clientId);

    public static Task<DataTable> Revenue() => Query("""
        SELECT CAST(payment_time AS date) AS payment_date, COUNT(*) AS payments_count, SUM(amount) AS revenue
        FROM dbo.payments
        GROUP BY CAST(payment_time AS date)
        ORDER BY payment_date DESC
        """);

    public static Task<DataTable> Clients() => Query("SELECT * FROM dbo.clients ORDER BY last_name, first_name");

    public static Task<DataTable> Users() =>
        Query("SELECT user_id, login, role_id, client_id, is_active FROM dbo.users ORDER BY login");

    public static Task StartSession(int clientId, int computerId, int tariffId) =>
        Execute("EXEC dbo.start_session @p0, @p1, @p2", clientId, computerId, tariffId);

    public static Task FinishSession(int sessionId) =>
        Execute("EXEC dbo.finish_session @p0", sessionId);

    public static Task PaySession(int sessionId, int methodId) =>
        Execute("EXEC dbo.pay_session @p0, @p1", sessionId, methodId);

    private static async Task<DataTable> Query(string sql, params object[] values)
    {
        await using var db = new ClubDbContext();
        await db.Database.OpenConnectionAsync();
        await using var command = CreateCommand(db, sql, values);
        await using var reader = await command.ExecuteReaderAsync();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private static async Task Execute(string sql, params object[] values)
    {
        await using var db = new ClubDbContext();
        await db.Database.OpenConnectionAsync();
        await using var command = CreateCommand(db, sql, values);
        await command.ExecuteNonQueryAsync();
    }

    private static DbCommand CreateCommand(ClubDbContext db, string sql, object[] values)
    {
        var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        for (int index = 0; index < values.Length; index++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@p" + index;
            parameter.Value = values[index];
            command.Parameters.Add(parameter);
        }

        return command;
    }
}
