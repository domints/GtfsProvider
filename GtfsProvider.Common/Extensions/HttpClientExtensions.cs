using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Attributes;
using Newtonsoft.Json;

namespace GtfsProvider.Common.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<TRs?> PostFormToGetJson<TRq, TRs>(this HttpClient client, string url, TRq request)
            where TRq : new()
            where TRs : new()
        {
            var rqType = typeof(TRq);
            var props = rqType.GetProperties();

            var paramDict = new Dictionary<string, string>();
            foreach(var prop in props)
            {
                var valueObj = prop.GetValue(request);
                if (valueObj == null)
                    continue;

                var paramAttr = (ParamAttribute?)Attribute.GetCustomAttribute(prop, typeof(ParamAttribute));
                var key = paramAttr?.ParamName ?? prop.Name;
                var value = Convert.ToString(valueObj)!;
                paramDict.Add(key, value);
            }

            var response = await client.PostAsync(url, new FormUrlEncodedContent(paramDict));
            if (!response.IsSuccessStatusCode)
                return default;

            var responseContent = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TRs>(responseContent);
        }
    }
}