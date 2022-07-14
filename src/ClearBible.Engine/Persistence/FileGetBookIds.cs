using ClearBible.Engine.Exceptions;
using System.Reflection;


namespace ClearBible.Engine.Persistence
{
    public static class FileGetBookIds
    {
        static FileGetBookIds()
        {
            using (var reader = new StreamReader(
                       Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                       Path.DirectorySeparatorChar + _fileName))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    int commentLocation = line?.IndexOf('#') ?? -1;
                    if (commentLocation != -1)
                    {
                        line = line?.Substring(0, commentLocation) ?? "";
                    }

                    var pieces = line?.Split(',') ?? new string[0];
                    if (pieces.Length >= 4)
                    {
                        _bookIds.Add(new BookId(pieces[0], pieces[1], pieces[2], pieces[3]));
                    }
                }
            }
        }

        public record BookId(string silCannonBookAbbrev, string silCannonBookNum, string clearTreeBookAbbrev,
            string clearTreeBookNum);

        private static string _fileName = "books.csv";
        private static List<BookId> _bookIds = new();

        public static List<BookId> BookIds => _bookIds;
    }
}