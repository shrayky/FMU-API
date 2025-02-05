namespace FmuApiDomain.TrueApi.MarkData.Check
{
    public class CheckMarksRequestData
    {
        public List<string> Codes { get; set; } = [];

        public CheckMarksRequestData() { }

        public CheckMarksRequestData(string mark)
        {
            Codes.Add(mark.Replace("\\u001d", "\u001d"));
        }

        public CheckMarksRequestData(List<string> marks)
        {
            foreach (string mark in marks)
            {
                Codes.Add(mark.Replace("\\u001d", "\u001d"));
            }
        }

        public void LoadMarks(List<string> marks)
        {
            Codes.Clear();

            foreach (var mark in marks)
            {
                Codes.Add(mark);
            }
        }
    }
}
