namespace DeliveryDost.Domain.Enums;

public static class UserRole
{
    public const string SuperAdmin = "SuperAdmin";
    public const string DPCM = "DPCM";
    public const string DP = "DP";
    public const string DBC = "DBC";
    public const string EC = "EC";
    public const string Inspector = "Inspector";

    public static readonly string[] AllRoles =
    {
        SuperAdmin,
        DPCM,
        DP,
        DBC,
        EC,
        Inspector
    };

    public static bool IsValid(string role)
    {
        return AllRoles.Contains(role);
    }
}
