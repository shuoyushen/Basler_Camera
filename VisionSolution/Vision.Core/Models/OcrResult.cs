namespace Basler_Camera.Models
{
    public class OcrResult
    {
        public string Text { get; set; }
        public double Conf { get; set; }
        public int Valid { get; set; }

        public OcrResult()
        {
            Text = "";
            Conf = 0.0;
            Valid = 0;
        }
    }
}
