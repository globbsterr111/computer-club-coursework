using System.Data;
using Avalonia.Controls;
using Avalonia.Layout;
using Club.Services;

namespace Club.Views;

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

    private bool IsAdmin => service.Current?.IsAdministrator == true;

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
        catch (Exception ex)
        {
            StatusLine.Text = UiSupport.ErrorMessage(ex);
        }
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
}
