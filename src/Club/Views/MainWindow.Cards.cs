using System.Data;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;

namespace Club.Views;

public sealed partial class MainWindow
{
    private sealed record Card(DataRow Row, string Title, string Details);
    private static string Value(DataRow row, string key) => Convert.ToString(row[key], CultureInfo.CurrentCulture) ?? "";
    private static string ClientName(DataRow row) => $"{Value(row, "last_name")} {Value(row, "first_name")}";

    private Card Describe(DataRow row) => page switch
    {
        "Компьютеры" => new(row, Value(row, "computer_name"),
            $"{row["zone_name"]} · {row["status_name"]} · {row["occupancy"]}"),
        "Тарифы" => new(row, Value(row, "tariff_name"),
            $"{row["price_per_hour"]:0.00} ₽/час · {((bool)row["is_active"] ? "Действует" : "Архив")}"),
        "Журнал сеансов" or "Мои сеансы" => new(row,
            $"Сеанс №{row["session_id"]} · {row["computer_name"]}",
            $"{row["client_name"]} · {row["tariff_name"]} · {row["start_time"]:dd.MM HH:mm} (UTC) · " +
            (row.IsNull("end_time") ? "Активен" : $"{row["total_cost"]:0.00} ₽ · {((bool)row["is_paid"] ? "Оплачен" : "К оплате")}")),
        "Клиенты" => new(row, ClientName(row), Value(row, "phone")),
        "Учётные записи" => new(row, Value(row, "login"),
            $"{((int)row["role_id"] == 1 ? "Администратор" : "Клиент")} · {((bool)row["is_active"] ? "Активна" : "Отключена")}"),
        "Выручка" => new(row, $"{row["payment_date"]:dd.MM.yyyy}", $"{row["revenue"]:0.00} ₽ · оплат: {row["payments_count"]}"),
        _ => throw new InvalidOperationException()
    };

    private void DisplayRows()
    {
        if (data == null) return;
        string query = SearchBox.Text?.Trim() ?? "";
        var cards = data.Rows.Cast<DataRow>()
            .Where(r => page != "Компьютеры" || FreeOnly.IsChecked != true ||
                (Value(r, "occupancy") == "Свободен" && (int)r["computer_status_id"] == 1))
            .Select(Describe)
            .Where(c => (c.Title + " " + c.Details).Contains(query, StringComparison.CurrentCultureIgnoreCase));
        cards = SortBox.SelectedIndex switch
        {
            1 => cards.OrderBy(c => c.Title, StringComparer.CurrentCultureIgnoreCase),
            2 => cards.OrderByDescending(c => c.Title, StringComparer.CurrentCultureIgnoreCase),
            _ => cards
        };
        var visible = cards.ToArray();
        RowCount.Text = $"Записей: {visible.Length}";
        EmptyState.IsVisible = visible.Length == 0;
        EmptyState.Text = string.IsNullOrEmpty(query) ? "В этом разделе пока нет записей" : "По вашему запросу ничего не найдено";
        Rows.ItemsSource = visible.Select(c => new ListBoxItem
        {
            Tag = c.Row,
            Background = SolidColorBrush.Parse("#F3F7FD"),
            Content = new StackPanel
            {
                Spacing = 4, Margin = new(12, 9),
                Children =
                {
                    new TextBlock { Text = c.Title, FontWeight = FontWeight.SemiBold },
                    new TextBlock { Text = c.Details, Foreground = Brushes.DimGray, TextWrapping = TextWrapping.Wrap }
                }
            }
        }).ToArray();
    }

    private int SelectedId(string column) =>
        Rows.SelectedItem is ListBoxItem { Tag: DataRow row } ? (int)row[column]
        : throw new ArgumentException("Сначала выберите строку");
}
