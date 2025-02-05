using FmuApiApplication.Mark.Interfaces;

namespace FmuApiApplication.Mark.Services
{
    public class MarkParser : IMarkParser
    {
        private readonly char Gs = (char)29;
        private readonly string GsE = @"\u001d";

        public string ParseCode(string markCode)
        {
            var code = markCode.Trim();

            if (markCode.StartsWith(GsE) || markCode.StartsWith(Gs))
                code = markCode.Substring(1);

            return code;
        }

        public string CalculateSGtin(string markCode)
        {
            string sgtin = string.Empty;

            // вся маркировка (кроме штучного табака)
            if (markCode.StartsWith("01"))
            {
                markCode = markCode.Replace(GsE, Gs.ToString());
                sgtin = markCode;

                int gsSymbolPosition = markCode.IndexOf(Gs);

                if (gsSymbolPosition > 0)
                {
                    sgtin = $"{sgtin.Substring(2, 14)}{sgtin.Substring(18, gsSymbolPosition - 18)}";
                    return sgtin;
                }
            }

            // штучный табак
            if (markCode.Length == 29)
            {
                sgtin = markCode.Substring(0, 21);
                return sgtin;
            }

            // если нам в проверку прилетел сразу sgtin
            if (sgtin.Length == 0)
                sgtin = markCode;

            return sgtin;
        }

        public string CalculateBarcode(string sgtin)
        {
            if (sgtin.Length == 0)
                return string.Empty;

            var barcode = sgtin.Substring(1, 13);
            return barcode.TrimStart('0');
        }

        public string EncodeMark(string markingCode)
        {
            string codeData;
            try
            {
                codeData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(markingCode));
            }
            catch
            {
                codeData = markingCode;
            }
            return codeData;
        }
    }
}
