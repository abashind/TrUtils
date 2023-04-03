using Newtonsoft.Json;

namespace TrUtils.DTOs;

public class SectionDto
{
    [JsonProperty("depth")]
    public int Depth { get; set; }

    [JsonProperty("display_order")]
    public int DisplayOrder { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("parent_id")]
    public object ParentId { get; set; }

    [JsonProperty("suite_id")]
    public int SuiteId { get; set; }
}