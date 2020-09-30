using System;
using System.Collections;
using System.IO;
using System.Xml;

using Newtonsoft.Json;


namespace GBI_Aligner
{
    class TimUtil
    {
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
