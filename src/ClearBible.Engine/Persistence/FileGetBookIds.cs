using ClearBible.Engine.Exceptions;
using System.Reflection;


namespace ClearBible.Engine.Persistence
{
    public static class FileGetBookIds
    {
        public record BookId(string silCannonBookAbbrev, string silCannonBookNum, string clearTreeBookAbbrev, string clearTreeBookNum);

        private static string _fileName = "books.csv";
        private static bool _fileNameLoaded = false;
        private static List<BookId> _bookIds = new();

        private static Mutex booksFileMutex = new Mutex(false, "books");
        public static string Filename
        {
            get
            {
                return _fileName;
            }
            set
            {
                if (!_fileName.Equals(value))
                {
                    _fileName = value;
                    _fileNameLoaded = false;
                }
                
            }
        }
 
        public static List<BookId> BookIds { 
            get
            {
                if (!_fileNameLoaded)
                {
                    _bookIds.Clear();

                    bool gotSignal = false;
                    try
                    {
                        gotSignal = booksFileMutex.WaitOne(1000);
                    }
                    catch (AbandonedMutexException) //mutex abandoned by another thread and this thread now has ownership. Thread is signaled. see https://docs.microsoft.com/en-us/dotnet/api/system.threading.mutex?redirectedfrom=MSDN&view=net-6.0
                    {
                        gotSignal = true;
                    } 

                    if (gotSignal)
                    {
                        try
                        {
                            using (var reader = new StreamReader(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + _fileName))
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
                            _fileNameLoaded = true;

                        }
                        finally
                        {
                            booksFileMutex.ReleaseMutex();
                        }
                    }
                    else
                    {
                        throw new InvalidStateEngineException(name: "booksFileMutex", value: "timed out", message: "attempt to obtain mutex 'books' timed out");
                    }
                }
                return _bookIds;
            }
        }
    }
}




