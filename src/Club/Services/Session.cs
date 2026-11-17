namespace Club.Services;

public sealed record Session(int UserId, int RoleId)
{
    public bool IsAdministrator => RoleId == 1;
}
