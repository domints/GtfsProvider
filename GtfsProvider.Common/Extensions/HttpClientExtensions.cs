using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GtfsProvider.Common.Attributes;
using Newtonsoft.Json;

namespace GtfsProvider.Common.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<TRs?> PostFormToGetJson<TRq, TRs>(this HttpClient client, string url, TRq request, CancellationToken cancellationToken)
            where TRq : new()
            where TRs : new()
        {
            var rqType = typeof(TRq);
            var props = rqType.GetProperties();

            var paramDict = new Dictionary<string, string>();
            foreach (var prop in props)
            {
                var valueObj = prop.GetValue(request);
                if (valueObj == null)
                    continue;

                var paramAttr = (ParamAttribute?)Attribute.GetCustomAttribute(prop, typeof(ParamAttribute));
                var key = paramAttr?.ParamName ?? prop.Name;
                var value = Convert.ToString(valueObj)!;
                paramDict.Add(key, value);
            }

            var response = await client.PostAsync(url, new FormUrlEncodedContent(paramDict), cancellationToken);
            if (!response.IsSuccessStatusCode)
                return default;

            var responseContent = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TRs>(responseContent);
        }

        public static async Task<TRs?> GetJson<TRs>(this HttpClient client, string url, CancellationToken cancellationToken)
            where TRs : new()
        {
            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return default;

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return JsonConvert.DeserializeObject<TRs>(responseContent);
        }

        public static Task<TRs?> GetJson<TRq, TRs>(this HttpClient client, string url, CancellationToken cancellationToken)
            where TRq : new()
            where TRs : new()
        {
            return client.GetJson<TRq, TRs>(url, System.Activator.CreateInstance<TRq>(), cancellationToken);
        }

        public static async Task<TRs?> GetJson<TRq, TRs>(this HttpClient client, string url, TRq request, CancellationToken cancellationToken)
            where TRq : new()
            where TRs : new()
        {
            var rqType = typeof(TRq);
            var props = rqType.GetProperties();

            StringBuilder paramBuilder = new StringBuilder();
            foreach (var prop in props)
            {
                var valueObj = prop.GetValue(request);
                if (valueObj == null)
                    continue;

                var paramAttr = (ParamAttribute?)Attribute.GetCustomAttribute(prop, typeof(ParamAttribute));
                var key = paramAttr?.ParamName ?? prop.Name;
                var value = Convert.ToString(valueObj)!;
                if (paramBuilder.Length == 0)
                {
                    paramBuilder.Append(url);
                    paramBuilder.Append('?');
                }
                else
                {
                    paramBuilder.Append('&');
                }

                paramBuilder.AppendFormat("{0}={1}", key, value);
            }

            var response = await client.GetAsync(paramBuilder.ToString(), cancellationToken);
            if (!response.IsSuccessStatusCode)
                return default;

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return JsonConvert.DeserializeObject<TRs>(responseContent);
        }
    }
}