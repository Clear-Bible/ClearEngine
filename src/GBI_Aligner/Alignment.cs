using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GBI_Aligner
{
    public class ManuscriptWord
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
    public class TranslationWord
    {
        public long id;
        public string altId;
        public string text;
    }
    public class Manuscript
    {
        public ManuscriptWord[] words;
    }
    public class Translation
    {
        public TranslationWord[] words;
    }
    public class Link {
        public int[] source;
        public int[] target;
        public double? cscore;

    }
    public class Line
    {
        public Manuscript manuscript;
        public Translation translation;

        //public int[][][] links;
        [JsonConverter(typeof(LinkJsonConverter))]
        public List<Link> links;
    }
    public class Alignment2
    {
        public Line[] Lines;
    }
    public class Line3
    {
        public Manuscript manuscript;
        public Translation gtranslation;
        public int[][][] glinks;   
             
        public Translation translation;
        //public int[][][] links;

        [JsonConverter(typeof(LinkJsonConverter))]
        public List<Link> links;        
    }
    class Alignment3
    {
        public Line3[] Lines;
    }

    public class LinkJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Link).IsAssignableFrom(objectType);
        }
    
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
             var links = new List<Link>();

            if (reader.TokenType == JsonToken.Null){
                return links;
            }
            else{
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

                    links.Add(new Link() { source = source, target = target, cscore = cscore });
                }
            }

            return links;
        }
    
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var links = value as List<Link>;
            var linksobj = new List<dynamic>();

            foreach(var link in links){
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
