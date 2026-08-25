using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MythKit.Utils
{
    public static class QueryStringHelper
    {
        public static NameValueCollection ParseQueryString(string query)
        {
            var result = new NameValueCollection();
            if (string.IsNullOrEmpty(query)) return result;

            if (query.StartsWith("?")) query = query.Substring(1);

            var pairs = query.Split('&');
            foreach (var pair in pairs)
            {
                if (string.IsNullOrEmpty(pair)) continue;

                var kv = pair.Split('=');
                if (kv.Length == 2)
                {
                    string key = WebUtility.UrlDecode(kv[0]);
                    string value = WebUtility.UrlDecode(kv[1]);
                    result.Add(key, value);
                }
                else if (kv.Length == 1 && !string.IsNullOrEmpty(kv[0]))
                {
                    string key = WebUtility.UrlDecode(kv[0]);
                    result.Add(key, null);
                }
            }
            return result;
        }
    }

}
