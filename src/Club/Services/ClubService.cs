using System.Data;
using Club.Data;
using Club.Models;
using Microsoft.EntityFrameworkCore;

namespace Club.Services;

public sealed class ClubService
{
    private const int Administrator = 1;
    private const int ClientRole = 2;

    public Session? Current { get; private set; }

    public static void CreateAdministrator(string login, string password)
    {
        if (password.Length < 12)
            throw new ArgumentException("Minimum password length is 12");

        using var db = new ClubDbContext();
        db.Users.Add(new UserAccount
        {
            Login = login,
            PasswordHash = Passwords.Hash(password),
            RoleId = Administrator
        });
        db.SaveChanges();
    }

    public async Task Login(string login, string password)
    {
        await using var db = new ClubDbContext();
        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.IsActive && user.Login == login.Trim());

        if (user is null || !Passwords.Verify(password, user.PasswordHash))
            throw new InvalidOperationException("Неверный логин или пароль");

        Current = new Session(user.Id, user.RoleId);
    }

    public void Logout() => Current = null;

    public Task<DataTable> Computers() => ClubData.Computers();

    public Task<DataTable> Tariffs() => ClubData.Tariffs(Current?.IsAdministrator == true);

    public async Task<DataTable> Sessions()
    {
        var current = Current ?? throw new UnauthorizedAccessException("Требуется вход");
        if (current.IsAdministrator)
        {
            await CheckAdministrator();
            return await ClubData.Sessions();
        }

        await using var db = new ClubDbContext();
        int? clientId = await db.Users
            .Where(user => user.Id == current.UserId && user.RoleId == ClientRole && user.IsActive)
            .Select(user => user.ClientId)
            .SingleOrDefaultAsync();

        if (clientId is null)
            throw new UnauthorizedAccessException("Учётная запись недоступна");

        return await ClubData.SessionsForClient(clientId.Value);
    }

    public async Task<DataTable> Clients()
    {
        await CheckAdministrator();
        return await ClubData.Clients();
    }

    public async Task<DataTable> Users()
    {
        await CheckAdministrator();
        return await ClubData.Users();
    }

    public async Task<DataTable> Revenue()
    {
        await CheckAdministrator();
        return await ClubData.Revenue();
    }

    public async Task<DataTable> PaymentMethods()
    {
        await CheckAdministrator();
        return await ClubData.PaymentMethods();
    }

    public async Task Start(int clientId, int computerId, int tariffId)
    {
        await CheckAdministrator();
        await ClubData.StartSession(clientId, computerId, tariffId);
    }

    public async Task Finish(int sessionId)
    {
        await CheckAdministrator();
        await ClubData.FinishSession(sessionId);
    }

    public async Task Pay(int sessionId, int methodId)
    {
        await CheckAdministrator();
        await ClubData.PaySession(sessionId, methodId);
    }

    public async Task AddClient(string last, string first, string phone)
    {
        await CheckAdministrator();
        if (string.IsNullOrWhiteSpace(last) || string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Заполните фамилию, имя и телефон");

        await using var db = new ClubDbContext();
        db.Clients.Add(new Client { LastName = last.Trim(), FirstName = first.Trim(), Phone = phone.Trim() });
        await db.SaveChangesAsync();
    }

    public async Task AddComputer(string name, string zone)
    {
        await CheckAdministrator();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(zone))
            throw new ArgumentException("Заполните название и зону");

        await using var db = new ClubDbContext();
        db.Computers.Add(new Computer { Name = name.Trim(), Zone = zone.Trim(), StatusId = 1 });
        await db.SaveChangesAsync();
    }

    public async Task AddTariff(string name, decimal price)
    {
        await CheckAdministrator();
        if (price <= 0 || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Нужны название и положительная цена");

        await using var db = new ClubDbContext();
        db.Tariffs.Add(new Tariff { Name = name.Trim(), PricePerHour = price });
        await db.SaveChangesAsync();
    }

    public async Task AddUser(string login, string password, int clientId)
    {
        await CheckAdministrator();
        if (string.IsNullOrWhiteSpace(login) || password.Length < 12)
            throw new ArgumentException("Укажите логин и пароль от 12 символов");

        await using var db = new ClubDbContext();
        db.Users.Add(new UserAccount
        {
            Login = login.Trim(),
            PasswordHash = Passwords.Hash(password),
            RoleId = ClientRole,
            ClientId = clientId
        });
        await db.SaveChangesAsync();
    }

    public async Task DisableUser(int id)
    {
        await CheckAdministrator();
        if (id == Current!.UserId)
            throw new ArgumentException("Нельзя отключить собственную запись");

        await using var db = new ClubDbContext();
        await db.Users.Where(user => user.Id == id)
            .ExecuteUpdateAsync(users => users.SetProperty(user => user.IsActive, false));
    }

    private async Task CheckAdministrator()
    {
        var current = Current ?? throw new UnauthorizedAccessException("Требуется вход администратора");
        await using var db = new ClubDbContext();
        bool isAdministrator = await db.Users.AnyAsync(user =>
            user.Id == current.UserId && user.IsActive && user.RoleId == Administrator);

        if (!isAdministrator)
            throw new UnauthorizedAccessException("Нет прав администратора");
    }
}
