using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using RestSharp;
using TrUtils;

namespace DashworksTestAutomationCore.Utils;

public static class JiraHelper
{
    public static bool ShouldSkipTest(string issueId)
    {
        var doneStatuses = new List<string> { "Closed", "Done", "Resolved" };
        var status = GetCurrentIssueStatus(issueId);
        Console.WriteLine($"'{issueId}' JIRA has '{status}'");

        var result = !doneStatuses.Contains(status);
        Console.WriteLine($"The test should be skipped - '{result}'");
        return result;
    }

    private static string GetCurrentIssueStatus(string issue)
    {
        try
        {
            var issueId = int.Parse(Regex.Match(issue, "\\d+").Value);

            var requestUri = $"{Parameters.JiraIssueUri}DAS-{issueId}";
            var request = new RestRequest(requestUri);
            request.AddHeader("Authorization", Parameters.JiraAuthToken);

            var client = new RestClient(Parameters.JiraBaseUri);
            var response = client.Get(request);

            return JObject.Parse(response.Content)["fields"]["status"]["name"].ToString();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unable to get jira status for '{issue}' ticket: {e}");
            return string.Empty;
        }
    }
}