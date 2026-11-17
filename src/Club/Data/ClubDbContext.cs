using Club.Models;
using Microsoft.EntityFrameworkCore;

namespace Club.Data;

public sealed class ClubDbContext : DbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<ComputerStatus> ComputerStatuses => Set<ComputerStatus>();
    public DbSet<SessionStatus> SessionStatuses => Set<SessionStatus>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Computer> Computers => Set<Computer>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<GameSession> Sessions => Set<GameSession>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlServer(Db.GetConnectionString());

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Client>().Property(client => client.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        model.Entity<UserAccount>().Property(user => user.IsActive).HasDefaultValue(true);
        model.Entity<Tariff>().Property(tariff => tariff.IsActive).HasDefaultValue(true);
        model.Entity<Payment>().Property(payment => payment.PaymentTime).HasDefaultValueSql("SYSUTCDATETIME()");

        // SQL Server forbids a bare OUTPUT clause on a table with an enabled trigger.
        model.Entity<Tariff>().ToTable("tariffs", "dbo", table => table.UseSqlOutputClause(false));

        model.Entity<UserAccount>().HasOne<Role>().WithMany().HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.NoAction);
        model.Entity<UserAccount>().HasOne<Client>().WithOne().HasForeignKey<UserAccount>(user => user.ClientId)
            .OnDelete(DeleteBehavior.NoAction);
        model.Entity<Computer>().HasOne<ComputerStatus>().WithMany().HasForeignKey(computer => computer.StatusId)
            .OnDelete(DeleteBehavior.NoAction);
        model.Entity<GameSession>().HasOne<Client>().WithMany().HasForeignKey(session => session.ClientId)
            .OnDelete(DeleteBehavior.NoAction);
        model.Entity<GameSession>().HasOne<Computer>().WithMany().HasForeignKey(session => session.ComputerId)
            .OnDelete(DeleteBehavior.NoAction);
        model.Entity<GameSession>().HasOne<Tariff>().WithMany().HasForeignKey(session => session.TariffId)
            .OnDelete(DeleteBehavior.NoAction);
        model.Entity<GameSession>().HasOne<SessionStatus>().WithMany().HasForeignKey(session => session.StatusId)
            .OnDelete(DeleteBehavior.NoAction);
        model.Entity<Payment>().HasOne<GameSession>().WithOne().HasForeignKey<Payment>(payment => payment.SessionId)
            .OnDelete(DeleteBehavior.NoAction);
        model.Entity<Payment>().HasOne<PaymentMethod>().WithMany().HasForeignKey(payment => payment.PaymentMethodId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
