using System.IO;
using Newtonsoft.Json.Linq;
using TrUtils.Interfaces;

namespace TrUtils.ParameterProviders;

public class JsonParameterProvider : IParameterProvider
{
    public JsonParameterProvider(int priority, string fileName)
    {
        Priority = priority;
        _fileName = fileName;
    }

    public int Priority { get; }

    private readonly string _fileName;

    public string GetParameter(string parameterName)
    {
        var parameters = JObject.Parse(File.ReadAllText(_fileName));

        return parameters.TryGetValue(parameterName, out var parameter) ? 
            parameter.ToObject<string>() : 
            null;
    }
}