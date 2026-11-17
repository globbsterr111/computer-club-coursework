using Avalonia;
using Club.Services;

namespace Club;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--create-admin")
        {
            var password = Environment.GetEnvironmentVariable("CLUB_ADMIN_PASSWORD")
                ?? throw new InvalidOperationException("Set CLUB_ADMIN_PASSWORD");
            ClubService.CreateAdministrator(args[1], password);
            return;
        }

        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }
}
