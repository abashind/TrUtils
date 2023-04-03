using Newtonsoft.Json;

namespace TrUtils.DTOs;

public class ResultDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("test_id")]
    public int TestId { get; set; }

    [JsonProperty("comment")]
    public string Comment { get; set; }

    [JsonProperty("status_id")]
    public int StatusId { get; set; }

    [JsonProperty("custom_exception")]
    public string CustomException { get; set; }

    [JsonProperty("defects")]
    public string Defects { get; set; }
}