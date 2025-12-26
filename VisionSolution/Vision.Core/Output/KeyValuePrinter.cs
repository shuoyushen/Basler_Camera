using System.Globalization;
using Basler_Camera.Models;

namespace Basler_Camera.Output
{
    public static class KeyValuePrinter
    {
        public static void Print(Response r)
        {
            if (r == null)
            {
                System.Console.WriteLine("STATUS=FAIL");
                System.Console.WriteLine("ERROR=NullResponse");
                PrintDefaults();
                return;
            }

            System.Console.WriteLine("STATUS=" + (string.IsNullOrWhiteSpace(r.Status) ? "OK" : r.Status));
            System.Console.WriteLine("ERROR=" + (r.Error ?? ""));
            System.Console.WriteLine("MODE=" + (r.Mode ?? ""));
            System.Console.WriteLine("IMAGE=" + (r.Image ?? ""));
            System.Console.WriteLine("SERIAL=" + (r.Serial ?? ""));

            System.Console.WriteLine("ROI_XYWH=" + (string.IsNullOrWhiteSpace(r.RoiXYWH) ? "0,0,0,0" : r.RoiXYWH));
            System.Console.WriteLine("STATE=" + (r.State ?? ""));
            System.Console.WriteLine("PROB=" + r.Prob.ToString("0.000", CultureInfo.InvariantCulture));
            System.Console.WriteLine("TOP3=" + (r.Top3 ?? ""));

            System.Console.WriteLine("OCR_TEXT=" + (r.OcrValid == 1 ? (r.OcrText ?? "") : ""));
            System.Console.WriteLine("OCR_CONF=" + (r.OcrValid == 1 ? r.OcrConf : 0.0).ToString("0.000", CultureInfo.InvariantCulture));
            System.Console.WriteLine("OCR_VALID=" + (r.OcrValid == 1 ? "1" : "0"));
            System.Console.WriteLine("OCR_ROI_XYWH=" + (string.IsNullOrWhiteSpace(r.OcrRoiXYWH) ? "0,0,0,0" : r.OcrRoiXYWH));

            System.Console.WriteLine("HIT=" + (r.Hit ?? ""));
            System.Console.WriteLine("STATE_OCR_RAW=" + (r.StateOcrRaw ?? ""));
            System.Console.WriteLine("STATE_OCR_STABLE=" + (r.StateOcrStable ?? ""));
            System.Console.WriteLine("STATE_OCR_HITS=" + r.StateOcrHits.ToString(CultureInfo.InvariantCulture));

            System.Console.WriteLine("STATE_FINAL=" + (r.StateFinal ?? ""));
            System.Console.WriteLine("STATE_SRC=" + (r.StateSource ?? "NONE"));

            System.Console.WriteLine("OUTDIR=" + (r.OutDir ?? ""));
        }

        private static void PrintDefaults()
        {
            System.Console.WriteLine("MODE=");
            System.Console.WriteLine("IMAGE=");
            System.Console.WriteLine("SERIAL=");
            System.Console.WriteLine("ROI_XYWH=0,0,0,0");
            System.Console.WriteLine("STATE=");
            System.Console.WriteLine("PROB=0.000");
            System.Console.WriteLine("TOP3=");
            System.Console.WriteLine("OCR_TEXT=");
            System.Console.WriteLine("OCR_CONF=0.000");
            System.Console.WriteLine("OCR_VALID=0");
            System.Console.WriteLine("OCR_ROI_XYWH=0,0,0,0");
            System.Console.WriteLine("HIT=");
            System.Console.WriteLine("STATE_OCR_RAW=");
            System.Console.WriteLine("STATE_OCR_STABLE=");
            System.Console.WriteLine("STATE_OCR_HITS=0");
            System.Console.WriteLine("STATE_FINAL=");
            System.Console.WriteLine("STATE_SRC=NONE");
            System.Console.WriteLine("OUTDIR=");
        }
    }
}
