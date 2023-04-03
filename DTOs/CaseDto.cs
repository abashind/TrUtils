using Newtonsoft.Json;

namespace TrUtils.DTOs;

public class CaseDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("section_id")]
    public int SectionId { get; set; }

    [JsonProperty("template_id")]
    public int TemplateId { get; set; }

    [JsonProperty("type_id")]
    public int TypeId { get; set; }

    [JsonProperty("priority_id")]
    public int PriorityId { get; set; }

    [JsonProperty("milestone_id")]
    public object MilestoneId { get; set; }

    [JsonProperty("refs")]
    public string Refs { get; set; }

    [JsonProperty("created_by")]
    public int CreatedBy { get; set; }

    [JsonProperty("created_on")]
    public int CreatedOn { get; set; }

    [JsonProperty("updated_by")]
    public int UpdatedBy { get; set; }

    [JsonProperty("updated_on")]
    public int UpdatedOn { get; set; }

    [JsonProperty("estimate")]
    public object Estimate { get; set; }

    [JsonProperty("estimate_forecast")]
    public string EstimateForecast { get; set; }

    [JsonProperty("suite_id")]
    public int SuiteId { get; set; }

    [JsonProperty("display_order")]
    public int DisplayOrder { get; set; }

    [JsonProperty("is_deleted")]
    public int IsDeleted { get; set; }

    [JsonProperty("custom_flaky")]
    public bool? IsFlaky { get; set; }

    [JsonProperty("custom_firsttimefail")]
    public bool? FirstTimeFailed { get; set; }

    [JsonProperty("custom_everydayfail")]
    public bool? FailsEveryDay { get; set; }

    [JsonProperty("custom_failrate")]
    public int? FailRate { get; set; }

    [JsonProperty("custom_failsonreferenceproject")]
    public string FailsOnReferenceProject { get; set; }

    [JsonProperty("custom_teamassigned")]
    public int? TeamAssigned { get; set; }

    [JsonProperty("custom_failssince")]
    public string FailsSince { get; set; }
}