using System.Runtime.Serialization;

namespace LMKR.Shared.Logging.Exceptions;

[Serializable]
public class AppException : Exception
{
    protected AppException(SerializationInfo info,
     StreamingContext context) : base(info, context)
    {
    }
    public AppException() : base() { }

    public AppException(string message)
       : base(message)
    {
    }

    public AppException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}