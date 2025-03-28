using Microsoft.EntityFrameworkCore.Query;
using Xunit.Abstractions;

namespace Issue18.Tests;

public class ExpressionOptimizerTests(ITestOutputHelper testOutputHelper)
{
    private static readonly Client[] AllClientCombinations = GenerateAllClientCombinations().ToArray();

    public static IEnumerable<object[]> TestCases =>
        new ExpressionTestData[]
        {
            new()
            {
                Name = "Remove mutually exclusive condition",
                Input = c => c.IsDeleted || (c.LastName == "John" && c.LastName != "John"),
                Expected = c => c.IsDeleted
            },
            new()
            {
                Name = "Remove always-false conjunction with mutually exclusive condition",
                Input = c => c.IncorrectLoginAttempts > 5 || (c.FirstName == "John" && c.FirstName != "John" && false),
                Expected = c => c.IncorrectLoginAttempts > 5
            },
            new()
            {
                Name = "Simplify always-true disjunction with grouped conditions",
                Input = c =>
                    c.IncorrectLoginAttempts > 5 ||
                    (
                        (!c.IsRestricted && !c.IsAnonymized && !c.IsDeleted) ||
                        (c.IsRestricted && !c.IsAnonymized && !c.IsDeleted) ||
                        (c.IsAnonymized && !c.IsDeleted) ||
                        c.IsDeleted
                    ),
                Expected = c => true
            },
            new()
            {
                Name = "Simplify always-true disjunction with flattened conditions",
                Input = c =>
                    c.IncorrectLoginAttempts > 5 ||
                    (!c.IsRestricted && !c.IsAnonymized && !c.IsDeleted) ||
                    (c.IsRestricted && !c.IsAnonymized && !c.IsDeleted) ||
                    (c.IsAnonymized && !c.IsDeleted) ||
                    c.IsDeleted,
                Expected = c => true
            },
            new()
            {
                Name = "Eliminate tautology inside conjunction",
                Input = c => c.IsRestricted && ((!c.IsAnonymized && c.IsAnonymized) || true),
                Expected = c => c.IsRestricted
            },
            new()
            {
                Name = "Apply distributive simplification",
                Input = c => (c.IsDeleted && c.IsRestricted) || (c.IsDeleted && !c.IsAnonymized),
                Expected = c => c.IsDeleted && (c.IsRestricted || !c.IsAnonymized)
            }
        }.Select(d => new object[] { d });


    [Theory]
    [MemberData(nameof(TestCases))]
    public void OptimizedExpression_ShouldMatchExpected_AndBehaveSame(ExpressionTestData data)
    {
        var optimized = data.Input.Optimize();

        testOutputHelper.WriteLine($"[TEST CASE: {data.Name}]");
        testOutputHelper.WriteLine("Input:");
        testOutputHelper.WriteLine(data.Input.ToString());
        testOutputHelper.WriteLine("\nExpected:");
        testOutputHelper.WriteLine(data.Expected.ToString());
        testOutputHelper.WriteLine("\nOptimized:");
        testOutputHelper.WriteLine(optimized.ToString());

        var expectedResults = AllClientCombinations.Select(data.Expected.Compile()).ToArray();
        var optimizedResults = AllClientCombinations.Select(optimized.Compile()).ToArray();

        Assert.Multiple(() =>
        {
            Assert.True(expectedResults.SequenceEqual(optimizedResults),
                "Optimized expression does not behave the same as expected.");
            Assert.True(ExpressionEqualityComparer.Instance.Equals(optimized, data.Expected),
                "Optimized expression does not match expected.");
        });
    }

    // ReSharper disable once CognitiveComplexity
    private static IEnumerable<Client> GenerateAllClientCombinations()
    {
        var bools = new[] { false, true };
        var incorrectLoginAttempts = new[] { 0, 5, 10 };
        var firstNames = new[] { "John", "Jane", "Albert", null };
        var lastNames = new[] { "Doe", "Smith", null };
        var datesOfBirth = new[] { new DateOnly(1990, 1, 1), new DateOnly(2000, 1, 1) };
        var passwordDates = new[] { DateTime.MinValue, new DateTime(2025, 3, 8, 12, 15, 00), DateTime.MaxValue };

        foreach (var isRestricted in bools)
        foreach (var isAnonymized in bools)
        foreach (var isDeleted in bools)
        foreach (var attempts in incorrectLoginAttempts)
        foreach (var firstName in firstNames)
        foreach (var lastName in lastNames)
        foreach (var dob in datesOfBirth)
        foreach (var pwdDate in passwordDates)
        {
            yield return new()
            {
                IsRestricted = isRestricted,
                IsAnonymized = isAnonymized,
                IsDeleted = isDeleted,
                IncorrectLoginAttempts = attempts,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dob,
                LatestPasswordChangeAtUtc = pwdDate
            };
        }
    }
}