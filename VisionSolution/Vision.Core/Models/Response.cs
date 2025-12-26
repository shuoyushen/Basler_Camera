namespace Basler_Camera.Models
{
    public class Response
    {
        public string Status { get; set; }
        public string Error { get; set; }

        public string Mode { get; set; }
        public string Image { get; set; }
        public string Serial { get; set; }
        public string OutDir { get; set; }

        // AI ROI
        public string RoiXYWH { get; set; }

        // AI output
        public string State { get; set; }
        public double Prob { get; set; }
        public string Top3 { get; set; }

        // OCR output
        public string OcrText { get; set; }
        public double OcrConf { get; set; }
        public int OcrValid { get; set; }
        public string OcrRoiXYWH { get; set; }

        // OCR hit (instant)
        public string Hit { get; set; }

        // A2 stabilized OCR
        public string StateOcrRaw { get; set; }
        public string StateOcrStable { get; set; }
        public int StateOcrHits { get; set; }

        // final
        public string StateFinal { get; set; }
        public string StateSource { get; set; }

        public Response()
        {
            Status = "OK";
            Error = "";

            Mode = "";
            Image = "";
            Serial = "";
            OutDir = "";

            RoiXYWH = "0,0,0,0";

            State = "";
            Prob = 0.0;
            Top3 = "";

            OcrText = "";
            OcrConf = 0.0;
            OcrValid = 0;
            OcrRoiXYWH = "0,0,0,0";

            Hit = "";
            StateOcrRaw = "";
            StateOcrStable = "";
            StateOcrHits = 0;

            StateFinal = "";
            StateSource = "NONE";
        }
    }
}
