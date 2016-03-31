using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Elmah.Gelf.Converters
{
    public interface IConverter
    {
        JObject GetGelfJson(LogEventInfo logEventInfo, string facility);
    }
}
