using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Globalization;
using System.Diagnostics;
using Elmah.Gelf.Extensions;

namespace Elmah.Gelf.Converters
{
    public class GelfConverter : IGelfConverter
    {
        private const int ShortMessageMaxLength = 250;
        private const string GelfVersion = "1.0";

        public JObject GetGelfJson(Error error, string facility)
        {
            //Retrieve the formatted message from LogEventInfo
            if (error == null) return null;

            var properties = new Dictionary<object, object>();
            StackFrame stackFrame = null;

            //If we are dealing with an exception, pass exception properties to LogEventInfo properties
            if (error.Exception != null)
            {
                stackFrame = new StackTrace(error.Exception, true).GetFrame(0);

                string exceptionDetail;
                string stackDetail;

                GetExceptionMessages(error.Exception, out exceptionDetail, out stackDetail);

                properties.Add("ExceptionSource", error.Exception.Source);
                properties.Add("ExceptionMessage", exceptionDetail);
                properties.Add("StackTrace", stackDetail);
            }


            properties.Add("ApplicationName", error.ApplicationName);
            properties.Add("Detail", error.Detail);
            properties.Add("ClientHostName", error.HostName);
            properties.Add("Source", error.Source);
            properties.Add("StatusCode", error.StatusCode);
            properties.Add("ErrorType", error.Type);
            properties.Add("WebHostMessage", error.WebHostHtmlMessage);
            properties.Add("User", error.User);

            //Figure out the short message
            var shortMessage = error.Message;
            if (shortMessage.Length > ShortMessageMaxLength)
            {
                shortMessage = shortMessage.Substring(0, ShortMessageMaxLength);
            }

            //Construct the instance of GelfMessage
            //See https://github.com/Graylog2/graylog2-docs/wiki/GELF "Specification (version 1.0)"
            var gelfMessage = new GelfMessage
            {
                Version = GelfVersion,
                Host = Dns.GetHostName(),
                ShortMessage = shortMessage,
                FullMessage = error.Message,
                Timestamp = error.Time,
                Level = 3, //LogLevel.Error
                Facility = (string.IsNullOrEmpty(facility) ? "GELF" : facility),   //Spec says: facility must be set by the client to "GELF" if empty
                Line = (stackFrame != null) ? stackFrame.GetFileLineNumber().ToString(CultureInfo.InvariantCulture) : string.Empty,
                File = (stackFrame != null) ? stackFrame.GetFileName() : string.Empty,
            };

            //Convert to JSON
            var jsonObject = JObject.FromObject(gelfMessage);

            //Add any other interesting data to properties
            //error.Properties.Add("LoggerName", error.LoggerName);

            properties.Add("cookies", string.Join(Environment.NewLine, error.Cookies.ToDictionary().Select(c => string.Format("{0}={1}", c.Key, c.Value))));

            foreach (var form in error.Form.ToDictionary())
            {
                AddAdditionalField(jsonObject, form);
            }

            properties.Add("query_string", string.Join("&", error.QueryString.ToDictionary().Select(c => string.Format("{0}={1}", c.Key, c.Value))));


            foreach (var v in error.ServerVariables.ToDictionary())
            {
                AddAdditionalField(jsonObject, v);
            }

            




            //We will persist them "Additional Fields" according to Gelf spec
            foreach (var property in properties)
            {
                AddAdditionalField(jsonObject, property);
            }

            return jsonObject;
        }

        private static void AddAdditionalField(IDictionary<string, JToken> jObject, KeyValuePair<object, object> property)
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

            JToken value = null;
            if (property.Value != null)
                value = JToken.FromObject(property.Value);

            jObject.Add(key, value);
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
