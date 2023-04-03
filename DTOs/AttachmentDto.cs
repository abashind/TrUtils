using Newtonsoft.Json;

namespace TrUtils.DTOs;

public class AttachmentDto
{
    [JsonProperty("client_id")]
    public int ClientId { get; set; }

    [JsonProperty("project_id")]
    public int ProjectId { get; set; }

    [JsonProperty("entity_type")]
    public string EntityType { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("created_on")]
    public int CreatedOn { get; set; }

    [JsonProperty("data_id")]
    public string DataId { get; set; }

    [JsonProperty("entity_id")]
    public string EntityId { get; set; }

    [JsonProperty("filename")]
    public string Filename { get; set; }

    [JsonProperty("filetype")]
    public string Filetype { get; set; }

    [JsonProperty("legacy_id")]
    public int LegacyId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("user_id")]
    public int UserId { get; set; }

    [JsonProperty("is_image")]
    public bool IsImage { get; set; }

    [JsonProperty("icon")]
    public string Icon { get; set; }

    [JsonProperty("run_id")]
    public int RunId { get; set; }
}