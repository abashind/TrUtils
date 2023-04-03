using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NUnit.Framework;
using TrUtils.DTOs;
using TrUtils.Extensions;
using TrUtils.Helpers;
using TrUtils.TestRailEntities;
using NUnitTestStatuses = NUnit.Framework.Interfaces.TestStatus;

namespace TrUtils;

public static class Utils
{
    private static StringWriter _customOut;
    private static TextWriter _defaultOut;

    private static readonly TrProject CurrentProject = new(Parameters.ProjectName);

    private const string Sections = "Section1 ,Section2 ,Section3";

    #region Utils

    public static void UpdateHistoryAndSaveInLastRun()
    {
        var history = CurrentProject.UpdateHistory(Parameters.RunsToObserve, TrStatic.FailedStatusId, CurrentProject.GetSavedHistory());
        history = CurrentProject.LastRun.AnalyzeFailedTestsHistory(history);
        CurrentProject.SaveHistoryInLastRun(history);
    }

    public static void UpdateCasesThatFailedInLastRun()
    {
        var testsFailedInLastRun = CurrentProject.GetSavedHistory()
            .SelectMany(c => c.History, (c, Result) => (c.CaseId, Result))
            .Where(c => c.Result.RunId.Equals(CurrentProject.LastRun.Dto.Id))
            .Where(c => c.Result.Failed)
            .Where(c => c.Result.Exception.IsNotNullOrEmpty())
            .ToList();

        var firstTimeFailed = testsFailedInLastRun.Where(t => t.Result.FirstTimeFailed ?? false).ToList();
        var failEveryDay = testsFailedInLastRun.Where(t => t.Result.FailsEveryDay ?? false).ToList();
        var flaky = testsFailedInLastRun.Where(t => t.Result.IsFlaky ?? false).ToList();

        #region Logging

        static void Log(ICollection list, string listName) =>
            Console.WriteLine($"{listName} - '{list.Count}'");

        Log(firstTimeFailed, nameof(firstTimeFailed));
        Log(failEveryDay, nameof(failEveryDay));
        Log(flaky, nameof(flaky));

        #endregion

        #region Update attributes

        CurrentProject.UpdateFailedAttributesForCasesByChunks(
            flaky.Select(t => new CaseDto{ Id = t.CaseId, IsFlaky = true}).ToList(), 100);
        CurrentProject.UpdateFailedAttributesForCasesByChunks(
            failEveryDay.Select(t => new CaseDto { Id = t.CaseId, FailsEveryDay = true }).ToList(), 100);
        CurrentProject.UpdateFailedAttributesForCasesByChunks(
            firstTimeFailed.Select(t => new CaseDto { Id = t.CaseId, FirstTimeFailed = true }).ToList(), 100);

        #endregion

        #region Update fail rate & fails since

        static void UpdateFailRateAndFailsSince(List<(int, CaseHistoryEntry)> tests)
        {
            foreach (var test in tests)
                new TrCase(new CaseDto
                        { Id = test.Item1, FailRate = test.Item2.FailRate, FailsSince = test.Item2.FailsSince })
                    .UpdateFailRateAndFailsSince();
        }

        UpdateFailRateAndFailsSince(flaky);
        UpdateFailRateAndFailsSince(failEveryDay);
        UpdateFailRateAndFailsSince(firstTimeFailed);

        #endregion
    }

    public static void MarkFailedTestFromLastRunForRerun(bool skipFlaky, string tag)
    {
        var run = CurrentProject.LastRun;
        var failedId = TrStatic.FailedStatusId;
        var failedTests = run.GetTests(failedId);

        if (skipFlaky)
        {
            var flakyCases = failedTests
                .Select(t => CurrentProject.GetCase(t.Dto.CaseId))
                .Where(c => c.IsFlaky ?? false).ToList().Select(c => c.Id);

            failedTests.RemoveAll(ft => flakyCases.Contains(ft.Dto.CaseId));
        }

        var failedTestsToMark = FeatureFilesHelper.CleanTestsFromIterators(failedTests.Select(t => t.Dto.Title)).ToList();
        FeatureFilesHelper.AddTagToTests(failedTestsToMark, tag);
        Console.WriteLine($"'{failedTestsToMark.Count}' tests were marked with '{tag}' tag.");
    }

    private static List<ResultDto> ConvertJunitResultsToTestrailResults(IEnumerable<JunitTestcase> junitResults, List<TestDto> trTests)
    {
        var trResultDtos = new List<ResultDto>();

        foreach (var jr in junitResults)
        {
            var testId = trTests.FirstOrDefault(tt => jr.Name.Contains(tt.Title))?.Id ?? 0;
            if (testId is 0)
                continue;

            trResultDtos.Add(new ResultDto
            {
                Comment = jr.Failure?.Message ?? "",
                StatusId = jr.Failure is null ? TrStatic.PassedOnRetestStatusId : TrStatic.FailedStatusId,
                TestId = testId
            });
        }

        var trResults = trResultDtos.Select(r => new TrResult(r)).ToList();

        foreach (var trResult in trResults)
            trResult.AssignException();

        return trResults.Select(r => r.Dto).ToList();
    }

    public static void ReportResultsFromJUnitXmlToFailedTestsInLastRunOfTargetTestrailProject(TrProject targetProject, string xmlResultFileName)
    {
        if (!File.Exists(xmlResultFileName))
            throw new Exception($"Can not find the '{xmlResultFileName}' file with junit results.");

        var serializer = new XmlSerializer(typeof(JunitTestResultDto));
        using var reader = new StreamReader(xmlResultFileName);
        var junitSuites = (JunitTestResultDto)serializer.Deserialize(reader);
        var junitResults = junitSuites?.JunitTestsuites[0].Testcases;

        if (junitResults is null)
            throw new Exception($"Can not parse the '{xmlResultFileName}' content to the '{nameof(JunitTestResultDto)}' class.");

        var lastRun = targetProject.LastRun;
        var trTests = lastRun.FailedTests.Select(t => t.Dto).ToList();
        var trResults = ConvertJunitResultsToTestrailResults(junitResults, trTests);
        lastRun.AddResults(trResults);
    }

    public static void MarkTestsInLastRunThatFailOnReferenceProject(string referenceProjectName, string referenceRunName = null)
    {
        var referenceProject = new TrProject(referenceProjectName);
        var referenceRun = referenceRunName is null
            ? referenceProject.LastRun
            : referenceProject.GetRun(referenceRunName);
        var referenceFailedTests = referenceRun.FailedTests;

        var currentFailedTests = CurrentProject.LastRun.FailedTests;

        var testsThatFailedOnBothProjects =
            currentFailedTests.Where(ct => referenceFailedTests.Any(rt => rt.Dto.Title.Equals(ct.Dto.Title))).ToList();

        var casesToUpdate = testsThatFailedOnBothProjects.Select(t => new CaseDto
        {
            Id = t.Dto.CaseId,
            FailsOnReferenceProject = referenceProjectName
        });

        CurrentProject.UpdateFailedOnReferenceAttributeForCasesByChunks(casesToUpdate.ToList(), 50);
    }

    public static void GenerateRunReportsAndAttachToLastRun()
    {
        var reports = CurrentProject.GenerateRunReports();
        CurrentProject.LastRun.AttachRunReportsHtml(reports, Parameters.RunReportsHtml, CurrentProject.ProjectName);
    }

    public static void ReportResultsFromJUnitXmlFilesToNewRunInTestrail(TrProject project, string resultFolder, string runName)
    {
        var junitXmlFiles = Directory.GetFiles(resultFolder, Parameters.XmlResultFilePattern);

        if (junitXmlFiles.Length == 0)
            throw new Exception($"Can not find files with the '{Parameters.XmlResultFilePattern}' pattern in the '{resultFolder}' folder.");

        #region Get results from the junit xml files

        var serializer = new XmlSerializer(typeof(JunitTestResultDto));
        var junitResults = new List<JunitTestsuite>();

        foreach (var junitXmlFile in junitXmlFiles)
        {
            using var reader = new StreamReader(junitXmlFile);
            var junitResult = (JunitTestResultDto)serializer.Deserialize(reader);
            if (junitResult?.JunitTestsuites != null)
                junitResults.AddRange(junitResult.JunitTestsuites);
        }

        #endregion

        #region Check if there are new test jobs and add them to Testrail as sections

        var sectionsInXml = junitResults.Select(s => s.Name);
        var sectionsInProject = project.Sections.Select(s => s.Name);
        var newSections = sectionsInXml.Where(xs => !sectionsInProject.Contains(xs)).ToList();

        if (newSections.Any())
            project.AddSections(newSections);

        #endregion

        #region Check if the xml contains new cases and add them to Testrail

        var allTrCases = new List<CaseDto>();
        foreach (var section in project.Sections)
        {
            var casesInSectionInTr = project.GetCases(section.Id);
            allTrCases.AddRange(casesInSectionInTr);

            var casesInSectionInXml =
                junitResults.FirstOrDefault(suite => suite.Name.Equals(section.Name))?.Testcases;

            if (casesInSectionInXml is null)
            {
                Console.WriteLine($"The '{section.Name}' section exists in Testrail, but a job with '{section.Name}' name is not presented in the xml. " +
                             "So the run had no tests in the section and it's better to delete the section from Testrail.");
                continue;
            }

            var newCases = casesInSectionInXml.Where(xmlCase => !casesInSectionInTr.Any(trCase => trCase.Title.Equals(xmlCase.Name))).ToList();

            if (!newCases.Any()) continue;
            foreach (var junitTestcase in newCases)
            {
                var theCase = project.AddCase(section.Id, junitTestcase.Name);
                allTrCases.Add(theCase);
            }
        }

        //We sort it in order to pick up old cases first, when we map xmlCases to trCases and there are several cases with same name in Testrail.
        allTrCases = allTrCases.OrderByDescending(trCase => trCase.Id).ToList();

        #endregion

        var allXmlCases = junitResults.SelectMany(testSuite => testSuite.Testcases).ToList();

        #region Create run & include all cases from the xml to the run

        var run = new TrRun(project.AddRun(runName));

        var casesToInclude =
            allXmlCases.Select(xmlCase => allTrCases.FirstOrDefault(trCase => trCase.Title.Equals(xmlCase.Name)))
                .Where(trCase => trCase is not null);
        //allTrCases.Where(trCase => allXmlCases.Any(xmlCase => xmlCase.Name.Equals(trCase.Title))).ToList();

        run.IncludeCasesToRun(casesToInclude);

        #endregion

        #region Generate tests results for TestRails & upload them

        var trTests = run.GetAllTests();

        var trResults = new List<ResultDto>();

        foreach (var trTest in trTests)
        {
            var xmlCase = allXmlCases.FirstOrDefault(xmlCase => xmlCase.Name.Equals(trTest.Dto.Title));
            if (xmlCase is null)
            {
                Console.WriteLine($"There is no result in the xml for the '{trTest.Dto.Title}' test.");
                continue;
            }

            var trResult = new ResultDto
            {
                TestId = trTest.Dto.Id,
                StatusId = xmlCase.Failure is null ? TrStatic.PassedStatusId : TrStatic.FailedStatusId,
                Comment = xmlCase.Failure?.Message
            };

            trResults.Add(trResult);
        }

        run.AddResultsByChunks(trResults, 500);

        #endregion
    }

    #endregion

    #region Nunit hooks

    [SetUp]
    [Order(0)]
    public static void AssignCustomOutput()
    {
        _defaultOut = Console.Out;
        _customOut = new StringWriter();
        Console.SetOut(_customOut);
        Console.SetError(_customOut);
    }

    [SetUp]
    [Order(1)]
    public static void LogUtilityNameAtStart() =>
        Console.WriteLine($"------- '{TestContext.CurrentContext.Test.Name}' started.");

    /// <summary>
    /// Enable it, if you need to report util results to TestRail.
    /// But reporting causes a lot of 'untested' cases because pf 'IncludeAllCasesToRun' method
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static void ReportToTestRail()
    {
        var project = new TrProject(Parameters.ProjectName);
        var c = TestContext.CurrentContext;
        var testName = c.Test.Name;
        var status = c.Result.Outcome.Status;
        var statusId = status switch
        {
            NUnitTestStatuses.Failed => TrStatic.FailedStatusId,
            NUnitTestStatuses.Passed => TrStatic.PassedStatusId,
            _ => throw new Exception($"Unexpected status for the utility - '{status}'")
        };

        var sectionId = project.AddSectionIfNotExists("TestRail");
        var caseDto = project.AddCaseIfNotExists(sectionId, testName);
        var lastRun = project.LastRun;

        //Needed as util cases are not included into run by default.
        lastRun.IncludeAllCasesToRun();

        var trCase = new TrCase(caseDto);

        trCase.AddResult(lastRun.Dto.Id, new ResultDto
        {
            StatusId = statusId,
            Comment = "Additional test info: \r\n " +
                      $"{_customOut}" +
                      $"The test error message is '{c.Result.Message}' \r\n " +
                      $"Stack trace is '{c.Result.StackTrace}'"
        });
    }

    [TearDown]
    [Order(1)]
    public static void RestoreDefaultOutput()
    {
        Console.SetOut(_defaultOut);
        Console.SetError(_defaultOut);
        Console.Write(_customOut);
    }

    #endregion

    #region Utils as tests

    [Test]
    public static void Utility_CreateSectionsInTheProject()
    {
        var sections = CurrentProject.AddSections(Sections.Split(" ,").OrderBy(s => s).ToList());
        Console.WriteLine($"'{sections.Count}' sections were added to the '{CurrentProject.ProjectName}' project:");
        Console.WriteLine(string.Join("\r\n", sections));
    }

    [Test]
    public static void Utility_GetResultFields() =>
       TrStatic.GetResultFields();

    [Test]
    public static void Utility_AddExceptionToFailedTestsInAllRunsEarlierThan() =>
        CurrentProject.AddExceptionToFailedTestsInAllRunsEarlierThan("Put the run name here!");

    [Test]
    public static void Utility_TransferTestStatusesToFailedTestsInLastRun() =>
        CurrentProject.LastRun
            .TransferTestStatusesToFailedTests(TestStatus.PassedLocally, CurrentProject.SecondToLastRun);

    /// <summary>
    /// Use it when you need to create history from a scratch, ignoring existing history files in the project.
    /// </summary>
    [Test]
    public static void Utility_RefreshHistoryAndSaveInLastRun()
    {
        var history = CurrentProject.UpdateHistory(Parameters.RunsToObserve, TrStatic.FailedStatusId, null);
        var runsToObserve = CurrentProject.AllRuns.GetRange(0, Parameters.RunsToObserve);
        for (var i = runsToObserve.Count - 1; i >= 0; i--)
            history = runsToObserve[i].AnalyzeFailedTestsHistory(history);

        CurrentProject.SaveHistoryInLastRun(history);
    }
    
    [Test]
    public static void Utility_MarkTestsInLastRunThatFailOnReferenceProject() =>
        MarkTestsInLastRunThatFailOnReferenceProject(Parameters.ReferenceProjectName);

    [Test]
    [Retry(2)]
    public static void Utility_MarkFailedTestsFromLastRunForRerun() =>
        Assert.That(() => MarkFailedTestFromLastRunForRerun(Parameters.SkipFlakyForRerun, Parameters.ReRunTag), Throws.Nothing);

    [Test]
    [Retry(2)]
    public static void Utility_ReportResultsFromJUnitXmlToFailedTestsInLastRunOfTargetTestrailProject() =>
        Assert.That(() => ReportResultsFromJUnitXmlToFailedTestsInLastRunOfTargetTestrailProject(CurrentProject, Parameters.RerunResultsFileName), Throws.Nothing);

    [Test]
    [Retry(2)]
    public static void Utility_ReportResultsFromXmlFilesToNewRunInTestrail() =>
        Assert.That(() => ReportResultsFromJUnitXmlFilesToNewRunInTestrail(CurrentProject, Parameters.XmlResultsFolder, Parameters.NewRunName), Throws.Nothing);


    [Test]
    [Retry(2)]
    [Category("AfterRunTestRailUtils")]
    [Order(0)]
    public static void Utility_0_AssignBugStatusInLastRun() =>
        Assert.That(() => CurrentProject.LastRun
            .AssignBugStatusToFailedTests(CurrentProject.SecondToLastRun), Throws.Nothing);

    [Test]
    [Retry(2)]
    [Category("AfterRunTestRailUtils")]
    [Order(1)]
    public static void Utility_1_AddExceptionToFailedTestsInLastRun() => 
        Assert.That(() => CurrentProject.LastRun.AddExceptionToFailedTests(), Throws.Nothing);

    [Test]
    [Retry(2)]
    [Category("AfterRunTestRailUtils")]
    [Order(2)]
    public static void Utility_2_AddScreenshotsToFailedTestsInLastRun() => 
        Assert.That(() => CurrentProject.LastRun.AddScreenshotsToFailedTests(), Throws.Nothing);

    [Test]
    [Retry(2)]
    [Category("AfterRunTestRailUtils")]
    [Order(3)]
    public static void Utility_3_UpdateHistoryAndSaveInLastRun() =>
        Assert.That(() => UpdateHistoryAndSaveInLastRun(), Throws.Nothing);

    [Test]
    [Retry(2)]
    [Category("AfterRunTestRailUtils")]
    [Order(4)]
    public static void Utility_4_UpdateCasesThatFailedInLastRun() => 
        Assert.That(UpdateCasesThatFailedInLastRun, Throws.Nothing);

    [Test]
    [Retry(2)]
    [Category("AfterRunTestRailUtils")]
    [Order(5)]
    public static void Utility_5_GenerateRunReportsAndAttachToLastRun() =>
        Assert.That(GenerateRunReportsAndAttachToLastRun, Throws.Nothing);
    #endregion
}