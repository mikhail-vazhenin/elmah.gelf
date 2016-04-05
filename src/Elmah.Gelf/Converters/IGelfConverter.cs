using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elmah.Gelf.Converters
{
    public interface IGelfConverter
    {
        string GetGelfJson(Error error, string facility, ICollection<string> ignoredProperties = null);
    }
}
