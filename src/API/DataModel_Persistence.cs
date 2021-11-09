using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClearBible.Clear3.API
{
    // Data Model for Persistence
    //---------------------------

    // At present only contains the legacy data model for persisting
    // an alignment.

    // FIXME: Adopt newer model being developed by Dirk and Charles.

    // FIXME: Do we need to design a persistent translation model?

    // FIXME: What else should be part of persistence?


    
    /// <summary>
    /// Clear2 data model for persisting an alignment.
    /// </summary>
    /// 
    public class LegacyPersistentAlignment
    {
        public LpaLine[] Lines;
    }

    /// <summary>
    /// Clear2 data model for persisting an alignment that has alignments to lemmas (not surface words) of the translation.
    /// </summary>
    /// 
    /// NOTE: OneToMany
    /// 
    public class LegacyLemmaPersistentAlignment
    {
        public LpaLemmaLine[] Lines;
    }


    /// <summary>
    /// Alignment data for a single zone, consisting of a database of
    /// information about the source and translation words, and a collection
    /// of many-to-many links between source and translation words.
    /// </summary>
    /// 
    public class LpaLine
    {
        public LpaManuscript manuscript;
        public LpaTranslation translation;

        //public int[][][] links;
        [JsonConverter(typeof(LpaLinkJsonConverter))]
        public List<LpaLink> links;
    }

    /// <summary>
    /// Alignment data for a single zone, consisting of a database of
    /// information about the source and translation words, and a collection
    /// of many-to-many links between source and translation words.
    /// </summary>
    ///
    /// NOTE: OneToMany
    ///
    public class LpaLemmaLine
    {
        public LpaManuscript manuscript;
        public LpaLemmaTranslation translation;

        //public int[][][] links;
        [JsonConverter(typeof(LpaLinkJsonConverter))]
        public List<LpaLink> links;
    }

    /// <summary>
    /// Information about the manuscript words that occur in
    /// a zone.
    /// </summary>
    /// 
    public class LpaManuscript
    {
        public LpaManuscriptWord[] words;
    }


    /// <summary>
    /// Information about the translated words that occur in
    /// a zone.
    /// </summary>
    /// 
    public class LpaTranslation
    {
        public LpaTranslationWord[] words;
    }

    /// <summary>
    /// Information about the translated words that occur in
    /// a zone.
    /// </summary>
    ///
    /// NOTE: OneToMany
    ///
    public class LpaLemmaTranslation
    {
        public LpaLemmaTranslationWord[] words;
    }


    /// <summary>
    /// Information about a single manuscript word.
    /// </summary>
    /// 
    public class LpaManuscriptWord
    {
        /// <summary>
        /// Source ID as a canonical string (which contains only decimal
        /// digits) and then converted to a long integer.
        /// </summary>
        /// 
        public long id;

        /// <summary>
        /// Alternate ID of the form, for example, "λόγος-2" to mean the
        /// second occurence of the surface text "λόγος" within this zone
        /// </summary>
        /// 
        public string altId;

        /// <summary>
        /// Surface text.
        /// </summary>
        /// 
        public string text;

        /// <summary>
        /// Strong number, with prefix such as "G" or "H" to indicate
        /// language, as obtained from the treebank.
        /// </summary>
        /// 
        public string strong;

        public string gloss;
        public string gloss2;

        public string lemma;

        /// <summary>
        /// Part of speech, as obtained from the treebank.
        /// </summary>
        /// 
        public string pos;

        /// <summary>
        /// Morphology, a string that encodes the linguistic morphological
        /// analysis of this word, as obtained from the treebank.
        /// </summary>
        /// 
        public string morph;
    }


    /// <summary>
    /// Information about a single translation word.
    /// </summary>
    /// 
    public class LpaTranslationWord
    {
        /// <summary>
        /// TargetID as a canonical string (which contains only digits) and
        /// then converted to a long integer.
        /// </summary>
        /// 
        public long id;

        /// <summary>
        /// Alternate ID of the form, for example, "word-2" to mean the
        /// second occurence of the surface text "word" within this zone.
        /// </summary>
        /// 
        public string altId;

        /// <summary>
        /// Text, not lowercased.
        /// </summary>
        /// 
        public string text;
    }

    /// <summary>
    /// Information about a single translation word.
    /// </summary>
    ///
    /// NOTE: OneToMany
    ///
    public class LpaLemmaTranslationWord
    {
        /// <summary>
        /// TargetID as a canonical string (which contains only digits) and
        /// then converted to a long integer.
        /// </summary>
        /// 
        public long id;

        /// <summary>
        /// Alternate ID of the form, for example, "word-2" to mean the
        /// second occurence of the surface text "word" within this zone.
        /// </summary>
        /// 
        public string altId;

        /// <summary>
        /// Text, original surface text.
        /// </summary>
        /// 
        public string text;

        /// <summary>
        /// Lemma. Depending on the language, it could be or not lowercase.
        /// </summary>
        /// 
        public string lemma;
    }


    /// <summary>
    /// Link, expressed as an association between a set of source words
    /// and a set of target words.  The particular words are specified by
    /// their array indices within the LpaManuscriptWord[] and
    /// LpaTranslationWord[] that are given for this zone.  There might
    /// be an associated score for this link.
    /// </summary>
    /// 
    public class LpaLink
    {
        public int[] source;
        public int[] target;
        public double? cscore;

    }


    /// <summary>
    /// Clear2 data structure used for persisting an alignment when there
    /// is a gateway translation involved.
    /// </summary>
    /// FIXME: Maybe refactor.
    /// 
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


    /// <summary>
    /// Helper class for JSON import and export of the list of LpaLink
    /// objects.
    /// </summary>
    /// FIXME: Maybe consider alternatives.
    /// 
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
