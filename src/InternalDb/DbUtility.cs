using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.InternalDb
{
    public class DbUtility
    {
        private static SHA1 sha = new SHA1CryptoServiceProvider();


        public static string Hash(string subject)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(subject);
            byte[] hashed = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hashed);
        }


        public static string MakeKey(object x)
        {
            switch (x)
            {
                case string s:
                    return ":" + Hash(s);

                case IEnumerable<string> subkeys:
                    return MakeKey(String.Concat(subkeys));

                default:
                    return ":" + $"{x}";
            }
        }


        public static string MakeKey(params object[] xs) =>
            MakeKey(xs.Select(x => MakeKey(x)));


        public static T LookupByKey<T>(string key, Dictionary<string, T> index)
        {
            if (String.IsNullOrWhiteSpace(key))
            {
                throw new ClearException(
                    "null or blank key",
                    StatusCode.NullOrBlankKey,
                    null);
            }
            if (!index.TryGetValue(key, out T subject))
            {
                throw new ClearException(
                    "key is not present",
                    StatusCode.KeyIsNotPresent,
                    null);
            }
            return subject;
        }


        public static T LookupOrCreate<T>(
            Dictionary<string, T> index,
            Func<string> makeKey,
            Func<string, T> makeSubject)
        {
            string key = makeKey();
            if (!index.TryGetValue(key, out T subject))
            {
                subject = makeSubject(key);
                index[key] = subject;
            }
            return subject;
        }
    }



    public class ImmutableSortedDoubleDict<TKey1, TKey2, TValue>
        : IEnumerable<Tuple<TKey1, TKey2, TValue>>
    {
        private ImmutableSortedDictionary<TKey1,
            ImmutableSortedDictionary<TKey2, TValue>> _inner;

        private Tuple<
            IComparer<TKey1>,
            IComparer<TKey2>,
            IEqualityComparer<TValue>>
            _misc;

        private ImmutableSortedDoubleDict(
            ImmutableSortedDictionary<TKey1,
                ImmutableSortedDictionary<TKey2, TValue>> inner,
            Tuple<
            IComparer<TKey1>,
            IComparer<TKey2>,
            IEqualityComparer<TValue>> misc)
        {
            _inner = inner;
            _misc = misc;
        }

        private ImmutableSortedDoubleDict<TKey1, TKey2, TValue> _update(
            ImmutableSortedDictionary<TKey1,
                ImmutableSortedDictionary<TKey2, TValue>> inner)
        {
            return new ImmutableSortedDoubleDict<TKey1, TKey2, TValue>(inner, _misc);
        }

        public static ImmutableSortedDoubleDict<TKey1, TKey2, TValue> Empty(
            IComparer<TKey1> key1Comparer,
            IComparer<TKey2> key2Comparer,
            IEqualityComparer<TValue> valueComparer)
        {
            var misc = Tuple.Create(key1Comparer, key2Comparer, valueComparer);
            var inner = ImmutableSortedDictionary.Create<TKey1,
                ImmutableSortedDictionary<TKey2, TValue>>();
            return new ImmutableSortedDoubleDict<TKey1, TKey2, TValue>(inner, misc);
        }

        public int Count => _inner.Sum(kvp => kvp.Value.Count);

        public bool IsEmpty => _inner.IsEmpty;

        public ImmutableSortedDictionary<TKey2, TValue> this[TKey1 key1] =>
            _inner[key1];

        public IComparer<TKey1> Key1Comparer => _misc.Item1;

        public IComparer<TKey2> Key2Comparer => _misc.Item2;

        public IEqualityComparer<TValue> ValueComparer => _misc.Item3;

        public IEnumerable<TKey1> Key1s => _inner.Keys;

        public IEnumerable<TKey2> Key2s(TKey1 key1)
        {
            if (_inner.TryGetValue(key1, out var _subInner))
            {
                return _subInner.Keys;
            }
            else
            {
                return Enumerable.Empty<TKey2>();
            }
        }

        public IEnumerable<TValue> Values(TKey1 key1)
        {
            if (_inner.TryGetValue(key1, out var _subInner))
            {
                return _subInner.Values;
            }
            else
            {
                return Enumerable.Empty<TValue>();
            }
        }

        public IEnumerable<KeyValuePair<TKey2, TValue>> KeyValuePairs(TKey1 key1)
        {
            if (_inner.TryGetValue(key1, out var subInner))
            {
                return subInner;
            }
            else
            {
                return Enumerable.Empty<KeyValuePair<TKey2, TValue>>();
            }
        }

        public IEnumerator<Tuple<TKey1, TKey2, TValue>> GetEnumerator()
        {
            foreach (var kvp1 in _inner)
            {
                foreach (var kvp2 in kvp1.Value)
                {
                    yield return Tuple.Create(kvp1.Key, kvp2.Key, kvp2.Value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ImmutableSortedDoubleDict<TKey1, TKey2, TValue> Set(
            TKey1 key1,
            ImmutableSortedDictionary<TKey2, TValue> subInner)
        {
            return _update(_inner.SetItem(key1, subInner));
        }

        public ImmutableSortedDoubleDict<TKey1, TKey2, TValue> Set(
            TKey1 key1,
            TKey2 key2,
            TValue val)
        {
            if (!_inner.TryGetValue(key1, out var subInner))
            {
                subInner = ImmutableSortedDictionary.Create<TKey2, TValue>(
                    Key2Comparer,
                    ValueComparer);
            }
            subInner = subInner.SetItem(key2, val);
            return _update(_inner.SetItem(key1, subInner));
        }

        public ImmutableSortedDoubleDict<TKey1, TKey2, TValue> Clear() =>
            _update(_inner.Clear());

        public bool Contains(Tuple<TKey1, TKey2, TValue> item) =>
            _inner.ContainsKey(item.Item1) &&
            _inner[item.Item1].Contains(new KeyValuePair<TKey2, TValue>(
                item.Item2,
                item.Item3));

        public bool ContainsKey(TKey1 key1) =>
            _inner.ContainsKey(key1);

        public bool ContainsKey(TKey1 key1, TKey2 key2) =>
            _inner.ContainsKey(key1) &&
            _inner[key1].ContainsKey(key2);

        public bool ContainsValue(TValue val) =>
            _inner.Any(kvp =>
                kvp.Value == null ? val == null : kvp.Value.Equals(val));

        public bool ContainsValue(KeyValuePair<TKey2, TValue> subKvp) =>
            _inner.Any(sub => sub.Value.Contains(subKvp));

        public ImmutableSortedDoubleDict<TKey1, TKey2, TValue>
            Remove(TKey1 key1) =>
                ContainsKey(key1)
                    ? _update(_inner.Remove(key1))
                    : this;

        public ImmutableSortedDoubleDict<TKey1, TKey2, TValue>
            Remove(TKey1 key1, TKey2 key2)
        {
            if (_inner.TryGetValue(key1, out var subInner))
            {
                var subInner2 = subInner.Remove(key2);
                if (subInner2.Count == 0)
                {
                    return _update(_inner.Remove(key1));
                }
                else if (subInner2.Count < subInner.Count)
                {
                    return _update(_inner.SetItem(key1, subInner2));
                }
                else
                {
                    return this;
                }
            }
            else
            {
                return this;
            }
        }

        public bool TryGetValue(
            TKey1 key1,
            out ImmutableSortedDictionary<TKey2, TValue> subDictionary)
        {
            return _inner.TryGetValue(key1, out subDictionary);
        }

        public bool TryGetValue(
            TKey1 key1,
            TKey2 key2,
            out TValue val)
        {
            if (_inner.TryGetValue(key1, out var subInner))
            {
                return subInner.TryGetValue(key2, out val);
            }
            else
            {
                val = default(TValue);
                return false;
            }
        }
    }
}


