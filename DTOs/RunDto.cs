using System.Collections.Generic;
using Newtonsoft.Json;

namespace TrUtils.DTOs;

public class RunDto
{
    [JsonProperty("assignedto_id")]
    public int? AssignedtoId { get; set; }

    [JsonProperty("blocked_count")]
    public int BlockedCount { get; set; }

    [JsonProperty("completed_on")]
    public object CompletedOn { get; set; }

    [JsonProperty("config")]
    public string Config { get; set; }

    [JsonProperty("config_ids")]
    public List<int> ConfigIds { get; set; }

    [JsonProperty("created_by")]
    public int CreatedBy { get; set; }

    [JsonProperty("created_on")]
    public int CreatedOn { get; set; }

    [JsonProperty("refs")]
    public string Refs { get; set; }

    [JsonProperty("custom_status1_count")]
    public int CustomStatus1Count { get; set; }

    [JsonProperty("custom_status2_count")]
    public int CustomStatus2Count { get; set; }

    [JsonProperty("custom_status3_count")]
    public int CustomStatus3Count { get; set; }

    [JsonProperty("custom_status4_count")]
    public int CustomStatus4Count { get; set; }

    [JsonProperty("custom_status5_count")]
    public int CustomStatus5Count { get; set; }

    [JsonProperty("custom_status6_count")]
    public int CustomStatus6Count { get; set; }

    [JsonProperty("custom_status7_count")]
    public int CustomStatus7Count { get; set; }

    [JsonProperty("description")]
    public object Description { get; set; }

    [JsonProperty("failed_count")]
    public int FailedCount { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("include_all")]
    public bool IncludeAll { get; set; }

    [JsonProperty("is_completed")]
    public bool IsCompleted { get; set; }

    [JsonProperty("milestone_id")]
    public int? MilestoneId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("passed_count")]
    public int PassedCount { get; set; }

    [JsonProperty("plan_id")]
    public int? PlanId { get; set; }

    [JsonProperty("project_id")]
    public int ProjectId { get; set; }

    [JsonProperty("retest_count")]
    public int RetestCount { get; set; }

    [JsonProperty("suite_id")]
    public int SuiteId { get; set; }

    [JsonProperty("untested_count")]
    public int UntestedCount { get; set; }

    [JsonProperty("updated_on")]
    public object UpdatedOn { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}