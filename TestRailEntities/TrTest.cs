using System;
using System.Collections.Generic;
using System.Linq;
using TrUtils.DTOs;

namespace TrUtils.TestRailEntities;

public class TrTest : TrBaseClass
{
    public readonly TestDto Dto;

    public TrTest(TestDto dto) => Dto = dto;

    public List<TrResult> GetResults()
    {
        var dtos = GetTestRailItemsWithPagination($"get_results/{Dto.Id}", "results")?
            .ToObject<List<ResultDto>>();
        return dtos?.Select(d => new TrResult(d)).ToList();
    }

    public void AttachScreenshot(string screenshot)
    {
        if (screenshot is null)
        {
            Console.WriteLine($"No screenshot for '{Dto.Title}' was found.");
            return;
        }

        var result = GetResults().First();
        result.AddAttachment(screenshot, Dto.Title);
    }
}