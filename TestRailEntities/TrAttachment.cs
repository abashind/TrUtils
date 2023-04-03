using TrUtils.DTOs;

namespace TrUtils.TestRailEntities;

public class TrAttachment : TrBaseClass
{
    public AttachmentDto Dto;
    public TrAttachment(AttachmentDto dto) => Dto = dto;

    public bool SaveLocally(string fileName)
    {
        var result = TrClient.SendGet($"get_attachment/{Dto.Id}", fileName);
        return result is not null;
    }

    public bool DeleteFromTestrail()
    {
        var result = TrClient.SendPost($"delete_attachment/{Dto.Id}", null);
        return result is not null;
    }
}