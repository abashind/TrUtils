namespace TrUtils.Interfaces;

public interface IParameterProvider
{
    /// <summary>
    /// The lowest has the most priority.
    /// </summary>
    public int Priority { get; }

    public string GetParameter(string parameterName);
}