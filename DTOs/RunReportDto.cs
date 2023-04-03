using System.Collections.Generic;

namespace TrUtils.DTOs;

public class RunReportDto
{
    public string ProjectName { get; set; }
    public string Date { get; set; }
    public string RunName { get; set; }
    public int RunId { get; set; }
    public int FailedTests { get; set; }
    public int PassedTests { get; set; }
    public int PassedOnRetest { get; set; }
    public int BugTests { get; set; }
    public int FlakyTests { get; set; }
    public int FailEveryDayTests { get; set; }
    public int FailedFirstTimeTests { get; set; }
    public int FailedWithCommonErrorsTests { get; set; }
    public int FailedWithUniqueErrorsTests { get; set; }
    public int CommonErrorsCount => CommonErrors.Count;
    public List<CommonErrorStatistic> CommonErrors { get; set; } = new();
    public List<FailedTest> UniqueErrorTests { get; set; } = new();
}

public class CommonErrorStatistic
{
    public string ErrorMessage { get; set; }
    public int FailedTests { get; set; }
    public int FlakyTests { get; set; }
    public int FailEveryDayTests { get; set; }
    public int FailedFirstTimeTests { get; set; }
    public List<FailedTest> FailedTestsInfo { get; set; }
}

public class FailedTest
{
    public string Title { get; set; }
    public string Team { get; set; }
    public string Section { get; set; }
    public string FailStatus { get; set; }
    public string FailsSince { get; set; }
    public int FailRate { get; set; }
    public string History { get; set; }
    public int TestrailLink { get; set; }
    public string Exception { get; set; }
}