using System;
using System.Collections.Generic;
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
}