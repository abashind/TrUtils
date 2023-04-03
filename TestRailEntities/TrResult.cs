using System;
using System.IO;
using Newtonsoft.Json.Linq;
using TrUtils.DTOs;
using TrUtils.Extensions;

namespace TrUtils.TestRailEntities;

public class TrResult : TrBaseClass
{
    public readonly ResultDto Dto;

    public TrResult(ResultDto dto) => Dto = dto;

    public void AddAttachment(string attachmentPath, string testName = "")
    {
        var response = (JObject)TrClient.SendPost($"add_attachment_to_result/{Dto.Id}", attachmentPath);

        if (!response.TryGetValue("attachment_id", out _))
        {
            Console.WriteLine($"Unable to add attachment to '{testName}'");
            return;
        }

        Console.WriteLine($"'{Path.GetExtension(attachmentPath)}' file was attached to the '{testName}' test.");
    }

    public void AssignException()
    {
        if (Dto.Comment.IsNullOrEmpty())
        {
            Dto.CustomException = "";
            return;
        }

        var comment = Dto.Comment.ReplaceUtfForComment();
        Dto.Comment = comment;

        var exception = comment.CutBeforePhrase(Parameters.ExceptionStartPhrase).CutAfterPhrase(Parameters.ExceptionStopPhrase).Trim();
        exception = exception.Length >= 249 ? exception.Remove(249) : exception;
        Dto.CustomException = exception;
    }
}