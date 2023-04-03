namespace TrUtils;

public class TestStatus
{
    public static TestStatus Failed { get; } = new("Failed");
    public static TestStatus BugAqa { get; } = new("Bug (AQA)");
    public static TestStatus Passed { get; } = new("Passed");
    public static TestStatus PassedOnRetest { get; } = new("Retest");
    public static TestStatus PassedLocally { get; } = new("Passed (locally)");
    public string Value { get; }
    private TestStatus(string status) => Value = status;
}