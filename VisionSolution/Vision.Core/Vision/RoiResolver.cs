using OpenCvSharp;
using CvRect = OpenCvSharp.Rect;
using Basler_Camera.Models;

namespace Basler_Camera.Vision
{
    public class RoiResolver
    {
        public CvRect ResolveAiRoi(Mat src, Request req)
        {
            if (req.HasUserRoi)
                return Clamp(new CvRect(req.Rx, req.Ry, req.Rw, req.Rh), src.Size());

            return new CvRect(0, 0, src.Width, src.Height);
        }

        public CvRect ResolveOcrRoi(Mat src, Request req)
        {
            if (req.HasUserRoi)
                return Clamp(new CvRect(req.Rx, req.Ry, req.Rw, req.Rh), src.Size());

            CvRect r = FindTextLikeRoi(src);
            if (r.Width <= 0 || r.Height <= 0) return new CvRect(0, 0, 0, 0);
            return Expand(r, src.Size(), 10);
        }

        private CvRect FindTextLikeRoi(Mat src)
        {
            using (Mat gray = new Mat())
            using (Mat blackhat = new Mat())
            using (Mat gradX = new Mat())
            using (Mat bin = new Mat())
            using (Mat morph = new Mat())
            {
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(25, 5)))
                {
                    Cv2.MorphologyEx(gray, blackhat, MorphTypes.BlackHat, kernel);

                    Cv2.Sobel(blackhat, gradX, MatType.CV_32F, 1, 0, 3);
                    Cv2.ConvertScaleAbs(gradX, gradX);

                    Cv2.Threshold(gradX, bin, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                    Cv2.MorphologyEx(bin, morph, MorphTypes.Close, kernel);
                }

                Point[][] contours;
                HierarchyIndex[] hier;
                Cv2.FindContours(morph, out contours, out hier, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                CvRect best = new CvRect(0, 0, 0, 0);
                double bestArea = 0.0;

                if (contours != null)
                {
                    for (int i = 0; i < contours.Length; i++)
                    {
                        CvRect r = Cv2.BoundingRect(contours[i]);
                        double area = r.Width * r.Height;
                        if (r.Width > r.Height * 2 && area > bestArea)
                        {
                            best = r;
                            bestArea = area;
                        }
                    }
                }

                return best;
            }
        }

        private static CvRect Expand(CvRect r, Size img, int pad)
        {
            int x = System.Math.Max(0, r.X - pad);
            int y = System.Math.Max(0, r.Y - pad);
            int w = System.Math.Min(img.Width - x, r.Width + pad * 2);
            int h = System.Math.Min(img.Height - y, r.Height + pad * 2);
            return new CvRect(x, y, w, h);
        }

        private static CvRect Clamp(CvRect r, Size img)
        {
            int x = System.Math.Max(0, r.X);
            int y = System.Math.Max(0, r.Y);
            int w = r.Width;
            int h = r.Height;

            if (x >= img.Width || y >= img.Height) return new CvRect(0, 0, 0, 0);
            if (x + w > img.Width) w = img.Width - x;
            if (y + h > img.Height) h = img.Height - y;

            if (w <= 0 || h <= 0) return new CvRect(0, 0, 0, 0);
            return new CvRect(x, y, w, h);
        }
    }
}
