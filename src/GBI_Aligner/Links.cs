using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;

using Newtonsoft.Json;

namespace GBI_Aligner
{
    class Links
    {
        public static ArrayList Getm2tLinks(Line[] m2tLines)  // manuscript to target from auto aligner
        {
            ArrayList m2tLinks = new ArrayList();

            for (int i = 0; i < m2tLines.Length; i++)
            {
                Line line = m2tLines[i];
                for (int j = 0; j < line.links.Count; j++)
                {
                    Link tmplink = line.links[j];
                    int[] sourceLinks = tmplink.source;
                    int[] targetLinks = tmplink.target;

                    for (int k = 0; k < sourceLinks.Length; k++)
                    {
                        int sourceLink = sourceLinks[k];
                        for (int l = 0; l < targetLinks.Length; l++)
                        {
                            int targetLink = targetLinks[l];
                            ManuscriptWord mWord = line.manuscript.words[sourceLink];
                            TranslationWord tWord = line.translation.words[targetLink];
                            long mid = mWord.id;
                            long tid = tWord.id;
                            string link = mid.ToString() + "-" + tid.ToString();
                            m2tLinks.Add(link);
                        }
                    }
                }
            }

            return m2tLinks;
        }

        // line3s originally from g2tJsonText, I think = checked gateway-target alignment
        // g2tLinks = ArrayList("gid-tid", ...)
        // auto_m_t_links = ArrayList("mid-tid", ...)
        public static Hashtable Getm2tLinks(Line3[] line3s, ArrayList g2tLinks, ArrayList auto_m_t_links)
        {
            Hashtable m2tLinks = new Hashtable();  // Hashtable("mid-tid" => 1, ...)

            ArrayList m2gLinks = Getm2gLinks(line3s);  // ArrayList("mid-gid", ...)
                                                       // comes from Line3.glinks :: int[][][]

            ArrayList linked_m_words = new ArrayList(); // ArrayList("mid", ...)

            foreach (string m2gLink in m2gLinks)
            {
                string mid = m2gLink.Substring(0, m2gLink.IndexOf("-"));
                string gid = m2gLink.Substring(m2gLink.IndexOf("-") + 1);
                ArrayList tids = GetTids(gid, g2tLinks);
                foreach (string tid in tids)
                {
                    string link = mid + "-" + tid;
                    if (!m2tLinks.ContainsKey(link))
                    {
                        m2tLinks.Add(link, 1);
                    }
                    if (!linked_m_words.Contains(mid))
                    {
                        linked_m_words.Add(mid);
                    }
                }
            }

            // link manuscript words that are not translated in the bridge language
            foreach(string auto_m_t_link in auto_m_t_links)
            {
                string mid = auto_m_t_link.Substring(0, auto_m_t_link.IndexOf("-"));
                if (!linked_m_words.Contains(mid))
                {
                    string tid = auto_m_t_link.Substring(auto_m_t_link.IndexOf("-") + 1);
                    string link = mid + "-" + tid;
                    m2tLinks.Add(link, 1);
                }
            }

            return m2tLinks;
        }

        static ArrayList Getm2gLinks(Line3[] line3s)
        {
            ArrayList m2gLinks = new ArrayList();

            for (int i = 0; i < line3s.Length; i++)
            {
                Line3 line = line3s[i];
                int[][][] links = line.glinks;
                for (int j = 0; j < links.Length; j++)
                {
                    int[][] link = links[j];
                    int[] sourceLinks = link[0];
                    int[] targetLinks = link[1];

                    for (int k = 0; k < sourceLinks.Length; k++)
                    {
                        int sourceLink = sourceLinks[k];
                        for (int l = 0; l < targetLinks.Length; l++)
                        {
                            int targetLink = targetLinks[l];
                            ManuscriptWord mWord = line.manuscript.words[sourceLink];
                            TranslationWord gWord = line.gtranslation.words[targetLink];
                            long mid = mWord.id;
                            long gid = gWord.id;
                            string key = mid.ToString() + "-" + gid.ToString();
                            m2gLinks.Add(key);
                        }
                    }
                }
            }

            return m2gLinks;
        }

        // m2gLines = manuscript to gateway from an input file
        // m2tLines = manuscript to target from auto aligner
        // m2tLinks = ArrayList("mid-tid", ...) from m2tLines
        // where the output is going to go
        public static void Linkg2tViaM2T(Line3[] m2gLines, Line[] m2tLines, ArrayList m2tLinks, string g2tJsonFile)
        {
            Alignment3 align = new Alignment3(); // Has a Line3[] inside of it.
            align.Lines = new Line3[m2gLines.Length];  // Output has same number of lines as the
                                                       // manuscript-to-gateway alignment.

            for (int i = 0; i < m2gLines.Length; i++)
            {
                Line3 line3 = m2gLines[i]; // line3 = manuscript to gateway line
                Line line = m2tLines[i]; // line = manuscript to target line
                line3.translation = line.translation; // line3.translation :: TranslationWord[]
                    // replacing the gateway translation words with the target translation words

                Hashtable g2tLinks = Getg2tLinks(line3.manuscript, line3.gtranslation, line.translation, line3.glinks, m2tLinks);
                    // g2tLinks = Hashtable("gid-tid" => 1, ...)
                ArrayList g2tGroups = Getg2tGroups(line3.gtranslation, line.translation, g2tLinks);
                    // g2tGroups = ArrayList(Group{ tSegments ArrayList("tid", ...), mSegments ArrayList("gid", ...) })

                Hashtable gPositionTable = BuildPositionTable(line3.gtranslation); // HashTable("gid" => position, ...)
                Hashtable tPositionTable = BuildPositionTable(line3.translation); // HashTable("tid" => position, ...)

                List<Link> links = new List<Link>();

                for (int j = 0; j < g2tGroups.Count; j++)
                {
                    Group2 g = (Group2)g2tGroups[j];

                    int[] s = new int[g.mSegments.Count];
                    for (int m = 0; m < g.mSegments.Count; m++)
                    {
                        string sourceWord = (string)g.mSegments[m];  // sourceWord = gid
                        int sPosition = (int)gPositionTable[sourceWord];
                        s[m] = sPosition;
                    }
                    int[] t = new int[g.tSegments.Count];
                    for (int n = 0; n < g.tSegments.Count; n++)
                    {
                        string targetWord = (string)g.tSegments[n]; // targetWord = tid
                        if (tPositionTable.ContainsKey(targetWord))
                        {
                            int tPosition = (int)tPositionTable[targetWord];
                            t[n] = tPosition;
                        }
                        else
                        {
                            continue; // This is not supposed to happen, but there can be some exceptions in the databases.
                        }
                    }

                    links.Add(new Link(){source=s, target=t, cscore=0.1});// dummy cscore
                }

                line3.links = links;
                align.Lines[i] = line3;
            }

            string json = JsonConvert.SerializeObject(align.Lines, Formatting.Indented);
            File.WriteAllText(g2tJsonFile, json);
        }

        // manuscript = data about the manuscript words in this line
        // gtranslation from the manuscript to gateway alignment :: TranslationWord[]
        // (A Line3 has both a translation and a gtranslation, both of type TranslationWord[])
        // translation = the struct with the TranslationWord[] from the manuscript to target alignment
        // m2glinks = the glinks :: int[][][] from the manuscript to gateway alignment
        // m2tLinks = ArrayList("mid-tid", ...) from manuscript to target alignment from auto aligner
        // 
        static Hashtable Getg2tLinks(Manuscript manuscript, Translation gtranslation, Translation translation, int[][][]m2gLinks, ArrayList m2tLinks)
        {
            Hashtable g2tLinks = new Hashtable();

            ArrayList g2mLinks = Getg2mLinks(manuscript, gtranslation, m2gLinks);
                // ArrayList("gid-mid", ...) where gid is translation word

            foreach (string g2mLink in g2mLinks)
            {
                string gid = g2mLink.Substring(0, g2mLink.IndexOf("-"));
                string mid = g2mLink.Substring(g2mLink.IndexOf("-") + 1);
                ArrayList tids = GetTids(mid, m2tLinks);  // ArrayList("tid", ...)
                foreach (string tid in tids)
                {
                    string link = gid + "-" + tid;
                    g2tLinks.Add(link, 1);
                }
            }

            return g2tLinks;  // Hashtable("gid-tid" => 1, ...)
        }

        public static ArrayList Getg2tLinks(Line3[] line3s)
        {
            ArrayList g2tLinks = new ArrayList();

            for (int i = 0; i < line3s.Length; i++)
            {
                Line3 line = line3s[i];
                List<Link> links = line.links;
                for (int j = 0; j < links.Count; j++)
                {
                    Link link = links[j];
                    int[] sourceLinks = link.source;
                    int[] targetLinks = link.target;

                    for (int k = 0; k < sourceLinks.Length; k++)
                    {
                        int sourceLink = sourceLinks[k];
                        for (int l = 0; l < targetLinks.Length; l++)
                        {
                            int targetLink = targetLinks[l];
                            TranslationWord gWord = line.gtranslation.words[sourceLink];
                            TranslationWord tWord = line.translation.words[targetLink];
                            long gid = gWord.id;
                            long tid = tWord.id;
                            string key = gid.ToString() + "-" + tid.ToString();
                            g2tLinks.Add(key);
                        }
                    }
                }
            }

            return g2tLinks;
        }

        // manuscript = data about the manuscript words in this line
        // gtranslation from the manuscript to gateway alignment :: TranslationWord[]
        // m2glinks = the glinks :: int[][][] from the manuscript to gateway alignment
        //
        // returns ArrayList("gid-mid", ...) where
        //   gid is a translation word and mid is a manuscript word
        //
        static ArrayList Getg2mLinks(Manuscript manuscript, Translation gTranslation, int[][][] m2gLinks)
        {
            ArrayList g2mLinks = new ArrayList();

            for (int i = 0; i < m2gLinks.Length; i++)
            {
                int[][] tmplink = m2gLinks[i];   // m2gLinks[i] is a link
                int[] sourceLinks = tmplink[0];  // m2gLinks[i][0] is array of source word indices
                int[] targetLinks = tmplink[1];  // m2gLinks[i][1] is array of target word indices

                for (int j = 0; j < sourceLinks.Length; j++)
                {
                    int sourceLink = sourceLinks[j];
                    for (int k = 0; k < targetLinks.Length; k++)
                    {
                        int targetLink = targetLinks[k];
                        ManuscriptWord mWord = manuscript.words[sourceLink];
                        TranslationWord tWord = gTranslation.words[targetLink];
                        long mid = mWord.id;
                        long gid = tWord.id;
                        string link = gid.ToString() + "-" + mid.ToString();
                        g2mLinks.Add(link);
                    }
                }
            }

            return g2mLinks;
        }

        // mid = manuscript word id
        // m2tLinks = ArrayList("mid-tid", ...) from manuscript to target alignment
        // returns ArrayList("tid", ...)
        // 
        static ArrayList GetTids(string mid, ArrayList m2tLinks)
        {
            ArrayList tids = new ArrayList();

            foreach(string m2tLink in m2tLinks)
            {
                string mid2 = m2tLink.Substring(0, m2tLink.IndexOf("-"));
                string tid = m2tLink.Substring(m2tLink.IndexOf("-") + 1);
                if (mid == mid2)
                {
                    tids.Add(tid);
                }
            }

            return tids;
        }

        // gTranslation = from the manuscript to gateway alignment, :: { words TranslationWord[] }
        // translation = from the manuscript to target alignment, :: { words TranslationWord[] }
        // g2tLinks = Hashtable("gid-tid" => 1, ...), gateway to target links as computed so far
        //
        // Computing some sort of equivalence relation ??
        //
        static ArrayList Getg2tGroups(Translation gTranslation, Translation translation, Hashtable map)
        {
            ArrayList links = new ArrayList();  // ::= ArrayList(Group2, ...)
            // Group2 {
            //   tWords ArrayList() -- not used here
            //   mWords ArrayList() -- not used here
            //   tSegments ArrayList("tid", ...)
            //   mSegments ArrayList("gid", ...)
            // }

            for (int i = 0; i < gTranslation.words.Length; i++) // for each gateway word
            {
                string sourceID = gTranslation.words[i].id.ToString();  // sourceID is a gateway word ID
                for (int j = 0; j < translation.words.Length; j++)  // for each target word
                {
                    string targetID = translation.words[j].id.ToString();  // targetID is a word in the target
                    string link = sourceID + "-" + targetID;
                    if (map.ContainsKey(link))
                    {
                        bool inserted = InsertIntoGroup(ref links, sourceID, targetID);
                        if (!inserted)
                        {
                            Group2 g = new Group2();
                            g.mSegments.Add(sourceID);
                            g.tSegments.Add(targetID);
                            links.Add(g);
                        }
                    }
                }
            }

            return links;
        }

        // line3s from gateway to target alignment
        // map = Hashtable("mid-tid" => int)
        //
        // returns ArrayList(Group2, ...) where the Group2's are
        // relating manuscript segments to target segments
        //
        public static ArrayList Getm2tGroups(Line3[] line3s, Hashtable map)
        {
            ArrayList links = new ArrayList(); // ArrayList(Group2{ mSegments -- mid, tSegements -- tid })

            for (int i = 0; i < line3s.Length; i++)
            {
                Line3 line = line3s[i];
                for (int j = 0; j < line.manuscript.words.Length; j++)
                {
                    string sourceID = line.manuscript.words[j].id.ToString(); // sourceID = mid
                    for (int k = 0; k < line.translation.words.Length; k++)
                    {
                        string targetID = line.translation.words[k].id.ToString(); // targetID = tid
                        string link = sourceID + "-" + targetID;
                        if (map.ContainsKey(link))
                        {
                            bool inserted = InsertIntoGroup(ref links, sourceID, targetID);
                            if (!inserted)
                            {
                                Group2 g = new Group2();
                                g.mSegments.Add(sourceID);
                                g.tSegments.Add(targetID);
                                links.Add(g);
                            }
                        }
                    }
                }
            }

            return links;
        }

        static bool InsertIntoGroup(ref ArrayList links, string s2tLink, string t2sLink)
        {
            bool inserted = false;

            for (int i = 0; i < links.Count; i++)
            {
                Group2 g = (Group2)links[i];
                if (g.mSegments.Contains(s2tLink) && !g.tSegments.Contains(t2sLink))
                {
                    g.tSegments.Add(t2sLink);
                    inserted = true;
                }
                if (g.tSegments.Contains(t2sLink) && !g.mSegments.Contains(s2tLink))
                {
                    g.mSegments.Add(s2tLink);
                    inserted = true;
                }
            }

            return inserted;
        }

        public static Hashtable BuildPositionTable(Manuscript m)
        {
            Hashtable positionTable = new Hashtable();

            for (int i = 0; i < m.words.Length; i++)
            {
                string id = m.words[i].id.ToString();
                positionTable.Add(id, i);
            }

            return positionTable;
        }

        public static Hashtable BuildPositionTable(Translation t)
        {
            Hashtable positionTable = new Hashtable();

            for (int i = 0; i < t.words.Length; i++)
            {
                string id = t.words[i].id.ToString();
                positionTable.Add(id, i);
            }

            return positionTable;
        }
    }

    public class Group2
    {
        public ArrayList tWords = new ArrayList();
        public ArrayList mWords = new ArrayList();
        public ArrayList tSegments = new ArrayList();
        public ArrayList mSegments = new ArrayList();
    }
}
