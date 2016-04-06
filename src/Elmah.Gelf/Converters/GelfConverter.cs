using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Elmah.Gelf.Extensions;

namespace Elmah.Gelf.Converters
{
    public class GelfConverter : IGelfConverter
    {
        private const int ShortMessageMaxLength = 250;
        private const string GelfVersion = "1.0";

        public string GetGelfJson(Error error, string facility, ICollection<string> ignoredProperties = null)
        {
            if (error == null) return null;

            StackFrame stackFrame = null;
            //If we are dealing with an exception, pass exception properties
            if (error.Exception != null)
            {
                stackFrame = new StackTrace(error.Exception, true).GetFrame(0);
            }

            //Figure out the short message
            var shortMessage = error.Message;
            if (shortMessage.Length > ShortMessageMaxLength)
            {
                shortMessage = shortMessage.Substring(0, ShortMessageMaxLength);
            }

            var stringWriter = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(stringWriter);
            writer.Object();

            //Construct the instance of GelfMessage
            //See https://github.com/Graylog2/graylog2-docs/wiki/GELF "Specification (version 1.0)"
            //Standard specification field
            writer.Member("version").String(GelfVersion);
            writer.Member("host").String(Dns.GetHostName());
            writer.Member("short_message").String(shortMessage);
            writer.Member("full_message").String(error.Message);
            writer.Member("timestamp").String(error.Time);
            writer.Member("level").String(3.ToString());
            writer.Member("facility").String((string.IsNullOrEmpty(facility) ? "GELF" : facility));
            writer.Member("line").String((stackFrame != null) ? stackFrame.GetFileLineNumber().ToString(CultureInfo.InvariantCulture) : string.Empty);
            writer.Member("file").String((stackFrame != null) ? stackFrame.GetFileName() : string.Empty);

            var properties = new Dictionary<object, object>();

            //Filling elmah properties

            string exceptionDetail;
            string stackDetail;
            GetExceptionMessages(error.Exception, out exceptionDetail, out stackDetail);
            properties.Add("ExceptionSource", error.Exception.Source);
            properties.Add("ExceptionMessage", exceptionDetail);
            properties.Add("StackTrace", stackDetail);


            properties.Add("application", error.ApplicationName);
            properties.Add("host", error.HostName);
            properties.Add("type", error.Type);
            properties.Add("message", error.Message);
            properties.Add("source", error.Source);
            properties.Add("detail", error.Detail);
            properties.Add("user", error.User);
            properties.Add("time", error.Time);
            properties.Add("statusCode", error.StatusCode);
            properties.Add("webHostHtmlMessage", error.WebHostHtmlMessage);

            properties.Add("queryString", error.QueryString.ToString("&", (k, v) => string.Format("{0}={1}", k, v)));
            properties.Add("form", error.Form.ToString(Environment.NewLine, (k, v) => string.Format("{0}={1}", k, v)));
            properties.Add("cookies", error.Cookies.ToString(Environment.NewLine, (k, v) => string.Format("{0}={1}", k, v)));

            foreach (var v in error.ServerVariables.ToDictionary())
            {
                properties.Add(v.Key, v.Value);
            }




            //Property filtering if is necessary
            if (ignoredProperties != null && ignoredProperties.Any())
            {
                properties = properties.Where(p => !ignoredProperties.Contains(p.Key.ToString())).ToDictionary(k => k.Key, v => v.Value);
            }

            //Convert to JSON
            //We will persist them "Additional Fields" according to Gelf spec
            foreach (var p in properties)
            {
                AddAdditionalField(writer, p);
            }

            writer.Pop();

            return stringWriter.ToString();
        }

        private void AddAdditionalField(JsonTextWriter writer, KeyValuePair<object, object> property)
        {
            var key = property.Key as string;
            if (key == null) return;

            //According to the GELF spec, libraries should NOT allow to send id as additional field (_id)
            //Server MUST skip the field because it could override the MongoDB _key field
            if (key.Equals("id", StringComparison.OrdinalIgnoreCase))
                key = "id_";

            //According to the GELF spec, additional field keys should start with '_' to avoid collision
            if (!key.StartsWith("_", StringComparison.OrdinalIgnoreCase))
                key = "_" + key;

            var value = property.Value != null ? property.Value.ToString() : null;
            if (!string.IsNullOrEmpty(value))
                writer.Member(key).String(property.Value.ToString());
        }


        /// <summary>
        /// Get the message details from all nested exceptions, up to 10 in depth.
        /// </summary>
        /// <param name="ex">Exception to get details for</param>
        /// <param name="exceptionDetail">Exception message</param>
        /// <param name="stackDetail">Stacktrace with inner exceptions</param>
        private void GetExceptionMessages(Exception ex, out string exceptionDetail, out string stackDetail)
        {
            var exceptionSb = new StringBuilder();
            var stackSb = new StringBuilder();
            var nestedException = ex;
            stackDetail = null;

            int counter = 0;
            do
            {
                exceptionSb.Append(nestedException.Message + " - ");
                if (nestedException.StackTrace != null)
                    stackSb.Append(nestedException.StackTrace + "--- Inner exception stack trace ---");
                nestedException = nestedException.InnerException;
                counter++;
            }
            while (nestedException != null && counter < 11);

            exceptionDetail = exceptionSb.ToString().Substring(0, exceptionSb.Length - 3);
            if (stackSb.Length > 0)
                stackDetail = stackSb.ToString().Substring(0, stackSb.Length - 35);
        }
    }
}
