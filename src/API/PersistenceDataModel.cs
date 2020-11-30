using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClearBible.Clear3.API
{
    // Data Models for Persistence

    // At present only contains the legacy data model for persisting
    // an alignment.

    // FIXME: Adopt newer model being developed by Dirk and Charles.

    // FIXME: Do we need to design a persistent translation model?

    // FIXME: What else should be part of persistence?


    // Legacy data model for persisting an alignment.

    public class LegacyPersistentAlignment
    {
        public LpaLine[] Lines;
    }


    public class LpaLine
    {
        public LpaManuscript manuscript;
        public LpaTranslation translation;

        //public int[][][] links;
        [JsonConverter(typeof(LpaLinkJsonConverter))]
        public List<LpaLink> links;
    }


    public class LpaManuscript
    {
        public LpaManuscriptWord[] words;
    }


    public class LpaTranslation
    {
        public LpaTranslationWord[] words;
    }


    public class LpaManuscriptWord
    {
        public long id;
        public string altId;
        public string text;
        public string strong;
        public string gloss;
        public string gloss2;
        public string lemma;
        public string pos;
        public string morph;
    }


    public class LpaTranslationWord
    {
        public long id;
        public string altId;
        public string text;
    }

    

    public class LpaLink
    {
        public int[] source;
        public int[] target;
        public double? cscore;

    }

    
    public class LegacyPersistentAlignmentWithGateway
    {
        public LpaManuscript manuscript;
        public LpaTranslation gtranslation;
        public int[][][] glinks;

        public LpaTranslation translation;
        //public int[][][] links;

        [JsonConverter(typeof(LpaLinkJsonConverter))]
        public List<LpaLink> links;
    }


    public class LpaLinkJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(LpaLink).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var links = new List<LpaLink>();

            if (reader.TokenType == JsonToken.Null)
            {
                return links;
            }
            else
            {
                JArray array = JArray.Load(reader);
                var linksobj = array.ToObject<IList<dynamic>>();
                foreach (var linkobj in linksobj)
                {
                    int[] source = JsonConvert.DeserializeObject<int[]>(linkobj[0].ToString());
                    int[] target = JsonConvert.DeserializeObject<int[]>(linkobj[1].ToString());
                    double? cscore = null;
                    if (linkobj.Count >= 3)
                    {
                        dynamic attr = JsonConvert.DeserializeObject<dynamic>(linkobj[2].ToString());
                        cscore = attr.cscore;
                    }

                    links.Add(new LpaLink() { source = source, target = target, cscore = cscore });
                }
            }

            return links;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var links = value as List<LpaLink>;
            var linksobj = new List<dynamic>();

            foreach (var link in links)
            {
                var linkobj = new List<dynamic>();
                linkobj.Add(link.source);
                linkobj.Add(link.target);
                linkobj.Add(new Dictionary<string, object>(){
                    {"cscore", link.cscore}
                });

                linksobj.Add(linkobj);
            }

            JToken t = JToken.FromObject(linksobj);
            t.WriteTo(writer);
        }
    }
}
