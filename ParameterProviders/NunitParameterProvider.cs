using NUnit.Framework;
using TrUtils.Interfaces;

namespace TrUtils.ParameterProviders;

public class NunitParameterProvider : IParameterProvider
{
    public NunitParameterProvider(int priority) => 
        Priority = priority;

    public int Priority { get; }

    public string GetParameter(string parameterName) => 
        TestContext.Parameters.Get(parameterName, null);
}