using System.Linq.Expressions;

namespace Issue18.Tests;

public class ExpressionTestData
{
    public required Expression<Func<Client, bool>> Input { get; init; }
    public required Expression<Func<Client, bool>> Expected { get; init; }
    public required string Name { get; init; }

    public override string ToString()
    {
        return Name;
    }
}