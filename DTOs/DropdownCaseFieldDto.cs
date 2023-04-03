using System.Collections.Generic;
using Newtonsoft.Json;

namespace TrUtils.DTOs;

public class Config
{
    [JsonProperty("context")]
    public Context Context;

    [JsonProperty("options")]
    public Options Options;

    [JsonProperty("id")]
    public string Id;
}

public class Context
{
    [JsonProperty("is_global")]
    public bool IsGlobal;

    [JsonProperty("project_ids")]
    public List<int> ProjectIds;
}

public class Options
{
    [JsonProperty("is_required")]
    public bool IsRequired;

    [JsonProperty("default_value")]
    public string DefaultValue;

    [JsonProperty("items")]
    public string Items;
}

public class DropdownCaseFieldDto
{
    [JsonProperty("id")]
    public int Id;

    [JsonProperty("is_active")]
    public bool IsActive;

    [JsonProperty("type_id")]
    public int TypeId;

    [JsonProperty("name")]
    public string Name;

    [JsonProperty("system_name")]
    public string SystemName;

    [JsonProperty("label")]
    public string Label;

    [JsonProperty("description")]
    public string Description;

    [JsonProperty("configs")]
    public List<Config> Configs;

    [JsonProperty("display_order")]
    public int DisplayOrder;

    [JsonProperty("include_all")]
    public bool IncludeAll;

    [JsonProperty("template_ids")]
    public List<object> TemplateIds;
}
