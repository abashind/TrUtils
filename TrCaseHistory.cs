using System;
using System.Collections.Generic;
using System.Linq;
using TrUtils.DTOs;
using TrUtils.Extensions;

namespace TrUtils;

public class TrCaseHistory
{
    public int CaseId;
    public string? Title { get; set; }
    public string? Team { get; set; }
    public string? Section { get; set; }

    public List<CaseHistoryEntry>? History;

    public void Analyze(int runId)
    {
        var runHistoryEntry = History.FirstOrDefault(he => he.RunId.Equals(runId));

        if (runHistoryEntry == null)
        {
            Console.WriteLine($"Can't find history entry for the '{Title}' case for the '{runId}' run");
            return;
        }

        var startIndex = History.IndexOf(runHistoryEntry);
        var historyBeforeRun = History.GetRange(startIndex, History.Count - startIndex);

        #region Calculate fail rate

        var percent = historyBeforeRun.Count / 100d;
        var failed = historyBeforeRun.Where(h => h.Failed).ToList().Count;
        var failRate = failed / percent;

        runHistoryEntry.FailRate = (int)Math.Round(failRate);

        #endregion

        var lastException = runHistoryEntry.Exception;

        if (lastException.IsNullOrEmpty())
        {
            Console.WriteLine($"The '{CaseId}' case failed, but has no exception.");
            return;
        }

        lastException = lastException.CutAfterPhrase("FailedStep: ").Trim();

        #region Calculate date when the test failed for the first time because of the current exception

        using var historyEnumerator = historyBeforeRun.GetEnumerator();
        var previousDate = "";

        while (historyEnumerator.MoveNext())
        {
            var current = historyEnumerator.Current;
            if (lastException.ContainedIn(current?.Exception))
            {
                previousDate = current.CompleteDate;
                continue;
            }
            break;
        }

        runHistoryEntry.FailsSince = previousDate;

        #endregion

        #region Find out if the case is flaky

        var lastPassed = historyBeforeRun.FirstOrDefault(h => h.Failed is false);
        if (lastPassed is not null)
        {
            var lastPassedIndex = historyBeforeRun.IndexOf(lastPassed);

            var resultsBeforePassed = historyBeforeRun.Skip(lastPassedIndex + 1);

            if (resultsBeforePassed.Where(r => r.Failed).Any(h => lastException.ContainedIn(h.Exception)))
            {
                runHistoryEntry.IsFlaky = true;
                runHistoryEntry.FirstTimeFailed = false;
                runHistoryEntry.FailsEveryDay = false;
                return;
            }
        }

        #endregion

        #region Find out if the case failed for the first time

        if (!historyBeforeRun.Skip(1).Where(r => r.Failed).Any(h => lastException.ContainedIn(h.Exception)))
        {
            runHistoryEntry.FirstTimeFailed = true;
            runHistoryEntry.IsFlaky = false;
            runHistoryEntry.FailsEveryDay = false;
            return;
        }

        #endregion

        #region Find out if the case fails stable

        if (lastException.ContainedIn(historyBeforeRun[1].Exception))
        {
            runHistoryEntry.IsFlaky = false;
            runHistoryEntry.FirstTimeFailed = false;
            runHistoryEntry.FailsEveryDay = true;
            return;
        }

        runHistoryEntry.IsFlaky = false;
        runHistoryEntry.FirstTimeFailed = false;
        runHistoryEntry.FailsEveryDay = false;

        #endregion
    }
}