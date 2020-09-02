using System;
using System.Xml;
using System.Collections;

namespace Utilities
{
	/// <summary>
	/// Summary description for Utils.
	/// </summary>
	public class Utils
	{
		//
		//   Doubles quotes
		//
		public static string QuoteQuotes(
			string    str
			)
		{
			int       cquot = 0;
			string    strRet = str;

			for (int ich = 0; ich < str.Length; ich ++)
			{
				if ('\'' == str[ich])
					cquot ++;
			}
			if (cquot > 0)
			{
				char[]    rgch;
                    
				rgch = new char[ str.Length + cquot ];
				for (int ich = 0, ichRet = 0; ich < str.Length; ich ++, ichRet ++)
				{
					rgch[ichRet] = str[ich];
					if ('\'' == str[ich])
						rgch[++ichRet] = '\'';
				}
				strRet = new String( rgch );
			}
			return strRet;
		}

		public static string PadStrongNumber ( string strongNumber )
		{
			if ( strongNumber == string.Empty ) strongNumber = "0000";
			if ( strongNumber.Length == 1 ) strongNumber = "000" + strongNumber;
			if ( strongNumber.Length == 2 ) strongNumber = "00" + strongNumber;
			if ( strongNumber.Length == 3 ) strongNumber = "0" + strongNumber;

			return strongNumber;
		}

        public static string Pad2(string number)
        {
            if (number.Length == 1)
            {
                number = "0" + number;
            }

            return number;
        }

		public static string Pad3( string number )
		{
			if ( number.Length == 1 )
			{
				number = "00" + number;
			}
			else if ( number.Length == 2 )
			{
				number = "0" + number;
			}

			return number;
		}

        public static string Pad4(string number)
        {
            if (number.Length == 1)
            {
                number = "000" + number;
            }
            else if (number.Length == 2)
            {
                number = "00" + number;
            }
            else if (number.Length == 3)
            {
                number = "0" + number;
            }

            return number;
        }

        public static string Pad5(string number)
        {
            if (number.Length == 1)
            {
                number = "0000" + number;
            }
            else if (number.Length == 2)
            {
                number = "000" + number;
            }
            else if (number.Length == 3)
            {
                number = "00" + number;
            }
            else if (number.Length == 4)
            {
                number = "0" + number;
            }

            return number;
        }

        public static string GetAttribValue(XmlNode treeNode, string attribName)
        {
            string attribValue = string.Empty;

            if (treeNode != null && treeNode.Attributes != null && treeNode.Attributes.GetNamedItem(attribName) != null)
            {
                attribValue = treeNode.Attributes.GetNamedItem(attribName).Value;
            }

            return attribValue;
        }

        public static string ArrayList2String(ArrayList listOfStrings)
        {
            string items = string.Empty;

            foreach (string s in listOfStrings)
            {
                items += ", " + s;
            }

            if (items.StartsWith(","))
            {
                items = items.Substring(1);
            }

            return items.Trim(); 
        }

        public static string ArrayList2String2(ArrayList listOfStrings)
        {
            string items = string.Empty;

            foreach (string s in listOfStrings)
            {
                items += " " + s;
            }

            return items.Trim(); 
        }

        public static ArrayList String2ArrayList(string list, string delimiters)
        {
            ArrayList aList = new ArrayList();

            string[] items = list.Split(delimiters.ToCharArray());

            for (int i = 0; i < items.Length; i++)
            {
                aList.Add(items[i]);
            }

            return aList;
        }

        public static int NumberOfWordsInString(string s, string delimiters)
        {
            string[] words = s.Split(delimiters.ToCharArray());
            return words.Length;
        }

        public static bool IsInitUpper(string s)
        {
            char[] characters = s.ToCharArray();
            if (Char.IsUpper(characters[0]))
            {
                return true;
            }

            return false;
        }

        public static bool IsAllUpper(string s)
        {
            char[] characters = s.ToCharArray();
            for (int i = 0; i < characters.Length; i++)
            {
                if (Char.IsLower(characters[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static ArrayList MergeLists(ArrayList list1, ArrayList list2)
        {
            ArrayList mergedList = new ArrayList();

            foreach(object member1 in list1)
            {
                mergedList.Add(member1);
            }
            foreach (object member2 in list2)
            {
                if (!mergedList.Contains(member2))
                {
                    mergedList.Add(member2);
                }
            }

            return mergedList;
        }
	}
}
