using ClearBible.Engine.Exceptions;
using System.Reflection;


namespace ClearBible.Engine.Persistence
{
    public static class FileGetBookIds
    {
        static FileGetBookIds()
        {
            using (Mutex mutex = new Mutex(true, "booksFileMutex"))
            {
                mutex.WaitOne();
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
                            try
                            {
                                _bookIds.Add(new BookId(pieces[0], pieces[1], pieces[2], pieces[3], Enum.Parse<LanguageCodeEnum>(pieces[4])));
                            }
                            catch (Exception)
                            {
                                throw new InvalidDataEngineException(name: "langaugeCode", value: pieces[4], message: $"Language code entry in {_fileName} for bookid {pieces[0]} is invalid");
                            }
                        }
                    }

                    mutex.ReleaseMutex();
                }
            }
        }

        public record BookId(
            string silCannonBookAbbrev, 
            string silCannonBookNum, 
            string clearTreeBookAbbrev,
            string clearTreeBookNum,
            LanguageCodeEnum languageCode);

        public enum LanguageCodeEnum
        {
            /// <summary>
            /// Greek
            /// </summary>
            G,
            /// <summary>
            /// Hebrew
            /// </summary>
            H
        }

        private static string _fileName = "books.csv";
        private static List<BookId> _bookIds = new();

        public static List<BookId> BookIds => _bookIds;
    }
}