using Microsoft.Data.SqlClient;

namespace Club.Views;

internal static class UiSupport
{
    public static string ErrorMessage(Exception exception)
    {
        var cause = exception.GetBaseException();
        if (cause is not SqlException sql)
            return cause.Message;

        return sql.Number switch
        {
            2601 or 2627 => "Запись уже существует, компьютер занят или у клиента есть активный сеанс.",
            547 => "Данные не прошли проверку базы. Проверьте выбранные значения.",
            >= 50000 => sql.Message,
            _ => "Не удалось выполнить запрос. Проверьте подключение и наличие скриптов 01 и 02."
        };
    }
}
