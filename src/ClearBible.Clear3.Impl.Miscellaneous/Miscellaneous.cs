using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Xml;
using System.Xml.Linq;

namespace ClearBible.Clear3.Impl.Miscellaneous
{
    public static class XmlExtensions
    {
        public static XElement ToXElement(this XmlNode node)
        {
            XDocument xDoc = new XDocument();
            using (XmlWriter xmlWriter = xDoc.CreateWriter())
                node.WriteTo(xmlWriter);
            return xDoc.Root;
        }

        public static XmlNode ToXmlNode(this XElement element)
        {
            using (XmlReader xmlReader = element.CreateReader())
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlReader);
                return xmlDoc.DocumentElement;
            }
        }

        public static string AttrAsString(
            this XElement element,
            string attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);
            return attribute is null
                ? ""
                : attribute.Value;
        }

        public static int AttrAsInt(
            this XElement element,
            string attributeName)
        {
            return int.Parse(element.Attribute(attributeName).Value);
        }
    }


    public static class SyntaxTreeNodeExtensions
    {
        /// <summary>
        /// Return the source ID for a terminal node.  The source ID is
        /// derived from the "morphId" attribute.  In the OT, the morphId
        /// has a subsegment number, but in the NT it does not.  So append
        /// the subsegment digit "1" if it is missing.
        /// </summary>
        /// 
        public static string SourceId(this XElement element)
        {
            string morphId = element.Attribute("morphId").Value;
            if (morphId.Length == 11) morphId += "1";
            return morphId;
        }
    }


    public static class LinqExtensions
    {
        /// <summary>
        /// Given a sequence x1, x2, ... and an analysis function f,
        /// produce a sequence (x1, n1), (x2, n2), ... where the ni's
        /// are version numbers that distinguish the xi's that have
        /// the same f(xi)'s.  For example, if the input is
        /// "a", "b", "A", "a", "B", ... and the analysis function f
        /// converts to lowercase, then the output is
        /// ("a", 1), ("b", 1), ("A", 2), ("a", 3), ("B", 2), ...
        /// </summary>
        /// 
        public static IEnumerable<Tuple<T, int>> WithVersionNumber<T, X>(
            this IEnumerable<T> sequence,
            Func<T, X> analysisFunction)
        {
            Dictionary<X, int> history = new Dictionary<X, int>();

            foreach (T item in sequence)
            {
                X analysis = analysisFunction(item);
                int versionNumber =
                    history.GetValueOrDefault(analysis, 0) + 1;
                history[analysis] = versionNumber;
                yield return Tuple.Create(item, versionNumber);
            }
        }
    }


    public class Miscellaneous
    {
        public static void Swap<T>(ref T x, ref T y)
        {
            T temp = x;
            x = y;
            y = temp;
        }
    }
}
