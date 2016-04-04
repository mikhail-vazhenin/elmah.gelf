using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Elmah.Gelf.Converters
{
    public interface IGelfConverter
    {
        JObject GetGelfJson(Error logEventInfo, string facility);
    }
}
