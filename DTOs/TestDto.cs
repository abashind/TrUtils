using Newtonsoft.Json;

namespace TrUtils.DTOs;

public class TestDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("case_id")]
    public int CaseId { get; set; }

    [JsonProperty("status_id")]
    public int StatusId { get; set; }
}