using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DashworksTestAutomationCore.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TrUtils.DTOs;
using Ionic.Zip;
using Ionic.Zlib;
using TrUtils.Extensions;

namespace TrUtils.TestRailEntities;

public class TrRun : TrBaseClass
{
    public readonly RunDto Dto;
    public TrRun(RunDto dto) => 
        Dto = dto;

    public List<TrTest> GetTests(int statusId)
    {
        var dtos = GetTestRailItemsWithPagination($"get_tests/{Dto.Id}&status_id={statusId}", "tests")?
            .ToObject<List<TestDto>>();
        return dtos?.Select(d => new TrTest(d)).ToList();
    }

    public List<TrTest> FailedTests => GetTests(TrStatic.FailedStatusId);

    public List<TrTest> GetAllTests()
    {
        var dtos = GetTestRailItemsWithPagination($"get_tests/{Dto.Id}", "tests")?
            .ToObject<List<TestDto>>();
        return dtos?.Select(d => new TrTest(d)).ToList();
    }

    public List<TrResult> GetResults(int statusId)
    {
        var dtos = GetTestRailItemsWithPagination($"get_results_for_run/{Dto.Id}&status_id={statusId}", "results")?
            .ToObject<List<ResultDto>>();
        return dtos?.Select(d => new TrResult(d)).ToList();
    }

    public List<TrAttachment> GetAttachments()
    {
        var dtos = GetTestRailItemsWithPagination($"get_attachments_for_run/{Dto.Id}", "attachments")?
            .ToObject<List<AttachmentDto>>();
        return dtos?.Select(d => new TrAttachment(d)).ToList();
    }

    public void AddResults(List<ResultDto> resultDtos)
    {
        if (!resultDtos.Any())
            return;

        TrClient.SendPost($"add_results/{Dto.Id}", $"{{\"results\": {JsonConvert.SerializeObject(resultDtos)}}}");
        Console.WriteLine($"'{resultDtos.Count}' results were added to '{Dto.Id}' run.");
    }

    public void AddResultsByChunks(List<ResultDto> results, int chunkSize)
    {
        while (results.Any())
        {
            chunkSize = results.Count > chunkSize ? chunkSize : results.Count;
            AddResults(results.GetRange(0, chunkSize));
            results.RemoveRange(0, chunkSize);
        }
    }

    public void IncludeAllCasesToRun()
    {
        TrClient.SendPost($"update_run/{Dto.Id}", new
        {
            include_all = true
        });
    }

    public RunDto IncludeCasesToRun(IEnumerable<CaseDto> cases)
    {
        var response = (JObject)TrClient.SendPost($"update_run/{Dto.Id}", new
        {
            include_all = false,
            case_ids = cases.Select(c => c.Id)
        });

        return response?.ToObject<RunDto>() ??
               throw new Exception($"Unable to update the '{Dto.Name}' run.");
    }

    /// <summary>
    /// Return null if can't find history attached to the run.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public List<TrCaseHistory> GetHistory(string fileName)
    {
        var historyAttachment = GetAttachments().FirstOrDefault(a => a.Dto.Filename.Equals(fileName));
        if (historyAttachment is null)
            return null;

        historyAttachment.SaveLocally(fileName);

        using ZipFile zip = new(fileName) { ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently };
        zip.ExtractAll(Directory.GetCurrentDirectory());

        var historyAsText = File.ReadAllText(Parameters.TempHistoryFileName);
        var history = JsonConvert.DeserializeObject<List<TrCaseHistory>>(historyAsText);

        return history;
    }

    public void AddAttachment(string attachmentPath)
    {
        var response = (JObject)TrClient.SendPost($"add_attachment_to_run/{Dto.Id}", attachmentPath);

        if (!response.TryGetValue("attachment_id", out _))
        {
            Console.WriteLine($"Unable to add attachment to the '{Dto.Id}' run.");
            return;
        }

        Console.WriteLine($"'{Path.GetExtension(attachmentPath)}' file was attached to the '{Dto.Id}' run.");
    }

    public void AttachHistory(List<TrCaseHistory> casesHistory, string fileName)
    {
        File.WriteAllText(Parameters.TempHistoryFileName, JsonConvert.SerializeObject(casesHistory));

        using ZipFile zip = new() { CompressionLevel = CompressionLevel.BestCompression };
        zip.AddFile(Parameters.TempHistoryFileName);

        if(File.Exists(fileName))
            File.Delete(fileName);

        zip.Save(fileName);

        var existingHistory = GetAttachments().FirstOrDefault(a => a.Dto.Filename.Contains(fileName));
        existingHistory?.DeleteFromTestrail();

        AddAttachment(fileName);
    }

    /// <summary>
    /// Attach Run reports as html file to the run. If there already is reports html - overwrite it.
    /// </summary>
    public void AttachRunReportsHtml(List<RunReportDto> reports, string htmlFilePath, string projectName)
    {
        var fileName = Path.GetFileName(htmlFilePath);

        var html = File.ReadAllText(htmlFilePath);
        var data = JsonConvert.SerializeObject(reports);
        html = html
            .Replace(Parameters.HtmlDataPlaceholder, data)
            .Replace(Parameters.HtmlProjectNamePlaceholder, projectName)
            .Replace(Parameters.HtmlTrServerPlaceholder, Parameters.TrServer);

        File.WriteAllText(htmlFilePath, html);

        var existingReports = GetAttachments().FirstOrDefault(a => a.Dto.Filename.Contains(fileName));
        existingReports?.DeleteFromTestrail();
        AddAttachment(htmlFilePath);
    }

    public void AddScreenshotsToFailedTests()
    {
        var failedTests = FailedTests;

        if (!failedTests.Any())
        {
            Console.WriteLine($"No failed tests were found in '{Dto.Id}' run.");
            return;
        }

        var runDir = Directory.GetDirectories(Parameters.ResultsDirectory).LastOrDefault(d => d.Contains(Parameters.UploadName));

        if (runDir is null)
        {
            Console.WriteLine($"The directory with screenshots for '{Parameters.UploadName}' run was not found.");
            return;
        }

        var allScreenshots = Directory.GetFiles(runDir, "*.png*", SearchOption.AllDirectories);

        foreach (var test in failedTests)
        {
            var testScreenshots = allScreenshots.Where(s => s.Contains(test.Dto.Title)).ToList();

            testScreenshots = testScreenshots.Count > 1 ? testScreenshots.GetRange(0, 2) : testScreenshots;

            foreach (var screenshot in testScreenshots) 
                test.AttachScreenshot(screenshot);
        }
    }

    public void AddExceptionToFailedTests()
    {
        var failedTests = FailedTests;

        if (!failedTests.Any())
        {
            Console.WriteLine($"No failed tests were found in '{Dto.Name}' run.");
            return;
        }

        var results = GetResults(TrStatic.FailedStatusId);
        if (!results.Any())
        {
            Console.WriteLine($"No results for failed tests were found in '{Dto.Name}' run.");
            return;
        }

        var resultsToAdd = failedTests.Select(t => results.FirstOrDefault(r => r.Dto.TestId.Equals(t.Dto.Id)))
            .Where(result => result is not null).ToList();

        foreach (var result in resultsToAdd) 
            result.AssignException();

        AddResultsByChunks(resultsToAdd.Select(r => r.Dto).ToList(), 50);
    }

    /// <summary>
    /// Assign bug status to the failed tests if they were marked as bugs in the sourceRun and the ticket is not closed yet.
    /// </summary>
    /// <param name="sourceRun">Run from where take bug results.</param>
    public void AssignBugStatusToFailedTests(TrRun sourceRun)
    {
        var sourceTests = sourceRun.GetTests(TrStatic.BugStatusId);
        if (!sourceTests.Any())
        {
            Console.WriteLine($"No tests with bug status were found in the '{Dto.Name}' run.");
            return;
        }

        var destTests = GetAllTests();

        var resultsToAdd = new List<TrResult>();
        foreach (var st in sourceTests)
        {
            var dt = destTests.FirstOrDefault(dt => dt.Dto.Title.Equals(st.Dto.Title));
            if (dt is null)
            {
                Console.WriteLine($"'{st.Dto.Title}' was not found in the '{sourceRun.Dto.Name}' run.");
                continue;
            }

            var bugRef = st.GetResults().FirstOrDefault(r => r.Dto.StatusId.Equals(TrStatic.BugStatusId))?.Dto.Defects ?? 
                         TrStatic.GetCase(st.Dto.CaseId).Refs;

            if (bugRef.IsNullOrEmpty())
            {
                Console.WriteLine($"The Reference field is not populated for the '{st.Dto.Title}' case and " +
                             $"The Defects field is not populated for the '{st.Dto.Id}' test bug status result.");
                continue;
            }

            var addBugResult = JiraHelper.ShouldSkipTest(bugRef);
            if (!addBugResult)
                continue;

            var result = dt.GetResults().Last();

            resultsToAdd.Add(new TrResult(
                new ResultDto
                {
                    Comment = result.Dto.Comment,
                    StatusId = TrStatic.BugStatusId,
                    TestId = dt.Dto.Id,
                    Defects = bugRef
                }));
        }

        foreach (var resultToAdd in resultsToAdd)
            resultToAdd.AssignException();

        AddResults(resultsToAdd.Select(r => r.Dto).ToList());
    }

    /// <summary>
    /// Transfer the status to the failed tests from the sourceRun.
    /// </summary>
    /// <param name="status"> Test rail test status</param>
    /// <param name="sourceRun">Run from where take bug results.</param>
    public void TransferTestStatusesToFailedTests(TestStatus status, TrRun sourceRun)
    {
        var statusId = TrStatic.GetStatusId(status);

        var sourceTests = sourceRun.GetTests(statusId);
        if (!sourceTests.Any())
        {
            Console.WriteLine($"No tests with '{status.Value}' status were found in the '{Dto.Name}' run.");
            return;
        }

        var destTests = GetTests(TrStatic.FailedStatusId);

        var resultsToAdd = new List<TrResult>();
        foreach (var st in sourceTests)
        {
            var dt = destTests.FirstOrDefault(dt => dt.Dto.Title.Equals(st.Dto.Title));
            if (dt is null)
            {
                Console.WriteLine($"'{st.Dto.Title}' was not found in the '{sourceRun.Dto.Name}' run.");
                continue;
            }

            resultsToAdd.Add(new TrResult(
                new ResultDto
                {
                    Comment = "Status was assigned by the utility.",
                    StatusId = statusId,
                    CustomException = "Status was assigned by the utility.",
                    TestId = dt.Dto.Id
                }));
        }

        AddResults(resultsToAdd.Select(r => r.Dto).ToList());
    }

    /// <summary>
    /// Analyze failed tests history and populate appropriate fields in the run's history entry.
    /// </summary>
    /// <param name="casesHistory">History of TestRail cases.</param>
    /// <returns>Case objects that contains only case Id and extra failed attributes.</returns>
    public List<TrCaseHistory> AnalyzeFailedTestsHistory(List<TrCaseHistory> casesHistory)
    {
        var failedTests = FailedTests;
        if (failedTests is null || !failedTests.Any())
        {
            Console.WriteLine($"No failed tests were found in '{Dto.Name}' run.");
            return new List<TrCaseHistory>();
        }

        foreach (var test in failedTests)
        {
            var history = casesHistory.FirstOrDefault(c => c.CaseId.Equals(test.Dto.CaseId));
            if (history is null)
            {
                Console.WriteLine($"Can not find history for the '{test.Dto.Title}' test.");
                continue;
            }
            history.Analyze(Dto.Id);
        }
        return casesHistory;
    }

    public RunReportDto GetReport(List<TrCaseHistory> history)
    {
        var runStat = new RunReportDto
        {
            Date = DateTimeOffset.FromUnixTimeSeconds(Dto.CreatedOn).Date.ToString("yy-MM-dd"),
            RunId = Dto.Id,
            RunName = Dto.Name,
            FailedTests = Dto.FailedCount,
            PassedTests = Dto.PassedCount,
            PassedOnRetest = Dto.RetestCount,
            BugTests = Dto.CustomStatus3Count
        };

        var failedInCurrentRunCases = history.SelectMany(c => c.History, (c, h) => new { c.CaseId, c.Title, c.Section, c.Team, Result = h })
              .Where(c => c.Result.RunId.Equals(Dto.Id))
              .Where(c => c.Result.Failed)
              .Where(c => c.Result.Exception.IsNotNullOrEmpty())
              .ToList();

        //If we have no history for the run - just return the info we get from Testrail.
        if (!failedInCurrentRunCases.Any())
            return runStat;

        runStat.FlakyTests = failedInCurrentRunCases.Count(c => c.Result.IsFlaky ?? false);
        runStat.FailEveryDayTests = failedInCurrentRunCases.Count(c => c.Result.FailsEveryDay ?? false);
        runStat.FailedFirstTimeTests = failedInCurrentRunCases.Count(c => c.Result.FirstTimeFailed ?? false);

        #region Find out common errors

        var commonErrors = new List<CommonErrorStatistic>();
        var uniqueErrorTests = new List<FailedTest>();

        static string GetDashHistory(List<CaseHistoryEntry> caseHistory)
        {
            var currentException = caseHistory[0].Exception;
            var result = new List<string>();

            foreach (var historyEntry in caseHistory)
            {
                switch (historyEntry.Failed)
                {
                    case false:
                        result.Add("-");
                        continue;
                    case true when currentException.IsNotNullOrEmpty() && currentException.IsSimilarTo(historyEntry.Exception ?? "", 98):
                        result.Add("‗");
                        break;
                    default:
                        result.Add("_");
                        break;
                }
            }
            return result.Aggregate((a, b) => $"{a} {b}");
        }

        while (failedInCurrentRunCases.Any())
        {
            var error = failedInCurrentRunCases.First().Result.Exception;

            var errorFailedTests = failedInCurrentRunCases.Where(c => error.IsSimilarTo(c.Result.Exception, Parameters.SimilarityForExceptions)).ToList();
            if (errorFailedTests.Count >= 2)
            {
                commonErrors.Add(new CommonErrorStatistic
                {
                    FailedTests = errorFailedTests.Count,
                    ErrorMessage = errorFailedTests.Select(c => c.Result.Exception).ToList().FindPattern(),
                    FailedFirstTimeTests = errorFailedTests.Count(c => c.Result.FirstTimeFailed ?? false),
                    FailEveryDayTests = errorFailedTests.Count(t => t.Result.FailsEveryDay ?? false),
                    FlakyTests = errorFailedTests.Count(t => t.Result.IsFlaky ?? false),
                    FailedTestsInfo = errorFailedTests.Select(c => new FailedTest
                    {
                        Title = c.Title,
                        Team = c.Team,
                        Section = c.Section,
                        Exception = c.Result.Exception,
                        FailRate = c.Result.FailRate,
                        FailsSince = c.Result.FailsSince,
                        FailStatus = c.Result.FailStatus,
                        History = GetDashHistory(history.First(ch => ch.CaseId.Equals(c.CaseId)).History),
                        TestrailLink = c.Result.TestId
                    }).ToList()
                });
            }
            else
            {
                uniqueErrorTests.AddRange(errorFailedTests.Select(c => new FailedTest
                {
                    Title = c.Title,
                    Team = c.Team,
                    Section = c.Section,
                    Exception = c.Result.Exception,
                    FailRate = c.Result.FailRate,
                    FailsSince = c.Result.FailsSince,
                    FailStatus = c.Result.FailStatus,
                    History = GetDashHistory(history.First(ch => ch.CaseId.Equals(c.CaseId)).History),
                    TestrailLink = c.Result.TestId
                }).ToList());
            }
            foreach (var caseHistoryEntry in errorFailedTests)
                failedInCurrentRunCases.Remove(caseHistoryEntry);
        }

        #endregion

        runStat.CommonErrors = commonErrors;
        runStat.UniqueErrorTests = uniqueErrorTests;
        runStat.FailedWithCommonErrorsTests = commonErrors.Select(e => e.FailedTests).Sum();
        runStat.FailedWithUniqueErrorsTests = runStat.FailedTests - runStat.FailedWithCommonErrorsTests;

        return runStat;
    }
}