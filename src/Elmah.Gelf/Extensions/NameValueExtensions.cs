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
            var dict = new Dictionary<object, object>();

            foreach (string name in col)
            {
                var key = name;
                var value = col[name];
                dict.Add(key, value);
            }

            return dict;
        }
    }
}
