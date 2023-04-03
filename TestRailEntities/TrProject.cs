using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TrUtils.DTOs;
using TrUtils.Extensions;

namespace TrUtils.TestRailEntities;

/// <summary>
/// This class is for single suite projects.
/// </summary>
public class TrProject : TrBaseClass
{
    public readonly string ProjectName;
    public readonly int ProjectId;
    public TrProject(string projectName)
    {
        ProjectName = projectName;
        ProjectId = GetProjectId();
    }
    private int GetProjectId()
    {
        var response = (JObject)TrClient.SendGet("get_projects");

        if (!response.TryGetValue("projects", out var projects))
            throw new Exception($"'projects' node is not found in the next JSON - '{response}'");

        return projects.FirstOrDefault(p => p["name"].ToString().Equals(ProjectName))?["id"]?.ToObject<int>() ??
               throw new Exception($"The '{ProjectName}' project was not found");
    }

    private int _suiteId;
    public int SuitId
    {
        get
        {
            if (_suiteId == 0)
            {
                var response = (JArray)TrClient.SendGet($"get_suites/{ProjectId}");
                if (response is null || !response.Any())
                    throw new Exception($"Failed to find suite id for the '{ProjectId}' project");

                _suiteId = response[0]["id"].ToObject<int>();
            }

            return _suiteId;
        }
       
    }

    private List<TrRun> _allRuns;
    public List<TrRun> AllRuns
    {
        get
        {
            if (_allRuns is null)
            {
                var runDtos = GetTestRailItemsWithPagination($"get_runs/{ProjectId}", "runs")?.ToObject<List<RunDto>>();
                _allRuns = runDtos?.Select(d => new TrRun(d)).ToList();
            }
            return _allRuns;
        }
    }
    
    public TrRun GetRun(string runName)
    {
        return AllRuns.FirstOrDefault(r => r.Dto.Name.Equals(runName)) ??
               throw new Exception($"Can not find run with '{runName}'");
    }

    private TrRun GetRun(int index)
    {
        if (AllRuns is null || AllRuns.Count.Equals(index))
            throw new Exception($"Can not get run with the '{index}' index in the '{ProjectName}' project");
        return AllRuns[index];
    }

    public TrRun LastRun => GetRun(0);

    public TrRun SecondToLastRun => GetRun(1);

    public void SaveHistoryInLastRun(List<TrCaseHistory> history) => 
        LastRun.AttachHistory(history, Parameters.HistoryFileName);

    /// <summary>
    /// Generate reports for all runs that we have history for.
    /// </summary>
    public List<RunReportDto> GenerateRunReports()
    {
        var history = GetSavedHistory();
        if (!history.Any())
            throw new Exception($"There is no history file attached to a run in the '{ProjectName}' project.");

        var runsCount = Parameters.RunsToObserve > AllRuns.Count ? AllRuns.Count : Parameters.RunsToObserve;
        return AllRuns.GetRange(0, runsCount).Select(r => r.GetReport(history)).ToList();
    }

    /// <summary>
    /// Search for history file in the last run, then in the second to last run and then in all runs to observe.
    /// </summary>
    /// <returns>The latest history that was saved in one of runes.</returns>
    public List<TrCaseHistory> GetSavedHistory()
    {
        var history = LastRun.GetHistory(Parameters.HistoryFileName);
        if (history is null)
            Console.WriteLine("Can not find history file in the last run.");
        else
            return history;

        history = SecondToLastRun.GetHistory(Parameters.HistoryFileName);
        if (history is null)
            Console.WriteLine("Can not find history file in the second to last run.");
        else
            return history;

        if (!IsThereRunWithHistory(Parameters.HistoryFileName, Parameters.RunsToObserve, out var runId))
            Console.WriteLine("There is no history file attached to runs.");
        else
        {
            history = AllRuns.First(r => r.Dto.Id.Equals(runId)).GetHistory(Parameters.HistoryFileName);
            return history;
        }

        return new List<TrCaseHistory>();
    }

    public List<CaseDto> GetCases() =>
        GetTestRailItemsWithPagination($"get_cases/{ProjectId}", "cases")?
            .ToObject<List<CaseDto>>();

    public List<CaseDto> GetCases(int sectionId) =>
        GetTestRailItemsWithPagination($"get_cases/{ProjectId}&section_id={sectionId}", "cases")?
            .ToObject<List<CaseDto>>();

    public CaseDto GetCase(string caseName) =>
        GetTestRailItemsWithPagination($"get_cases/{ProjectId}&filter={caseName}", "cases")?
            .ToObject<List<CaseDto>>()?
            .FirstOrDefault();

    public CaseDto GetCase(int caseId)
    {
        var response = (JObject)TrClient.SendGet($"get_case/{caseId}");
        return response is not null ? 
            response.ToObject<CaseDto>() : 
            throw new Exception($"Can not find the '{caseId}' case in the '{ProjectName}' project.");
    }

    public CaseDto AddCase(int sectionId, string caseName)
    {
        var response = (JObject)TrClient.SendPost($"add_case/{sectionId}", new
        {
            title = caseName
        });

        return response?.ToObject<CaseDto>() ??
               throw new Exception($"Unable to create '{caseName}' case.");
    }

    public RunDto AddRun(string runName)
    {
        var response = (JObject)TrClient.SendPost($"add_run/{ProjectId}", new
        {
            suite_id = SuitId,
            name = runName,
            include_all = false
        });

        return response?.ToObject<RunDto>() ??
               throw new Exception($"Unable to create '{runName}' case.");
    }

    public CaseDto AddCaseIfNotExists(int sectionId, string caseName) =>
        DoesCaseExists(caseName, out var caseId) ? caseId : AddCase(sectionId, caseName);

    public bool DoesSectionExist(string sectionName, out int sectionId)
    {
        var section = Sections.FirstOrDefault(s => s.Name.Equals(sectionName));
        if (section is null)
        {
            sectionId = int.MinValue;
            return false;
        }
        sectionId = section.Id;
        return true;
    }

    public bool DoesCaseExists(string caseName, out CaseDto trCase)
    {
        var theCase = GetCase(caseName);
        if (theCase is null) 
            trCase = null;

        trCase = theCase;
        return true;
    }

    private List<SectionDto> _sections;
    public List<SectionDto>? Sections => 
        _sections ??= GetTestRailItemsWithPagination($"get_sections/{ProjectId}", "sections")?.ToObject<List<SectionDto>>();

    public SectionDto AddSection(string sectionName)
    {
        var response = (JObject)TrClient.SendPost($"add_section/{ProjectId}", new
        {
            name = sectionName
        });

        return response?.ToObject<SectionDto>() ??
               throw new Exception($"Unable to create '{sectionName}' section.");
    }

    public List<SectionDto> AddSections(IEnumerable<string> sectionNames) => 
        sectionNames.Select(AddSection).ToList();

    public int AddSectionIfNotExists(string sectionName) =>
        DoesSectionExist(sectionName, out var sectionId) ? sectionId :
            AddSection(sectionName).Id;

    /// <summary>
    /// Search for a run with attached history.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="runsToObserve"></param>
    /// <param name="runId"></param>
    /// <returns>Id of the run with attached history.</returns>
    public bool IsThereRunWithHistory(string fileName, int runsToObserve, out int runId)
    {
        var historyAttachments = new List<TrAttachment>();

        foreach (var run in AllRuns.GetRange(0, runsToObserve >= AllRuns.Count ? AllRuns.Count : runsToObserve))
            historyAttachments.AddRange(run.GetAttachments().Where(a => a.Dto.Filename.Equals(fileName)));

        var _runId = historyAttachments.MaxBy(r => r.Dto.EntityId)?.Dto.RunId;
        if (_runId is not null)
        {
            runId = (int)_runId;
            return true;
        }

        runId = int.MinValue;
        return false;
    }

    /// <summary>
    /// Extract history for all cases from TestRail.
    /// </summary>
    /// <param name="runsToObserve">How many runs to analyze.</param>
    /// <param name="failedStatusId">Id of TestRail test failed status.</param>
    /// <param name="casesHistory">History that was collected before ans saved as a zip file in one of the project's runs.</param>
    /// <returns></returns>
    public List<TrCaseHistory> UpdateHistory(int runsToObserve, int failedStatusId, List<TrCaseHistory> casesHistory)
    {
        var runsToDownload = AllRuns.GetRange(0, runsToObserve > AllRuns.Count ? AllRuns.Count : runsToObserve);

        if (casesHistory is not null && casesHistory.Any())
        {
            //Delete history for the last run. Because there may be changes in results, so we need to update the history.
            foreach (var trCaseHistory in casesHistory)
                trCaseHistory.History.RemoveAll(historyEntry => historyEntry.RunId.Equals(runsToDownload.First().Dto.Id));

            var runsThatDontRequireUpdate = new HashSet<int>();
            foreach (var c in casesHistory.Select(c => c.History))
            {
                foreach (var entry in c)
                    runsThatDontRequireUpdate.Add(entry.RunId);
            }
            runsToDownload.RemoveAll(r => runsThatDontRequireUpdate.Contains(r.Dto.Id));
        }
        else
            casesHistory = new List<TrCaseHistory>();

        #region Download tests and results for new runs

        var allTestsInRuns = runsToDownload.ToDictionary(run => run.Dto.Id, run => run.GetAllTests());
        var failedResults = runsToDownload.ToDictionary(run => run.Dto.Id, run => run.GetResults(failedStatusId));

        #endregion

        #region Add new history entries for cases

        var allTrCases = GetCases();

        foreach (var (runId, tests) in allTestsInRuns)
        {
            var runResults = failedResults.FirstOrDefault(rr => rr.Key.Equals(runId)).Value;
            var completeDate = runsToDownload.First(r => r.Dto.Id.Equals(runId)).Dto.CreatedOn;

            foreach (var test in tests)
            {
                var historyEntry = new CaseHistoryEntry
                {
                    CompleteDate = DateTimeOffset.FromUnixTimeSeconds(completeDate).Date.ToString("yy-MM-dd"),
                    Failed = test.Dto.StatusId == failedStatusId,
                    RunId = runId,
                    Exception = runResults.Where(r => r.Dto.TestId.Equals(test.Dto.Id))
                        .FirstOrDefault(r => r.Dto.CustomException.IsNotNullOrEmpty())?.Dto.CustomException,
                    TestId = test.Dto.Id
                };

                var caseHistory = casesHistory.FirstOrDefault(h => h.CaseId.Equals(test.Dto.CaseId));

                if (caseHistory is not null)
                {
                    caseHistory.History.Add(historyEntry);
                    continue;
                }

                var theCase = allTrCases.First(trCase => trCase.Title.Equals(test.Dto.Title));

                casesHistory.Add(new TrCaseHistory
                    { 
                        CaseId = test.Dto.CaseId, 
                        Title = test.Dto.Title,
                        Section = Sections.First(s => s.Id.Equals(theCase.SectionId)).Name,
                        Team = TrStatic.GetCaseDropdownField("Team assigned")
                            .Items.FirstOrDefault(i => i.itemNumber.Equals(theCase.TeamAssigned ?? int.MinValue)).itemName,
                        History = new List<CaseHistoryEntry> { historyEntry }
                });
            }
        }

        #endregion

        #region Sort history entries by runId and delete the last in there are more runs than we need

        foreach (var trCase in casesHistory)
        {
            var history = trCase.History;
            history.Sort((e1, e2) => e2.RunId.CompareTo(e1.RunId));
            if (history.Count > runsToObserve)
                history.RemoveAt(trCase.History.Count - 1);
        }

        #endregion

        return casesHistory;
    }

    /// <summary>
    /// All cases should have the same values in the fields, the values are taken form the first case in the List.
    /// </summary>
    private void UpdateFailedAttributesForCases(IList<CaseDto> cases)
    {
        if (cases is null || !cases.Any())
            return;

        var firstCase = cases.First();
        var body = new
        {
            case_ids = cases.Select(c => c.Id),
            custom_flaky = firstCase.IsFlaky,
            custom_firsttimefail = firstCase.FirstTimeFailed,
            custom_everydayfail = firstCase.FailsEveryDay
        };

        var result = TrClient.SendPost($"update_cases/{SuitId}", body);
        if (result is null)
            Console.WriteLine("No cases were updated with flaky statuses, some error.");
    }

    /// <summary>
    /// All cases should have the same values in the fields, the values are taken form the first case in the List.
    /// </summary>
    public void UpdateFailedAttributesForCasesByChunks(List<CaseDto> cases, int chunkSize)
    {
        var shift = 0;
        while (shift < cases.Count)
        {
            chunkSize = cases.Count > chunkSize + shift ? chunkSize : cases.Count - shift;
            UpdateFailedAttributesForCases(cases.GetRange(shift, chunkSize));
            shift += chunkSize;
        }
        Console.WriteLine($"'{cases.Count}' cases were updated.");
    }

    /// <summary>
    /// All cases should have the same values in the fields, the values are taken form the first case in the List.
    /// </summary>
    private void UpdateFailedOnReferenceAttributeForCases(List<CaseDto> cases)
    {
        if (cases is null || !cases.Any())
            return;

        var firstCase = cases.First();
        var body = new
        {
            case_ids = cases.Select(c => c.Id),
            custom_failsonreferenceproject = firstCase.FailsOnReferenceProject
        };

        var result = TrClient.SendPost($"update_cases/{SuitId}", body);
        if (result is null)
            Console.WriteLine("No cases were updated with flaky statuses, some error.");
    }

    public void UpdateFailedOnReferenceAttributeForCasesByChunks(List<CaseDto> cases, int chunkSize)
    {
        var shift = 0;
        while (shift < cases.Count)
        {
            chunkSize = cases.Count > chunkSize + shift ? chunkSize : cases.Count - shift;
            UpdateFailedOnReferenceAttributeForCases(cases.GetRange(shift, chunkSize));
            shift += chunkSize;
        }
        Console.WriteLine($"'{cases.Count}' cases were updated.");
    }

    public void AddExceptionToFailedTestsInAllRunsEarlierThan(string startRunName)
    {
        var runs = AllRuns.OrderByDescending(r => r.Dto.Id).ToList();
        var startRunIndex = runs.IndexOf(runs.First(r => r.Dto.Name.Equals(startRunName)));
        runs.RemoveRange(0, startRunIndex);

        foreach (var run in runs)
            run.AddExceptionToFailedTests();
    }
}