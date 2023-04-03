using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using TrUtils.DTOs;
using TrUtils.TestRailEntities;

namespace TrUtils;

public class TrStatic : TrBaseClass
{
    public static JArray GetResultFields()
    {
        var result = (JArray)TrClient.SendGet("get_result_fields");
        Console.WriteLine(result.ToString());
        return result;
    }

    private static JArray? _caseFields;
    public static JArray CaseFields => 
        _caseFields ??= (JArray)TrClient.SendGet("get_case_fields");

    public static TrDropdownCaseField GetCaseDropdownField(string fieldName)
    {
        var allFields = CaseFields.Select(f => f.ToObject<DropdownCaseFieldDto>()).ToList();
        var fieldDto = allFields.FirstOrDefault(f => f.Label.Equals(fieldName));
        return new TrDropdownCaseField(fieldDto);
    }

    public static CaseDto GetCase(int caseId)
    {
        var response = TrClient.SendGet($"get_case/{caseId}") as JObject;
        return response?.ToObject<CaseDto>();
    }

    public static int GetStatusId(TestStatus status)
    {
        var response = (JArray)TrClient.SendGet("get_statuses");
        return response.FirstOrDefault(s => s["label"].ToString().Contains(status.Value))?["id"]?.ToObject<int>() ??
               throw new Exception($"Unable to find the '{status.Value}' status.");
    }

    private static int _failedStatusId = int.MinValue;
    public static int FailedStatusId
    {
        get
        {
            if(_failedStatusId is int.MinValue)
                _failedStatusId = GetStatusId(TestStatus.Failed);
            return _failedStatusId;
        }
    }

    private static int _passedStatusId = int.MinValue;
    public static int PassedStatusId
    {
        get
        {
            if (_passedStatusId is int.MinValue)
                _passedStatusId = GetStatusId(TestStatus.Passed);
            return _passedStatusId;
        }
    }

    private static int _bugStatusId = int.MinValue;
    public static int BugStatusId
    {
        get
        {
            if (_bugStatusId is int.MinValue)
                _bugStatusId = GetStatusId(TestStatus.BugAqa);
            return _bugStatusId;
        }
    }

    private static int _passedOnRerunStatusId = int.MinValue;
    public static int PassedOnRetestStatusId
    {
        get
        {
            if (_passedOnRerunStatusId is int.MinValue)
                _passedOnRerunStatusId = GetStatusId(TestStatus.PassedOnRetest);
            return _passedOnRerunStatusId;
        }
    }
}