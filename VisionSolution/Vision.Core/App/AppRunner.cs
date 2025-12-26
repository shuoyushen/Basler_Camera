using System;
using System.IO;
using OpenCvSharp;
using CvRect = OpenCvSharp.Rect;

using Basler_Camera.Models;
using Basler_Camera.Cameras;
using Basler_Camera.Vision;
using Basler_Camera.Ocr;
using Basler_Camera.Ai;

namespace Basler_Camera.App
{
    public class AppRunner
    {
        private readonly BaslerGrabber _grabber = new BaslerGrabber();
        private readonly RoiResolver _roi = new RoiResolver();
        private readonly OcrEngine _ocr = new OcrEngine();
        private readonly OnnxClassifier _ai = new OnnxClassifier();
        private readonly OcrStateStabilizer _stab = new OcrStateStabilizer();

        public Response Run(Request req)
        {
            EnsureOutDir(req);

            // 执行 list 功能时不要求图像路径
            if (req.Mode == "list")
            {
                // 如果没有图像路径，跳过相关操作
                if (string.IsNullOrWhiteSpace(req.ImagePath))
                {
                    return _grabber.List(req); // 执行 List 操作
                }
                else
                {
                    return Fail("ImagePathNotRequiredInListMode", req);
                }
            }

            // 如果图像路径为空，跳过其他模式的图像相关步骤
            if (string.IsNullOrWhiteSpace(req.ImagePath))
                return Fail("MissingImagePath", req);

            if (req.Mode == "grab")
                return RunGrab(req);

            if (req.Mode == "ocr")
                return RunOcr(req, false);

            if (req.Mode == "grab_ocr")
                return RunOcr(req, true);

            if (req.Mode == "ai")
                return RunAi(req, false, false);

            if (req.Mode == "grab_ai")
                return RunAi(req, true, false);

            if (req.Mode == "grab_ai_ocr")
                return RunAi(req, true, true);

            return Fail("UnknownMode", req);
        }


        private static void EnsureOutDir(Request req)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(req.OutDir))
                    Directory.CreateDirectory(req.OutDir);
            }
            catch
            {
                req.OutDir = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        private Response RunGrab(Request req)
        {
            _grabber.GrabToFile(req.Serial, req.ImagePath);

            Response r = new Response();
            r.Status = "OK";
            r.Mode = req.Mode;
            r.Image = req.ImagePath;
            r.Serial = req.Serial ?? "";
            r.OutDir = req.OutDir ?? "";
            DecideFinal(r);
            return r;
        }

        private Response RunOcr(Request req, bool grabFirst)
        {
            if (grabFirst)
                _grabber.GrabToFile(req.Serial, req.ImagePath);

            if (!File.Exists(req.ImagePath))
                return Fail("ImageNotFound", req);

            using (Mat src = Cv2.ImRead(req.ImagePath))
            {
                CvRect ocrRoi = _roi.ResolveOcrRoi(src, req);
                OcrResult o = _ocr.Recognize(src, ocrRoi, req);

                string instant = (o.Valid == 1) ? OcrHitRules.ClassifyHit(o.Text) : "";
                var st = _stab.Update(req.OutDir, req.Serial, instant, req.OcrConfirmHits);

                Response r = new Response();
                r.Status = "OK";
                r.Mode = req.Mode;
                r.Image = req.ImagePath;
                r.Serial = req.Serial ?? "";
                r.OutDir = req.OutDir ?? "";

                r.OcrText = (o.Valid == 1) ? o.Text : "";
                r.OcrConf = (o.Valid == 1) ? o.Conf : 0.0;
                r.OcrValid = (o.Valid == 1) ? 1 : 0;
                r.OcrRoiXYWH = string.Format("{0},{1},{2},{3}", ocrRoi.X, ocrRoi.Y, ocrRoi.Width, ocrRoi.Height);

                r.Hit = instant;
                r.StateOcrRaw = instant;
                r.StateOcrStable = st.Item1;
                r.StateOcrHits = st.Item3;

                DecideFinal(r);
                return r;
            }
        }

        private Response RunAi(Request req, bool grabFirst, bool forceOcr)
        {
            if (grabFirst)
                _grabber.GrabToFile(req.Serial, req.ImagePath);

            if (!File.Exists(req.ImagePath))
                return Fail("ImageNotFound", req);

            using (Mat src = Cv2.ImRead(req.ImagePath))
            {
                CvRect aiRoi = _roi.ResolveAiRoi(src, req);

                if (req.DebugLevel > 0)
                    DebugSaver.SaveRoiDebug(req.OutDir, src, aiRoi, "ai", req.DebugLevel);

                using (Mat roiMat = new Mat(src, aiRoi))
                {
                    _ai.Load(req.ModelPath, req.LabelsPath);
                    AiResult ar = _ai.PredictTopK(roiMat, 3);

                    Response r = new Response();
                    r.Status = "OK";
                    r.Mode = req.Mode;
                    r.Image = req.ImagePath;
                    r.Serial = req.Serial ?? "";
                    r.OutDir = req.OutDir ?? "";

                    r.RoiXYWH = string.Format("{0},{1},{2},{3}", aiRoi.X, aiRoi.Y, aiRoi.Width, aiRoi.Height);
                    r.State = ar.Top1Label;
                    r.Prob = ar.Top1Prob;
                    r.Top3 = ar.TopKString();

                    bool needOcr = forceOcr || (ar.Top1Prob < req.Threshold);

                    if (needOcr)
                    {
                        CvRect ocrRoi = _roi.ResolveOcrRoi(src, req);
                        OcrResult o = _ocr.Recognize(src, ocrRoi, req);

                        r.OcrText = (o.Valid == 1) ? o.Text : "";
                        r.OcrConf = (o.Valid == 1) ? o.Conf : 0.0;
                        r.OcrValid = (o.Valid == 1) ? 1 : 0;
                        r.OcrRoiXYWH = string.Format("{0},{1},{2},{3}", ocrRoi.X, ocrRoi.Y, ocrRoi.Width, ocrRoi.Height);

                        string instant = (o.Valid == 1) ? OcrHitRules.ClassifyHit(o.Text) : "";
                        var st = _stab.Update(req.OutDir, req.Serial, instant, req.OcrConfirmHits);

                        r.Hit = instant;
                        r.StateOcrRaw = instant;
                        r.StateOcrStable = st.Item1;
                        r.StateOcrHits = st.Item3;
                    }

                    DecideFinal(r);
                    return r;
                }
            }
        }

        private static void DecideFinal(Response r)
        {
            if (!string.IsNullOrWhiteSpace(r.StateOcrStable))
            {
                r.StateFinal = r.StateOcrStable;
                r.StateSource = "OCR_STABLE";
            }
            else if (!string.IsNullOrWhiteSpace(r.StateOcrRaw))
            {
                r.StateFinal = r.StateOcrRaw;
                r.StateSource = "OCR_RAW";
            }
            else if (!string.IsNullOrWhiteSpace(r.State))
            {
                r.StateFinal = r.State;
                r.StateSource = "AI";
            }
            else
            {
                r.StateFinal = "";
                r.StateSource = "NONE";
            }
        }

        private static Response Fail(string err, Request req)
        {
            Response r = new Response();
            r.Status = "FAIL";
            r.Error = err;
            r.Mode = (req != null) ? (req.Mode ?? "") : "";
            r.Image = (req != null) ? (req.ImagePath ?? "") : "";
            r.Serial = (req != null) ? (req.Serial ?? "") : "";
            r.OutDir = (req != null) ? (req.OutDir ?? "") : "";
            return r;
        }

    }
}
