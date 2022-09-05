using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Common.Extensions
{
    public static class CollectionExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> defFactory)
            where TKey : notnull
        {
            if(!dict.ContainsKey(key))
                dict.Add(key, defFactory());

            return dict[key];
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TValue> defFactory)
            where TKey : notnull
        {
            if(!dict.ContainsKey(key))
                return dict.AddOrUpdate(key, (_) => defFactory(), (_, ex) => ex);

            return dict[key];
        }

        public static IEnumerable<T> ExceptIn<T>(this IEnumerable<T> source, HashSet<T> others)
        {
            foreach(var item in source)
            {
                if(!others.Contains(item))
                    yield return item;
            }
        }

        public static IEnumerable<TValue> ExceptIn<TKey, TValue>(this Dictionary<TKey, TValue> source, HashSet<TKey> keys)
        {
            foreach(var item in source)
            {
                if(!keys.Contains(item.Key))
                    yield return item.Value;
            }
        }
    }
}