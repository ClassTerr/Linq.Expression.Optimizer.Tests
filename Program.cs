using System.Linq.Expressions;
using LinqKit;
using Microsoft.EntityFrameworkCore.Query;

var statuses = Enum.GetValues<ClientStatus>().Cast<ClientStatus>();

var predicate = PredicateBuilder.New<Client>(false);

foreach (var status in statuses.Distinct())
{
    predicate = status switch
    {
        ClientStatus.Active => predicate.Or(c => !c.IsRestricted && !c.IsAnonymized && !c.IsDeleted),
        ClientStatus.Restricted => predicate.Or(c => c.IsRestricted && !c.IsAnonymized && !c.IsDeleted),
        ClientStatus.Anonymized => predicate.Or(c => c.IsAnonymized && !c.IsDeleted),
        ClientStatus.Deleted => predicate.Or(c => c.IsDeleted),
        _ => throw new ArgumentOutOfRangeException(nameof(statuses), status, $@"Unsupported ClientStatus: {status}")
    };
}
// Optimize expression
var optimized = OptimizeExtension.Optimize<Func<Client, bool>>(predicate);

Console.WriteLine("Original Expression:");
Console.WriteLine(predicate);

Console.WriteLine("\nOptimized Expression:");
Console.WriteLine(optimized);

Expression<Func<Client, bool>> expected = c => true;

Console.WriteLine("\nEquals:");
Console.WriteLine(ExpressionEqualityComparer.Instance.Equals(optimized, expected));

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

public enum ClientStatus
{
    Active,
    Restricted,
    Anonymized,
    Deleted
}