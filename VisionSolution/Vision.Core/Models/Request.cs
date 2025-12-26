namespace Basler_Camera.Models
{
    public class Request
    {
        public string Mode { get; set; }
        public string ImagePath { get; set; }
        public string Serial { get; set; }

        // ROI
        public bool HasUserRoi { get; set; }
        public int Rx { get; set; }
        public int Ry { get; set; }
        public int Rw { get; set; }
        public int Rh { get; set; }

        // debug: 0 none, 1 key only, 2 full
        public int DebugLevel { get; set; }
        public string OutDir { get; set; }

        // AI
        public string ModelPath { get; set; }
        public string LabelsPath { get; set; }
        public double Threshold { get; set; }

        // OCR filter (A3)
        public double OcrMinConf { get; set; }
        public int OcrMinUsefulChars { get; set; }
        public double OcrOtherRatio { get; set; }
        public int OcrMaxRun { get; set; }

        // OCR debouncer (A2)
        public int OcrConfirmHits { get; set; }

        public Request()
        {
            Mode = "";
            ImagePath = "";
            Serial = "";

            HasUserRoi = false;
            Rx = 0; Ry = 0; Rw = 0; Rh = 0;

            DebugLevel = 1;
            OutDir = "";

            ModelPath = "model.onnx";
            LabelsPath = "labels.txt";
            Threshold = 0.80;

            OcrMinConf = 0.70;
            OcrMinUsefulChars = 4;
            OcrOtherRatio = 0.35;
            OcrMaxRun = 8;

            OcrConfirmHits = 2;
        }
    }
}
