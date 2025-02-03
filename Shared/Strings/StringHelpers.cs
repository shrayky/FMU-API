namespace Shared.Strings
{
    public class StringHelpers
    {
        public static string ArgumentValue(string[] keyValues, string key, string defaultValue = "")
        {
            bool nextValue = false;

            foreach (string arg in keyValues)
            {
                if (arg == key)
                {
                    nextValue = true;
                    continue;
                }

                if (nextValue)
                    return arg;
            }

            return defaultValue;
        }

        public static bool IsDigitString(string stringValue)
        {
            foreach (var c in stringValue)
            {
                if (c is < '0' or > '9')
                    return false;
            }

            return true;
        }
    }
}
