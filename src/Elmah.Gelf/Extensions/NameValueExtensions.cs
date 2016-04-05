using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Elmah.Gelf.Extensions
{
    public static class NameValueExtensions
    {
        public static Dictionary<object, object> ToDictionary(this NameValueCollection col)
        {
            if (col == null) return new Dictionary<object, object>();

            var dict = new Dictionary<object, object>();

            foreach (string name in col)
            {
                var key = name;
                var value = col[name];
                dict.Add(key, value);
            }

            return dict;
        }

        /// <summary>
        /// Join keys and values in 
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public static string ToString(this NameValueCollection col, string separator, Func<object, object, string> format)
        {
            if (col == null) return string.Empty;

            var keyValues = new Dictionary<object, object>();

            foreach (string name in col)
            {
                var key = name;
                var value = col[name];
                keyValues.Add(key, value);
            }
            
            var formatted = string.Join(separator, keyValues.Select((k, v) => format(k, v)).ToArray());

            return formatted;
        }
    }
}
