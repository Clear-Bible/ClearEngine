using ClearBible.Engine.Exceptions;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public static class Extensions
    {
        public static int AsInt(this string str, string name)
        {
            bool success = int.TryParse(str, out int strAsInt);
            if (success)
                return strAsInt;
            else
                throw new InvalidParameterEngineException(message: "string not parseable as int", name: name, value: str);
        }
    }
}
