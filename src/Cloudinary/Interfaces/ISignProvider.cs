using System.Collections.Generic;

namespace CloudinaryDotNet
{
    public interface ISignProvider
    {
        string SignParameters(IDictionary<string, object> parameters);
        string SignUriPart(string uriPart);
    }
}
