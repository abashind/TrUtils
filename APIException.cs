using System;
using System.Runtime.Serialization;

namespace TrUtils;

[Serializable]
public class APIException: Exception
{
    public APIException()
    {
    }

    public APIException(string message): base(message)
    {
    }

    public APIException(string message,
        Exception innerException): base(message, innerException)
    {
    }

    protected APIException(SerializationInfo info,
        StreamingContext context): base(info, context)
    {
    }
}