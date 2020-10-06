using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using Newtonsoft.Json;

using Utilities;


namespace GBI_Aligner
{
    class TimUtil
    {
        public static string DebugTreeToString(XmlNode xmlNode)
        {
            if (xmlNode.FirstChild.NodeType.ToString() == "Text") // terminal node
            {
                return $"[{xmlNode.InnerText}] ";
            }
            else
            {
                return "(" + String.Concat(xmlNode.ChildNodes
                    .Cast<XmlNode>()
                    .Select(n => DebugTreeToString(n)))
                    + " )";
            }
        }

        public static string DebugAlignmentsToString(Hashtable alignments)
        {
            string renderAlignments(object alignments) =>
                "(" +
                String.Concat(((ArrayList)alignments)
                    .Cast<object>()
                    .Select(o => o.GetType().ToString() + " ")) + " )";

            return String.Concat(alignments
                .Cast<DictionaryEntry>()
                .Select(kvp => $"({kvp.Key} {renderAlignments(kvp.Value)} "));
        }

        public static void PrintArrayList(string name, ArrayList arrayList)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (JsonWriter w = new JsonTextWriter(sw))
                {
                    JsonSerializer s = JsonSerializer.CreateDefault();
                    foreach (object obj in arrayList)
                    {
                        s.Serialize(w, obj);
                        sw.WriteLine();
                    }
                    sw.WriteLine();
                }
                Console.WriteLine($"\n{name}\n{sw}\n\n");
            }
        }

        public static void PrintHashTable(string name, Hashtable hashtable)
        {
            {
                using (StringWriter sw = new StringWriter())
                {
                    using (JsonWriter w = new JsonTextWriter(sw))
                    {
                        JsonSerializer s = JsonSerializer.CreateDefault();
                        foreach (DictionaryEntry e in hashtable)
                        {
                            s.Serialize(w, e.Key);
                            sw.Write(" => ");
                            s.Serialize(w, e.Value);
                            sw.WriteLine();
                        }
                    }
                    Console.WriteLine($"\n{name}\n{sw}\n\n");
                }
            }
        }


        public static void PrintAsJson(string name, object obj)
        {
            {
                using (StringWriter sw = new StringWriter())
                {
                    using (JsonWriter w = new JsonTextWriter(sw))
                    {
                        JsonSerializer s = JsonSerializer.CreateDefault();
                        s.Serialize(w, obj);
                        sw.WriteLine();
                    }
                    Console.WriteLine($"\nname\n{sw}\n\n");
                }
            }
        }


        public static void PrintXmlNode(XmlNode xmlNode)
        {
            using (StringWriter sw = new StringWriter())
            {
                XmlWriterSettings xws = new XmlWriterSettings();
                xws.Indent = true;
                using (XmlWriter xw = XmlWriter.Create(sw, xws))
                {
                    xmlNode.WriteContentTo(xw);
                }
                Console.WriteLine(sw.ToString());
            }
        }
    }
}
