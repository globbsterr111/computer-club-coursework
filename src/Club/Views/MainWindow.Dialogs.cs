using System.Data;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Club.Views;

public sealed partial class MainWindow
{
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
