using System;
using System.Collections.Generic;
using TrUtils.Extensions;
using TrUtils.Interfaces;
using TrUtils.ParameterProviders;

namespace TrUtils;

public static class Parameters
{
    private static readonly List<IParameterProvider> Providers = new()
    {
        new NunitParameterProvider(0),
        new JsonParameterProvider(1, "TrUtilsParameters.json")
    };

    private static string GetExternalParameter(string paramName)
    {
        var results = new PriorityQueue<string, int>();

        foreach (var provider in Providers)
        {
            var param = provider.GetParameter(paramName);
            if(param.IsNotNullOrEmpty())
                results.Enqueue(param, provider.Priority);
        }

        var result = results.Count is not 0 ?results.Dequeue() : throw new Exception($"Parameter '{paramName}' was not found.");

        return result.IsNotNullOrEmpty() ? result : throw new Exception($"Parameter '{paramName}' was not found.");
    }

    public static readonly string TestrailUserName = GetExternalParameter("testrailuser");
    public static readonly string TestrailApiToken = GetExternalParameter("testrailkey");
    public static readonly string ProjectName = GetExternalParameter("projectName");
    public static readonly string TrServer = GetExternalParameter("trServer");
    public static readonly string ReRunTag = GetExternalParameter("reRunTag");
    public static readonly int SimilarityForExceptions = int.Parse(GetExternalParameter("similarityForExceptions"));
    public static readonly string XmlResultFilePattern = GetExternalParameter("xmlResultFilePattern");
    public static readonly int RunsToObserve = int.Parse(GetExternalParameter("runsToObserve"));
    public static readonly string HistoryFileName = $"{ProjectName.Replace(" ", "")}_history.zip";
    public static readonly string TempHistoryFileName = $"{ProjectName.Replace(" ", "")}_history.json";
    public static readonly string RerunResultsFileName = GetExternalParameter("rerunResultsFileName");
    public static readonly string ReferenceProjectName = GetExternalParameter("referenceProjectName");
    public static readonly string ExceptionStartPhrase = GetExternalParameter("exceptionStartPhrase");
    public static readonly string ExceptionStopPhrase = GetExternalParameter("exceptionStopPhrase");

    public static string ResultsDirectory => GetExternalParameter("testResultPath");
    public static string UploadName => GetExternalParameter("uploadname");
    public static string XmlResultsFolder => GetExternalParameter("resultsFilePath");
    public static string NewRunName => GetExternalParameter("newRunName");
    public static bool SkipFlakyForRerun => bool.Parse(GetExternalParameter("skipFlakyForRerun"));

    //Html params
    public static readonly string RunReportsHtml = GetExternalParameter("runReportsHtmlName");
    public static readonly string HtmlDataPlaceholder = GetExternalParameter("htmlDataPlaceHolder");
    public static readonly string HtmlProjectNamePlaceholder = GetExternalParameter("htmlProjectNamePlaceholder");
    public static readonly string HtmlTrServerPlaceholder = GetExternalParameter("htmlTrServerPlaceholder");

    //Jira params
    public static string JiraAuthToken => GetExternalParameter("jiraAuthToken");
    public static string JiraBaseUri = GetExternalParameter("jiraApiUri");
    public static string JiraIssueUri = $"{JiraBaseUri}/issue/";
}