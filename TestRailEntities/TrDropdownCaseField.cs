using System.Collections.Generic;
using System.Linq;
using TrUtils.DTOs;

namespace TrUtils.TestRailEntities;

public class TrDropdownCaseField
{
    public readonly DropdownCaseFieldDto Dto;

    public TrDropdownCaseField(DropdownCaseFieldDto dto) => Dto = dto;

    public IEnumerable<(int itemNumber, string itemName)> Items => 
        Dto.Configs[0].Options.Items
            .Split('\n').Select(i => (itemNumber: int.Parse(i.Split(',').First()), itemName: i.Split(',').Last()));
}