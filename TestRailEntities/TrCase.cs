using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TrUtils.DTOs;

namespace TrUtils.TestRailEntities;

public class TrCase : TrBaseClass
{
    public readonly CaseDto Dto;

    public TrCase(CaseDto dto) => Dto = dto;

    public List<ResultDto> GetResults(int runId) =>
        GetTestRailItemsWithPagination($"get_results_for_case/{runId}/{Dto.Id}", "results")?
            .ToObject<List<ResultDto>>();

    public JObject AddResult(int runId, ResultDto result) =>
        TrClient.SendPost($"add_result_for_case/{runId}/{Dto.Id}", JsonConvert.SerializeObject(result)) as JObject;

    public void UpdateFailedAttributes()
    {
        var body = new
        {
            custom_flaky = Dto.IsFlaky,
            custom_firsttimefail = Dto.FirstTimeFailed,
            custom_everydayfail = Dto.FailsEveryDay
        };

        if (TrClient.SendPost($"update_case/{Dto.Id}", body) is null)
            Console.WriteLine($"Unable to update the '{Dto.Title}' case");
    }

    public void UpdateFailRateAndFailsSince()
    {
        var body = new
        {
            custom_failrate = Dto.FailRate,
            custom_failssince = Dto.FailsSince
        };

        if (TrClient.SendPost($"update_case/{Dto.Id}", body) is null)
            Console.WriteLine($"Unable to update the '{Dto.Title}' case");
    }
}