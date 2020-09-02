using System;
using System.Collections;

namespace Utilities
{
	/// <summary>
	/// Summary description for sets.
	/// </summary>
	public class Sets
	{
		public static Hashtable EntryNotInHashtable( Hashtable table1, Hashtable table2 )
		{
			Hashtable oovList = new Hashtable();
			 
			IDictionaryEnumerator listEnum = table1.GetEnumerator();

			while ( listEnum.MoveNext() )
			{
				string entry = (string) listEnum.Key;
				int count = (int) listEnum.Value;

				if ( !table2.ContainsKey(entry) )
				{
					oovList.Add(entry, count);
				}
			}
 
			return oovList;
		}

		public static Hashtable EntryNotInArray( Hashtable table, ArrayList list )
		{
			Hashtable oovList = new Hashtable();
			 
			IDictionaryEnumerator listEnum = table.GetEnumerator();

			while ( listEnum.MoveNext() )
			{
				string entry = (string) listEnum.Key;
				int count = (int) listEnum.Value;

				if ( !list.Contains(entry) )
				{
					oovList.Add(entry, count);
				}
			}
 
			return oovList;
		}

		public static ArrayList MemberNotInHashtable( ArrayList list, Hashtable table )
		{
			ArrayList oovList = new ArrayList();
			 
			foreach( string member in list )
			{
				if ( !table.ContainsKey(member) && !oovList.Contains(member) )
				{
					oovList.Add(member);
				}
			}
 
			return oovList;
		}

		public static ArrayList MemberNotInArray( ArrayList list1, ArrayList list2 )
		{
			ArrayList oovList = new ArrayList();
			 
			foreach( string member in list1 )
			{
				if ( !list2.Contains(member) && !oovList.Contains(member) )
				{
					oovList.Add(member);
				}
			}
 
			return oovList;
		}

		public static ArrayList MemberInTable ( ArrayList list, Hashtable table )
		{
			ArrayList inList = new ArrayList();

			foreach( string member in list )
			{
				if ( table.ContainsKey(member) )
				{
					inList.Add(member);
				}
			}

			return inList;
		}

		public static bool CharIntersect ( string word1, string word2 )
		{
			bool intersect = false;

			char [] chars1 = word1.ToCharArray();
			char [] chars2 = word2.ToCharArray();

			for ( int i = 0; i < chars1.Length; i++ )
			{
				for ( int j = 0; j < chars2.Length; j++ )
				{
					if ( chars1[i].Equals(chars2[j]) )
					{
						intersect = true;
					}
				}
			}

			return intersect;
		}

        public static double CharIntersectRate(string word1, string word2)
        {
            int commomMembers = 0;
            Hashtable members = new Hashtable();

            word1 = word1.Replace(" ", "");
            word2 = word2.Replace(" ", "");

            char[] chars1 = word1.ToCharArray();
            char[] chars2 = word2.ToCharArray();

            for (int i = 0; i < chars1.Length; i++)
            {
                for (int j = 0; j < chars2.Length; j++)
                {
                    if ( !members.ContainsKey(chars1[i]) ) members.Add(chars1[i], 1);
                    if ( !members.ContainsKey(chars2[j]) ) members.Add(chars2[j], 1);
                    if (chars1[i].Equals(chars2[j]))
                    {
                        commomMembers++;
                    }
                }
            }

            return (double)commomMembers / (double)members.Count;
        }

        public static double WordIntersectRate(string words1, string words2)
        {
            int commomMembers = 0;
            Hashtable members = new Hashtable();

            string[] w1 = words1.Split(" ".ToCharArray());
            string[] w2 = words2.Split(" ".ToCharArray());

            for (int i = 0; i < w1.Length; i++)
            {
                for (int j = 0; j < w2.Length; j++)
                {
                    if (!members.ContainsKey(w1[i])) members.Add(w1[i], 1);
                    if (!members.ContainsKey(w2[j])) members.Add(w2[j], 1);
                    if (w1[i].Equals(w2[j]))
                    {
                        commomMembers++;
                    }
                }
            }

            return (double)commomMembers / (double)members.Count;
        }

		public static bool WordIntersect ( ArrayList words1, ArrayList words2 )
		{
			bool intersect = false;

			foreach ( string word1 in words1 )
			{
				foreach ( string word2 in words2 )
				{
					if ( word1 != string.Empty && word1 == word2 )
					{
						intersect = true;
					}
				}
			}

			return intersect;
		}

        public static bool ContainsEnglishWord(string phrase, string word)
        {
            bool containsEW = false;

            string[] words = phrase.Split(" ".ToCharArray());

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i] == word) containsEW = true;
            }

            return containsEW;
        }

        public static bool EnglishPhraseIntersect(string phrase1, string phrase2)
        {
            bool intersect = false;

            string[] words1 = phrase1.Split(" ".ToCharArray());
            string[] words2 = phrase2.Split(" ".ToCharArray());

            for (int i = 0; i < words1.Length; i++)
            {
                for (int j = 0; j < words2.Length; j++)
                {
                    if (words1[i] == words2[j])
                    {
                        intersect = true;
                        break;
                    }
                }
            }

            return intersect;
        }

		public static bool NumberIntersect ( ArrayList numbers1, ArrayList numbers2 )
		{
			bool intersect = false;

			foreach ( int number1 in numbers1 )
			{
				foreach ( int number2 in numbers2 )
				{
					if ( number1 == number2 )
					{
						intersect = true;
					}
				}
			}

			return intersect;
		}

        public static bool InSet(string w, string set)
        {
            string[] members = set.Substring(1, set.Length - 2).Split(" ".ToCharArray());

            for (int i = 0; i < members.Length; i++)
            {
                if (w == members[i]) return true;
            }

            return false;
        }

        public static bool SetEquivalent(ArrayList set1, ArrayList set2)
        {
            if (set1.Count != set2.Count) return false;

            foreach (string member1 in set1)
            {
                if (!set2.Contains(member1)) return false;
            }

            foreach (string member2 in set2)
            {
                if (!set1.Contains(member2)) return false;
            }

            return true;
        }
    }
}
