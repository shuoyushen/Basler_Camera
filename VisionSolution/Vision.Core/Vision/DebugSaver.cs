using System.IO;
using OpenCvSharp;
using CvRect = OpenCvSharp.Rect;

namespace Basler_Camera.Vision
{
    public static class DebugSaver
    {
        public static void EnsureDir(string outDir)
        {
            try { Directory.CreateDirectory(outDir); } catch { }
        }

        public static void SaveRoiDebug(string outDir, Mat src, CvRect roi, string tag, int debugLevel)
        {
            if (debugLevel <= 0) return;
            EnsureDir(outDir);

            // debug=2 才存可视化
            if (debugLevel >= 2)
            {
                using (Mat vis = src.Clone())
                {
                    Cv2.Rectangle(vis, roi, new Scalar(0, 255, 0), 2);
                    Cv2.ImWrite(Path.Combine(outDir, "dbg_" + tag + "_roi_vis.png"), vis);
                }
            }

            // debug>=1 存 roi crop
            using (Mat roiMat = new Mat(src, roi))
            {
                Cv2.ImWrite(Path.Combine(outDir, "dbg_" + tag + "_roi.png"), roiMat);
            }
        }

        public static void SaveMat(string outDir, string filename, Mat mat, int debugLevel, int minLevel)
        {
            if (debugLevel < minLevel) return;
            EnsureDir(outDir);
            Cv2.ImWrite(Path.Combine(outDir, filename), mat);
        }
    }
}
