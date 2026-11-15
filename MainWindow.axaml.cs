using System.Data;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Club;

public sealed partial class MainWindow : Window
{
    private readonly ClubService service;
    private string page = "Компьютеры";
    private DataTable? data;
    private bool busy;

    public MainWindow() : this(new ClubService()) { }

    public MainWindow(ClubService initialService)
    {
        service = initialService;
        InitializeComponent();
        SortBox.ItemsSource = new[] { "По умолчанию", "А → Я / по возрастанию", "Я → А / по убыванию" };
        SortBox.SelectedIndex = 0;
        SearchBox.TextChanged += (_, _) => DisplayRows();
        SortBox.SelectionChanged += (_, _) => DisplayRows();
        FreeOnly.IsCheckedChanged += (_, _) => DisplayRows();
        SignIn.Click += async (_, _) => await Run(async () =>
        {
            try { await service.Login(LoginBox.Text ?? "", PasswordBox.Text ?? ""); }
            finally { PasswordBox.Text = ""; }
            UpdateRole();
            await Load("Компьютеры");
        });
        SignOut.Click += async (_, _) => await Run(async () =>
        {
            service.Logout();
            LoginBox.Text = PasswordBox.Text = "";
            UpdateRole();
            await Load("Компьютеры");
        });
        UpdateRole();
        Opened += async (_, _) => await Run(() => Load("Компьютеры"));
    }

    private bool IsAdmin => service.Current?.Role == 1;

    private Button ActionButton(string title, Func<Task> action)
    {
        var button = new Button { Content = title, Margin = new(0, 0, 8, 6) };
        button.Click += async (_, _) => await Run(action);
        return button;
    }

    private void UpdateRole()
    {
        bool signedIn = service.Current != null;
        RoleLabel.Text = IsAdmin ? "Администратор" : signedIn ? "Клиент" : "Гость";
        LoginBox.IsVisible = PasswordBox.IsVisible = SignIn.IsVisible = !signedIn;
        SignOut.IsVisible = signedIn;
        Nav.Children.Clear();
        Actions.Children.Clear();
        data = null;
        Rows.ItemsSource = null;
        foreach (string name in new[] { "Компьютеры", "Тарифы", "Мои сеансы", "Журнал сеансов", "Клиенты", "Учётные записи", "Выручка" })
        {
            if (name == "Мои сеансы" && (!signedIn || IsAdmin)) continue;
            if (name is "Журнал сеансов" or "Клиенты" or "Учётные записи" or "Выручка" && !IsAdmin) continue;
            var button = ActionButton(name, () => Load(name));
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Classes.Add("nav");
            button.Classes.Set("current", name == page);
            Nav.Children.Add(button);
        }
    }

    private async Task Run(Func<Task> action)
    {
        if (busy) return;
        busy = true;
        IsEnabled = false;
        StatusLine.Text = "Загрузка…";
        try
        {
            await action();
            StatusLine.Text = "Готово";
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            StatusLine.Text = ex.Number switch
            {
                2601 or 2627 => "Запись уже существует, компьютер занят или у клиента есть активный сеанс.",
                547 => "Данные не прошли проверку базы. Проверьте выбранные значения.",
                >= 50000 => ex.Message,
                _ => "Не удалось выполнить запрос. Проверьте подключение и наличие скриптов 01 и 02."
            };
        }
        catch (Exception ex) { StatusLine.Text = ex.GetBaseException().Message; }
        finally { IsEnabled = true; busy = false; }
    }

    private async Task Load(string name)
    {
        var next = await (name switch
        {
            "Компьютеры" => service.Computers(),
            "Тарифы" => service.Tariffs(),
            "Мои сеансы" or "Журнал сеансов" => service.Sessions(),
            "Клиенты" => service.Clients(),
            "Учётные записи" => service.Users(),
            "Выручка" => service.Revenue(),
            _ => throw new ArgumentException("Неизвестный раздел")
        });
        ShowPage(name, next);
    }

    private void ShowPage(string name, DataTable next)
    {
        page = PageTitle.Text = name;
        foreach (var button in Nav.Children.OfType<Button>())
            button.Classes.Set("current", Equals(button.Content, name));
        data = next;
        SearchBox.Text = "";
        SortBox.SelectedIndex = 0;
        FreeOnly.IsVisible = name == "Компьютеры";
        Actions.Children.Clear();
        if (IsAdmin && name == "Компьютеры")
        {
            Actions.Children.Add(ActionButton("Начать сеанс", StartSession));
            Actions.Children.Add(ActionButton("Добавить ПК", AddComputer));
        }
        if (IsAdmin && name == "Журнал сеансов")
        {
            Actions.Children.Add(ActionButton("Завершить выбранный", async () =>
            {
                await service.Finish(SelectedId("session_id"));
                await Load(name);
            }));
            Actions.Children.Add(ActionButton("Оплатить выбранный", PaySession));
        }
        if (IsAdmin && name == "Клиенты")
            Actions.Children.Add(ActionButton("Добавить клиента", AddClient));
        if (IsAdmin && name == "Тарифы")
            Actions.Children.Add(ActionButton("Добавить тариф", AddTariff));
        if (IsAdmin && name == "Учётные записи")
        {
            Actions.Children.Add(ActionButton("Создать запись клиента", AddUser));
            Actions.Children.Add(ActionButton("Отключить выбранную", async () =>
            {
                await service.DisableUser(SelectedId("user_id"));
                await Load(name);
            }));
        }
        DisplayRows();
    }

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

    private sealed record Choice(int Id, string Label) { public override string ToString() => Label; }

    private static ComboBox Choose(string hint, IEnumerable<Choice> choices) =>
        new() { PlaceholderText = hint, ItemsSource = choices.ToArray(), HorizontalAlignment = HorizontalAlignment.Stretch };

    private static int Pick(ComboBox box) => box.SelectedItem is Choice choice ? choice.Id
        : throw new ArgumentException("Заполните все списки выбора");

    private async Task<bool> Form(string title, params Control[] fields)
    {
        var dialog = new Window { Title = title, Width = 400, SizeToContent = SizeToContent.Height,
            CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var panel = new StackPanel { Margin = new(20), Spacing = 10 };
        foreach (var field in fields) panel.Children.Add(field);
        var submit = new Button { Content = "Подтвердить", Classes = { "accent" } };
        var cancel = new Button { Content = "Отмена" };
        submit.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);
        panel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { submit, cancel } });
        dialog.Content = panel;
        IsEnabled = true;
        try { return await dialog.ShowDialog<bool>(this); }
        finally { IsEnabled = false; }
    }

    private async Task StartSession()
    {
        int id = SelectedId("computer_id");
        var clients = Choose("Клиент", (await service.Clients()).Rows.Cast<DataRow>().Select(r => new Choice((int)r["client_id"], ClientName(r) + " · " + r["phone"])));
        var tariffs = Choose("Тариф", (await service.Tariffs()).Rows.Cast<DataRow>().Where(r => (bool)r["is_active"]).Select(r => new Choice((int)r["tariff_id"], $"{r["tariff_name"]} · {r["price_per_hour"]:0.00} ₽/ч")));
        if (!await Form("Начать сеанс", clients, tariffs)) return;
        await service.Start(Pick(clients), id, Pick(tariffs));
        await Load("Журнал сеансов");
    }

    private async Task PaySession()
    {
        int id = SelectedId("session_id");
        var methods = Choose("Способ оплаты", (await service.PaymentMethods()).Rows.Cast<DataRow>().Select(r => new Choice((int)r[0], (string)r[1])));
        if (!await Form("Оплата сеанса", methods)) return;
        await service.Pay(id, Pick(methods));
        await Load(page);
    }

    private async Task AddClient()
    {
        var last = new TextBox { Watermark = "Фамилия", MaxLength = 60 };
        var first = new TextBox { Watermark = "Имя", MaxLength = 60 };
        var phone = new TextBox { Watermark = "Телефон", MaxLength = 20 };
        if (!await Form("Новый клиент", last, first, phone)) return;
        await service.AddClient(last.Text ?? "", first.Text ?? "", phone.Text ?? "");
        await Load(page);
    }

    private async Task AddComputer()
    {
        var name = new TextBox { Watermark = "Название ПК", MaxLength = 30 };
        var zone = new TextBox { Watermark = "Зона", MaxLength = 40 };
        if (!await Form("Новый компьютер", name, zone)) return;
        await service.AddComputer(name.Text ?? "", zone.Text ?? "");
        await Load(page);
    }

    private async Task AddTariff()
    {
        var name = new TextBox { Watermark = "Название тарифа", MaxLength = 60 };
        var price = new NumericUpDown { Minimum = 0.01m, Maximum = 99999999.99m, FormatString = "0.00", Watermark = "Цена за час" };
        if (!await Form("Новый тариф", name, price)) return;
        await service.AddTariff(name.Text ?? "", price.Value ?? 0);
        await Load(page);
    }

    private async Task AddUser()
    {
        var login = new TextBox { Watermark = "Логин", MaxLength = 50 };
        var password = new TextBox { Watermark = "Пароль от 12 символов", PasswordChar = '●' };
        var clients = Choose("Клиент", (await service.Clients()).Rows.Cast<DataRow>().Select(r => new Choice((int)r["client_id"], ClientName(r) + " · " + r["phone"])));
        if (!await Form("Учётная запись клиента", clients, login, password)) return;
        await service.AddUser(login.Text ?? "", password.Text ?? "", Pick(clients));
        await Load(page);
    }
}

