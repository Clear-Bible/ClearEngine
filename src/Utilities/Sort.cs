using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;

namespace Utilities
{
    public class Sort
    {
        public static ArrayList TableToListByInt(Hashtable table, bool descending)
        {
            int[] counts;
            counts = new int[table.Count];
            Object[] objList;
            objList = new Object[table.Count];

            int i = 0;

            IDictionaryEnumerator tableEnum = table.GetEnumerator();

            while (tableEnum.MoveNext())
            {
                counts[i] = (int)tableEnum.Value;
                objList[i] = tableEnum.Key;
                i++;
            }

            Array.Sort(counts, objList, 0, objList.Length);

            ArrayList sortedList = new ArrayList();

            if (descending)
            {
                for (int j = objList.Length - 1; j >= 0; j--)
                {
                    sortedList.Add(objList[j]);
                }
            }
            else
            {
                for (int j = 0; j < objList.Length; j++)
                {
                    sortedList.Add(objList[j]);
                }
            }

            return sortedList;
        }

        public static ArrayList TableToListByInt2(Hashtable table, bool descending)
        {
            int[] counts;
            counts = new int[table.Count];
            Object[] objList;
            objList = new Object[table.Count];

            int i = 0;

            IDictionaryEnumerator tableEnum = table.GetEnumerator();

            while (tableEnum.MoveNext())
            {
                counts[i] = (int)tableEnum.Key;
                objList[i] = tableEnum.Value;
                i++;
            }

            Array.Sort(counts, objList, 0, objList.Length);

            ArrayList sortedList = new ArrayList();

            if (descending)
            {
                for (int j = objList.Length - 1; j >= 0; j--)
                {
                    sortedList.Add(objList[j]);
                }
            }
            else
            {
                for (int j = 0; j < objList.Length; j++)
                {
                    sortedList.Add(objList[j]);
                }
            }

            return sortedList;
        }

        public static ArrayList TableToListByDouble(Hashtable table, bool descending)
        {
            double[] scores;
            scores = new double[table.Count];
            Object[] objList;
            objList = new Object[table.Count];

            int i = 0;

            IDictionaryEnumerator tableEnum = table.GetEnumerator();

            while (tableEnum.MoveNext())
            {
                scores[i] = (double)tableEnum.Value;
                objList[i] = tableEnum.Key;
                i++;
            }

            Array.Sort(scores, objList, 0, objList.Length);

            ArrayList sortedList = new ArrayList();

            if (descending)
            {
                for (int j = objList.Length - 1; j >= 0; j--)
                {
                    sortedList.Add(objList[j]);
                }
            }
            else
            {
                for (int j = 0; j < objList.Length; j++)
                {
                    sortedList.Add(objList[j]);
                }
            }

            return sortedList;
        }

        public static ArrayList ReverseArrayList(ArrayList list)
        {
            ArrayList reverseList = new ArrayList();

            for (int i = list.Count - 1; i >= 0; i--)
            {
                object obj = (object)list[i];
                reverseList.Add(obj);
            }

            return reverseList;
        }

        public static ArrayList SortTableIntAsc(Hashtable table)
        {
            int[] scores;
            scores = new int[table.Count];
            ArrayList[] items;
            items = new ArrayList[table.Count];

            int i = 0;

            IDictionaryEnumerator pathEnum = table.GetEnumerator();

            while (pathEnum.MoveNext())
            {
                items[i] = (ArrayList)pathEnum.Key;
                scores[i] = (int)pathEnum.Value;
                i++;
            }

            ArrayList sortedTable = new ArrayList();

            Array.Sort(scores, items, 0, scores.Length);

            foreach(object item in items)
            {
                sortedTable.Add(item);
            }

            return sortedTable;
        }

        public static ArrayList SortTableIntDesc(Hashtable table)
        {
            int[] scores;
            scores = new int[table.Count];
            ArrayList[] items;
            items = new ArrayList[table.Count];

            int i = 0;

            IDictionaryEnumerator pathEnum = table.GetEnumerator();

            while (pathEnum.MoveNext())
            {
                items[i] = (ArrayList)pathEnum.Key;
                scores[i] = (int)pathEnum.Value;
                i++;
            }

            IComparer myComparer = new myReverserClass();

            Array.Sort(scores, items, myComparer);

            ArrayList sortedTable = new ArrayList();

            foreach(object item in items)
            {
                sortedTable.Add(item);
            }

            return sortedTable;
        }

        public static ArrayList SortTableIntDesc2(Hashtable table)
        {
            int[] scores;
            scores = new int[table.Count];
            object[] items;
            items = new ArrayList[table.Count];

            int i = 0;

            IDictionaryEnumerator pathEnum = table.GetEnumerator();

            while (pathEnum.MoveNext())
            {
                items[i] = (object)pathEnum.Key;
                scores[i] = (int)pathEnum.Value;
                i++;
            }

            IComparer myComparer = new myReverserClass();

            Array.Sort(scores, items, myComparer);

            ArrayList sortedTable = new ArrayList();

            foreach (object item in items)
            {
                sortedTable.Add(item);
            }

            return sortedTable;
        }

        public static ArrayList SortTableDoubleAsc(Hashtable table)
        {
            double[] scores;
            scores = new double[table.Count];
            ArrayList[] items;
            items = new ArrayList[table.Count];

            int i = 0;

            IDictionaryEnumerator pathEnum = table.GetEnumerator();

            while (pathEnum.MoveNext())
            {
                items[i] = (ArrayList)pathEnum.Key;
                scores[i] = (double)pathEnum.Value;
                i++;
            }

            ArrayList sortedTable = new ArrayList();

            Array.Sort(scores, items, 0, scores.Length);

            foreach (object item in items)
            {
                sortedTable.Add(item);
            }

            return sortedTable;
        }

        public static List<T> SortTableDoubleDesc<T>(
            Dictionary<T, double> table)
        {
            double[] scores;
            scores = new double[table.Count];
            T[] items;
            items = new T[table.Count];

            int i = 0;

            foreach (var keyValuePair in table)
            {
                items[i] = keyValuePair.Key;
                scores[i] = keyValuePair.Value;
                i++;
            }

            List<T> sortedTable = new List<T>();

            Array.Sort(scores, items, 0, scores.Length);

            for (int j = scores.Length - 1; j >= 0; j--)
            {
                sortedTable.Add(items[j]);
            }

            return sortedTable;
        }

        public static ArrayList SortTableDecimalDesc(Hashtable table)
        {
            Decimal[] scores;
            scores = new Decimal[table.Count];
            object[] items;
            items = new object[table.Count];

            int i = 0;

            IDictionaryEnumerator pathEnum = table.GetEnumerator();

            while (pathEnum.MoveNext())
            {
                items[i] = (object)pathEnum.Key;
                scores[i] = (Decimal)pathEnum.Value;
                i++;
            }

            IComparer myComparer = new myReverserClass();

            Array.Sort(scores, items, myComparer);

            ArrayList sortedTable = new ArrayList();

            foreach (object item in items)
            {
                sortedTable.Add(item);
            }

            return sortedTable;
        }
    }

    public class myReverserClass : IComparer
    {

        // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
        int IComparer.Compare(Object x, Object y)
        {
            return ((new CaseInsensitiveComparer()).Compare(y, x));
        }

    }

    public class myClass : IComparer
    {
        int IComparer.Compare(Object x, Object y)
        {
            return ((new CaseInsensitiveComparer()).Compare(y, x));
        }
    }
}
