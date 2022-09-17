using System;
using System.Collections;
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
            if (!dict.ContainsKey(key))
                dict.Add(key, defFactory());

            return dict[key];
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TValue> defFactory)
            where TKey : notnull
        {
            if (!dict.ContainsKey(key))
                return dict.AddOrUpdate(key, (_) => defFactory(), (_, ex) => ex);

            return dict[key];
        }

        /// <summary>
        /// If given key exists, adds new item to the list in the dictionary under given key, if not, adds new List containing provided value
        /// </summary>
        /// <typeparam name="TKey">Type of dictionary key</typeparam>
        /// <typeparam name="TValue">Type of values stored in list</typeparam>
        /// <param name="dict">Dictionary to operate on</param>
        /// <param name="key">Key to look for</param>
        /// <param name="value">Velue to insert</param>
        public static void AddListItem<TKey, TValue>(this Dictionary<TKey, List<TValue>> dict, TKey key, TValue value)
            where TKey : notnull
        {
            if (dict.ContainsKey(key))
                dict[key].Add(value);
            else
                dict.Add(key, new List<TValue> { value });
        }

        public static IEnumerable<T> ExceptIn<T>(this IEnumerable<T> source, HashSet<T> others)
        {
            foreach (var item in source)
            {
                if (!others.Contains(item))
                    yield return item;
            }
        }

        public static IEnumerable<TValue> ExceptIn<TKey, TValue>(this Dictionary<TKey, TValue> source, HashSet<TKey> keys)
        {
            foreach (var item in source)
            {
                if (!keys.Contains(item.Key))
                    yield return item.Value;
            }
        }

        public static IReadOnlyCollection<T> AsReadOnly<T>(this ICollection<T> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            return source as IReadOnlyCollection<T> ?? new ReadOnlyCollectionAdapter<T>(source);
        }

        sealed class ReadOnlyCollectionAdapter<T> : IReadOnlyCollection<T>
        {
            readonly ICollection<T> source;
            public ReadOnlyCollectionAdapter(ICollection<T> source) => this.source = source;
            public int Count => source.Count;
            public IEnumerator<T> GetEnumerator() => source.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}