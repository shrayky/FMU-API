namespace FmuApiApplication.Utilites
{
    public static class StringHelper
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
    }
}
