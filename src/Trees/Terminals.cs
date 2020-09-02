using System;
using System.Collections;
using System.Text;
using System.Xml;
using Utilities;
using Trees;

namespace Trees
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class Terminals
	{
		public static ArrayList GetTerminalNodes ( XmlNode treeNode )
		{
			ArrayList terminalCats = ListTerminalCats();
			ArrayList terminalNodes = new ArrayList();
			GetTerminalNodes( treeNode, ref terminalNodes, terminalCats );

			return terminalNodes;
		}

		public static void GetTerminalNodes ( XmlNode treeNode, ref ArrayList terminalNodes, ArrayList terminalCats )
		{
			if ( treeNode.NodeType.ToString().Equals("Text") ) // Terminal node
			{
				return;
			}

			if ( !treeNode.HasChildNodes ) return;

			int length = 0;
			if ( treeNode.Attributes.GetNamedItem("Start") != null ) 
			{
				int start = Int32.Parse(treeNode.Attributes.GetNamedItem("Start").Value);
				int end = Int32.Parse(treeNode.Attributes.GetNamedItem("End").Value);
				length = end-start+1;
			}
            string cat = Utils.GetAttribValue(treeNode, "Cat");
//			string cat = treeNode.Attributes.GetNamedItem("Cat").Value.ToString();
			string rule = string.Empty;
			if ( treeNode.Attributes.GetNamedItem("Rule") != null ) rule = treeNode.Attributes.GetNamedItem("Rule").Value;
//			if ( terminalCats.Contains(cat) )
			if ( treeNode.FirstChild.NodeType.ToString().Equals("Text") 
			   || cat == "CD"
			   || cat == "AD"
			   || cat == "NR"
			   || ( cat == "NN" && length <= 3 )
			   || ( cat == "VV" && length <= 3 )
			   || ( rule == "Vdir" && length <= 3 )
			   || ( rule == "Vres" && length <= 3 )
			   || ( rule == "CompoundVP" && length == 4 )
			   || ( rule == "CompoundVP2" && length == 4 )
			   || ( rule == "CompoundADVP" && length == 4 )
			   || ( rule == "AdvP-A" && treeNode.Attributes.GetNamedItem("adLemma") != null && treeNode.Attributes.GetNamedItem("adLemma").Value == "×î" )
			   || ( rule == "AdvP-V" && length == 2 )
			   ) // terminal ndoe
			{
				TerminalNode tn = new TerminalNode();

				if ( treeNode.Attributes.GetNamedItem("StrongNumber") != null )
				{
					string sn = treeNode.Attributes.GetNamedItem("StrongNumber").Value.ToString();
					tn.StrongNumber = Utils.PadStrongNumber( sn );
				}

                if (treeNode.Attributes.GetNamedItem("Sense") != null)
                {
                    tn.SenseNumber = Int32.Parse(treeNode.Attributes.GetNamedItem("Sense").Value);
                }

                tn.Lemma = Utils.GetAttribValue(treeNode, "Lemma");
                if (tn.Lemma == string.Empty) tn.Lemma = treeNode.InnerText;
//				tn.Lemma = treeNode.Attributes.GetNamedItem("Lemma").Value.ToString();
				tn.Category = cat;
				tn.Lex = treeNode.InnerText;

				if ( treeNode.Attributes.GetNamedItem("Chinese") != null )
				{
					tn.Chinese = treeNode.Attributes.GetNamedItem("Chinese").Value;
				}
				else
				{
					tn.Chinese = string.Empty;
				}

				if ( treeNode.Attributes.GetNamedItem("English") != null )
				{
					tn.English = treeNode.Attributes.GetNamedItem("English").Value;
				}
				else
				{
					tn.English = string.Empty;
				}

				if ( treeNode.Attributes.GetNamedItem("morphId") != null )
				{
					tn.MorphId = treeNode.Attributes.GetNamedItem("morphId").Value;
				}
				else
				{
					tn.MorphId = string.Empty;
				}

				terminalNodes.Add(tn);		
				return;
			}

			if ( treeNode.HasChildNodes )
			{
				XmlNodeList subNodes = treeNode.ChildNodes;

				for ( int i = 0; i < subNodes.Count; i++ )
				{
					GetTerminalNodes( subNodes[i], ref terminalNodes, terminalCats );
				}	
			}
		}

		public static ArrayList GetTerminalXmlNodes ( XmlNode treeNode )
		{
			ArrayList terminalNodes = new ArrayList();
            GetTerminalXmlNodes(treeNode, ref terminalNodes);

			return terminalNodes;
		}

		public static void GetTerminalXmlNodes ( XmlNode treeNode, ref ArrayList terminalNodes )
		{
			if ( treeNode.NodeType.ToString().Equals("Text") ) // Terminal node
			{
				return;
			}

            if (!treeNode.HasChildNodes)
            {
                return;
            }

			if ( treeNode.FirstChild.NodeType.ToString().Equals("Text") ) // terminal ndoe
			{
				terminalNodes.Add(treeNode);		
				return;
			}
 /*           if (treeNode.Attributes.GetNamedItem("Rule") != null && treeNode.Attributes.GetNamedItem("Rule").Value.EndsWith("X")) // Hebrew suffix as part of the word
            {
                terminalNodes.Add(treeNode);
                return;
            } */

			if ( treeNode.HasChildNodes )
			{
				XmlNodeList subNodes = treeNode.ChildNodes;

				for ( int i = 0; i < subNodes.Count; i++ )
				{
					GetTerminalXmlNodes( subNodes[i], ref terminalNodes );
				}	
			}
		}

        public static ArrayList GetMaxTerminalXmlNodes(XmlNode treeNode)
        {
            ArrayList terminalCats = ListTerminalCats();
            ArrayList terminalNodes = new ArrayList();
            GetMaxTerminalXmlNodes(treeNode, ref terminalNodes, terminalCats);

            return terminalNodes;
        }

        public static void GetMaxTerminalXmlNodes(XmlNode treeNode, ref ArrayList terminalNodes, ArrayList terminalCats)
        {
            if (treeNode.NodeType.ToString().Equals("Text")) // Terminal node
            {
                return;
            }

            if (!treeNode.HasChildNodes) return;

            if (treeNode.Attributes.GetNamedItem("Cat") != null && terminalCats.Contains(treeNode.Attributes.GetNamedItem("Cat").Value)) // terminal ndoe
            {
                terminalNodes.Add(treeNode);
                return;
            }

            if (treeNode.HasChildNodes)
            {
                XmlNodeList subNodes = treeNode.ChildNodes;

                for (int i = 0; i < subNodes.Count; i++)
                {
                    GetMaxTerminalXmlNodes(subNodes[i], ref terminalNodes, terminalCats);
                }
            }
        }

        public static XmlNode GetHeadTerminal(XmlNode treeNode)
        {
            if (treeNode.HasChildNodes && treeNode.FirstChild.NodeType.ToString() == "Text")
            {
                return treeNode;
            }
            else
            {
                int headPosition = Int32.Parse(Utils.GetAttribValue(treeNode, "Head"));
                XmlNode childNode = treeNode.ChildNodes[headPosition];
                return GetHeadTerminal(childNode);
            }
        }

        public static ArrayList GetHeadTerminals(XmlNode treeNode)
        {
            ArrayList headTerminals = new ArrayList();

            GetHeadTerminals2(treeNode, ref headTerminals);

            return headTerminals;
        }

        private static void GetHeadTerminals2(XmlNode treeNode, ref ArrayList headTerminals)
        {
            if (treeNode.HasChildNodes && treeNode.FirstChild.NodeType.ToString() == "Text")
            {
                headTerminals.Add(treeNode);
                return;
            }
            else if (Utils.GetAttribValue(treeNode, "Coord") == "True")
            {
                foreach (XmlNode child in treeNode.ChildNodes)
                {
                    string childCat = Utils.GetAttribValue(child, "Cat");
                    if (!(childCat == "cjp" || childCat == "conj" || childCat == "cj"))
                    {
                        GetHeadTerminals2(child, ref headTerminals);
                    }
                }
            }
            else
            {
                int headPosition = Int32.Parse(Utils.GetAttribValue(treeNode, "Head"));
                XmlNode childNode = treeNode.ChildNodes[headPosition];
                GetHeadTerminals2(childNode, ref headTerminals);
            }
        }

        public static string NodeStrongs(XmlNode treeNode)
        {
            ArrayList terminals = GetTerminalXmlNodes(treeNode);

            string nodeStrongs = " ";

            foreach (XmlNode terminal in terminals)
            {
                nodeStrongs += Utils.GetAttribValue(terminal, "StrongNumber") + " ";
            }

            return nodeStrongs;
        }

        public static string NodeUnicodeLemmas(XmlNode treeNode)
        {
            ArrayList terminals = GetTerminalXmlNodes(treeNode);

            string nodeUnicodeLemmas = string.Empty;

            foreach (XmlNode terminal in terminals)
            {
                string unicodeLemma = Utils.GetAttribValue(terminal, "UnicodeLemma");
                if (unicodeLemma.Contains("_")) unicodeLemma = unicodeLemma.Substring(0, unicodeLemma.IndexOf("_"));
                nodeUnicodeLemmas += unicodeLemma + " ";
            }

            return nodeUnicodeLemmas;
        }

        public static string NodeUnicodeText(XmlNode treeNode)
        {
            ArrayList terminals = GetTerminalXmlNodes(treeNode);

            string nodeUnicodes = string.Empty;

            foreach (XmlNode terminal in terminals)
            {
                string unicode = Utils.GetAttribValue(terminal, "Unicode");
                nodeUnicodes += unicode + " ";
            }

            return nodeUnicodes;
        }

		public static ArrayList ListTerminalCats()
		{
			ArrayList terminalCats = new ArrayList();

			terminalCats.Add("noun");
			terminalCats.Add("verb");
			terminalCats.Add("adj");
			terminalCats.Add("adv");
			terminalCats.Add("om");
			terminalCats.Add("cj");
			terminalCats.Add("art");
			terminalCats.Add("ij");
			terminalCats.Add("rel");
			terminalCats.Add("ptcl");
			terminalCats.Add("prep");
			terminalCats.Add("pron");
			terminalCats.Add("num");
			terminalCats.Add("x");
			terminalCats.Add("det");
			terminalCats.Add("conj");
			terminalCats.Add("intj");
			terminalCats.Add("VA");
			terminalCats.Add("VC");
			terminalCats.Add("VE");
			terminalCats.Add("VV");
			terminalCats.Add("NR");
			terminalCats.Add("NT");
			terminalCats.Add("NN");
			terminalCats.Add("LC");
			terminalCats.Add("PN");
			terminalCats.Add("M");
			terminalCats.Add("DT");
			terminalCats.Add("CD");
			terminalCats.Add("OD");
			terminalCats.Add("AD");
			terminalCats.Add("P");
			terminalCats.Add("CC");
			terminalCats.Add("CS");
			terminalCats.Add("DEC");
			terminalCats.Add("DEG");
			terminalCats.Add("DER");
			terminalCats.Add("DEV");
			terminalCats.Add("SP");
			terminalCats.Add("AS");
			terminalCats.Add("ETC");
			terminalCats.Add("MSP");
			terminalCats.Add("IJ");
			terminalCats.Add("ON");
			terminalCats.Add("PU");
			terminalCats.Add("JJ");
			terminalCats.Add("FW");
			terminalCats.Add("BEI");
			terminalCats.Add("BA");
			terminalCats.Add("ZI");

			return terminalCats;
		}

        public static ArrayList ListTerminalRules()
        {
            ArrayList terminalRules = new ArrayList();

            terminalRules.Add("NounSfx");
            terminalRules.Add("CD-Add");
            terminalRules.Add("CD-Multi");
            terminalRules.Add("CD-CD");
            terminalRules.Add("CD3CD");
            terminalRules.Add("CD4CD");
            terminalRules.Add("CD5CD");
            terminalRules.Add("OrdNum");
            terminalRules.Add("Fraction");
            terminalRules.Add("MM");
            terminalRules.Add("AA");
            terminalRules.Add("VV");
            terminalRules.Add("NN");
            terminalRules.Add("VlaiVqu");
            terminalRules.Add("VerbSfx");
            terminalRules.Add("VdeDir");
            terminalRules.Add("VdeResV");
            terminalRules.Add("VbuResV");
            terminalRules.Add("Vdir");
            terminalRules.Add("Vres");
            terminalRules.Add("Vres2");
            terminalRules.Add("Vres3");
            terminalRules.Add("VleDir");
            terminalRules.Add("AdjSfx");
            terminalRules.Add("VE-NP");
            terminalRules.Add("NN-NN");
            terminalRules.Add("ADJP-NP");
            terminalRules.Add("VP-LC");
            terminalRules.Add("OrdNum");
            terminalRules.Add("Adj-DEC");

            return terminalRules;
        }

        public static ArrayList ListTerminalterminalWordTypes()
        {
            ArrayList terminalWordTypes = new ArrayList();

            terminalWordTypes.Add("VR");
            terminalWordTypes.Add("VO");
            terminalWordTypes.Add("VD");
            terminalWordTypes.Add("AdVerb");
            terminalWordTypes.Add("NounNoun");
            terminalWordTypes.Add("V_dir");
            terminalWordTypes.Add("NounSfx");
            terminalWordTypes.Add("PrepNoun");
            terminalWordTypes.Add("Cmpd");
            terminalWordTypes.Add("other");
            terminalWordTypes.Add("Other");
            terminalWordTypes.Add("AdjNoun");
            terminalWordTypes.Add("VerbVerb");
            terminalWordTypes.Add("V_obj");
            terminalWordTypes.Add("V_prep");
            terminalWordTypes.Add("Suffix");
            terminalWordTypes.Add("NounLC");
            terminalWordTypes.Add("Cmpd");

            return terminalWordTypes;
        }

        public static string GetStrongs(XmlNode treeNode, string lang)
        {
            string strongs = string.Empty;

            ArrayList terminalNodes = Terminals.GetTerminalXmlNodes(treeNode);

            foreach (XmlNode terminalNode in terminalNodes)
            {
                string strong = Utils.GetAttribValue(terminalNode, "StrongNumberX");
                strongs += strong + " ";
            }

            strongs = lang + strongs;
            return strongs.Trim();
        }

        public static string GetLemmas(XmlNode treeNode, string lang)
        {
            string lemmas = string.Empty;

            ArrayList terminalNodes = Terminals.GetTerminalXmlNodes(treeNode);

            foreach (XmlNode terminalNode in terminalNodes)
            {
                string lemma = lang == "G" ? Utils.GetAttribValue(terminalNode, "Lemma") : Utils.GetAttribValue(terminalNode, "UnicodeLemma");
                lemmas += lemma + " ";
            }

            return lemmas.Trim();
        }
    }

	public class TerminalNode
	{
		public string Lex;
		public string Lemma;
		public string Category;
		public string StrongNumber;
        public int SenseNumber = 0;
		public string Chinese;
		public string English;
		public bool Linked = false;
		public string LinkType = string.Empty;
		public string MorphId;
		public TerminalNode Link;
		public ArrayList SemDomains = null;
	}
}
