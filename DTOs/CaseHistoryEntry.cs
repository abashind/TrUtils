namespace TrUtils.DTOs;

public class CaseHistoryEntry
{
    public string CompleteDate { get; set; }
    public int RunId { get; set; }
    public int TestId { get; set; }
    public bool? IsFlaky { get; set; }
    public bool? FirstTimeFailed { get; set; }
    public bool? FailsEveryDay { get; set; }
    public bool Failed { get; set; }
    public string Exception { get; set; }
    public string FailsSince { get; set; }
    public int FailRate { get; set; }

    public string FailStatus
    {
        get
        {
            if (IsFlaky ?? false) return "Flaky";
            if (FirstTimeFailed ?? false) return "First Time";
            if (FailsEveryDay ?? false) return "Every Day";

            return string.Empty;
        }
    }
}