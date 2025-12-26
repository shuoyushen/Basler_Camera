using System;
using System.IO;
using OpenCvSharp;
using CvRect = OpenCvSharp.Rect;
using OpenCvSharp.Extensions;
using Tesseract;
using Basler_Camera.Models;
using Basler_Camera.Vision;

namespace Basler_Camera.Ocr
{
    public class OcrEngine : IDisposable
    {
        private readonly object _lock = new object();
        private TesseractEngine _engine;

        private const double FALLBACK_TH = 0.60;

        public OcrResult Recognize(Mat src, CvRect roi, Request req)
        {
            if (roi.Width <= 0 || roi.Height <= 0)
                return new OcrResult();

            EnsureEngine();

            // debug ROI：debug>=1 crop；debug=2 额外可视化
            DebugSaver.SaveRoiDebug(req.OutDir, src, roi, "ocr", req.DebugLevel);

            using (Mat roiMat = new Mat(src, roi))
            using (Mat gray = new Mat())
            {
                Cv2.CvtColor(roiMat, gray, ColorConversionCodes.BGR2GRAY);

                // debug=2 才保存灰度
                DebugSaver.SaveMat(req.OutDir, "dbg_ocr_gray.png", gray, req.DebugLevel, 2);

                Mat bestBin = null;
                string bestName = "";
                OcrResult best = OcrFromGrayBins(gray, PageSegMode.SingleBlock, req, "block", ref bestBin, ref bestName);

                if (best.Conf < FALLBACK_TH)
                {
                    Mat bestBin2 = null;
                    string bestName2 = "";
                    OcrResult lines = OcrSplitTwoLines(gray, req, ref bestBin2, ref bestName2);

                    if (lines.Conf > best.Conf)
                    {
                        best = lines;
                        DisposeMat(bestBin);
                        bestBin = bestBin2;
                        bestName = bestName2;
                    }
                    else
                    {
                        DisposeMat(bestBin2);
                    }
                }

                // debug>=1：只保存 best bin 一张
                if (req.DebugLevel >= 1 && bestBin != null)
                {
                    string file = (req.DebugLevel >= 2) ? ("dbg_ocr_bin_best_" + bestName + ".png") : "dbg_ocr_bin_best.png";
                    DebugSaver.SaveMat(req.OutDir, file, bestBin, req.DebugLevel, 1);
                }

                DisposeMat(bestBin);

                bool gib = OcrTextFilter.IsGibberish(
                    best.Text, best.Conf,
                    req.OcrMinConf, req.OcrMinUsefulChars,
                    req.OcrOtherRatio, req.OcrMaxRun);

                if (gib) return new OcrResult(); // Valid=0

                best.Valid = 1;
                return best;
            }
        }

        private OcrResult OcrSplitTwoLines(Mat gray, Request req, ref Mat bestBin, ref string bestName)
        {
            int h = gray.Height, w = gray.Width;
            int mid = h / 2;
            int overlap = Math.Max(4, h / 20);

            CvRect r1 = new CvRect(0, 0, w, Math.Min(h, mid + overlap));
            CvRect r2 = new CvRect(0, Math.Max(0, mid - overlap), w, Math.Max(1, h - (mid - overlap)));

            using (Mat g1 = new Mat(gray, r1))
            using (Mat g2 = new Mat(gray, r2))
            {
                Mat b1 = null; string n1 = "";
                Mat b2 = null; string n2 = "";

                OcrResult o1 = OcrFromGrayBins(g1, PageSegMode.SingleLine, req, "line1", ref b1, ref n1);
                OcrResult o2 = OcrFromGrayBins(g2, PageSegMode.SingleLine, req, "line2", ref b2, ref n2);

                // 证据 bin 取更高 conf 的那张
                if (o1.Conf >= o2.Conf)
                {
                    bestBin = b1;
                    bestName = "line1_" + n1;
                    DisposeMat(b2);
                }
                else
                {
                    bestBin = b2;
                    bestName = "line2_" + n2;
                    DisposeMat(b1);
                }

                string t = (o1.Text ?? "").Trim();
                string b = (o2.Text ?? "").Trim();
                string combined = string.IsNullOrEmpty(t) ? b : (string.IsNullOrEmpty(b) ? t : (t + "\n" + b));

                double conf = 0.0;
                int nonEmpty = 0;
                if (!string.IsNullOrWhiteSpace(o1.Text)) nonEmpty++;
                if (!string.IsNullOrWhiteSpace(o2.Text)) nonEmpty++;

                if (nonEmpty == 2) conf = (o1.Conf + o2.Conf) / 2.0;
                else if (nonEmpty == 1) conf = Math.Max(o1.Conf, o2.Conf) * 0.80;

                OcrResult outR = new OcrResult();
                outR.Text = NormalizeText(combined);
                outR.Conf = conf;
                outR.Valid = 1;
                return outR;
            }
        }

        private OcrResult OcrFromGrayBins(Mat gray, PageSegMode psm, Request req, string tag, ref Mat bestBin, ref string bestName)
        {
            string bestText = "";
            double bestConf = 0.0;

            foreach (OcrBinarizationItem item in OcrBinarization.Build(gray))
            {
                using (Mat bin = item.BinMat)
                {
                    OcrResult r = RunOcrWithConf(bin, psm);

                    if (r.Conf > bestConf)
                    {
                        bestConf = r.Conf;
                        bestText = r.Text;

                        DisposeMat(bestBin);
                        bestBin = bin.Clone();
                        bestName = tag + "_" + item.Name;
                    }

                    // debug=2 才保存全量 bin
                    if (req.DebugLevel >= 2)
                        DebugSaver.SaveMat(req.OutDir, "dbg_" + tag + "_" + item.Name + ".png", bin, req.DebugLevel, 2);
                }
            }

            OcrResult outR = new OcrResult();
            outR.Text = bestText;
            outR.Conf = bestConf;
            outR.Valid = 1;
            return outR;
        }

        private OcrResult RunOcrWithConf(Mat bin, PageSegMode psm)
        {
            EnsureEngine();

            using (var bmp = BitmapConverter.ToBitmap(bin))
            using (var pix = PixConverter.ToPix(bmp))
            using (var page = _engine.Process(pix, psm))
            {
                OcrResult r = new OcrResult();
                r.Text = NormalizeText(page.GetText() ?? "");
                double c = page.GetMeanConfidence();
                if (c > 1.0) c /= 100.0;
                if (c < 0) c = 0;
                if (c > 1) c = 1;
                r.Conf = c;
                r.Valid = 1;
                return r;
            }
        }

        private void EnsureEngine()
        {
            lock (_lock)
            {
                if (_engine != null) return;

                string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                string trained = Path.Combine(tessDataPath, "eng.traineddata");
                if (!File.Exists(trained))
                    throw new Exception("Missing eng.traineddata at: " + trained);

                _engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
                _engine.SetVariable("tessedit_char_whitelist",
                    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 -_:/().");
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                try { if (_engine != null) _engine.Dispose(); } catch { }
                _engine = null;
            }
        }

        private static void DisposeMat(Mat m)
        {
            try { if (m != null) m.Dispose(); } catch { }
        }

        private static string NormalizeText(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Replace("\r", " ").Trim();
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s.Trim();
        }

        private class OcrBinarizationItem
        {
            public string Name;
            public Mat BinMat;
        }

        private static class OcrBinarization
        {
            public static System.Collections.Generic.IEnumerable<OcrBinarizationItem> Build(Mat gray)
            {
                // otsu
                {
                    Mat b = new Mat();
                    Cv2.Threshold(gray, b, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                    Cv2.BitwiseNot(b, b);
                    yield return new OcrBinarizationItem { Name = "otsu", BinMat = Post(b) };
                }

                // fixed thresholds
                int[] ts = new int[] { 120, 180 };
                for (int i = 0; i < ts.Length; i++)
                {
                    int t = ts[i];
                    Mat b = new Mat();
                    Cv2.Threshold(gray, b, t, 255, ThresholdTypes.Binary);
                    Cv2.BitwiseNot(b, b);
                    yield return new OcrBinarizationItem { Name = "t" + t.ToString(), BinMat = Post(b) };
                }

                // adaptive
                {
                    Mat b = new Mat();
                    Cv2.AdaptiveThreshold(gray, b, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 31, 5);
                    Cv2.BitwiseNot(b, b);
                    yield return new OcrBinarizationItem { Name = "adapt", BinMat = Post(b) };
                }
            }

            private static Mat Post(Mat bin)
            {
                Mat outMat = bin.Clone();
                using (Mat k = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2)))
                {
                    Cv2.MorphologyEx(outMat, outMat, MorphTypes.Open, k);
                    Cv2.MorphologyEx(outMat, outMat, MorphTypes.Close, k);
                }

                Mat up = new Mat();
                Cv2.Resize(outMat, up, new Size(outMat.Width * 2, outMat.Height * 2), 0, 0, InterpolationFlags.Nearest);
                outMat.Dispose();
                return up;
            }
        }
    }
}
