namespace Issue18.Tests;

public class Client
{
    public bool IsRestricted { get; set; }
    public bool IsAnonymized { get; set; }
    public bool IsDeleted { get; set; }
    public int IncorrectLoginAttempts { get; set; }
    public DateTime LatestPasswordChangeAtUtc { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
}