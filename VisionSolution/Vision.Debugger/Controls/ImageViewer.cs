using System;
using System.Drawing;
using System.Windows.Forms;

using OpenCvSharp;
using OpenCvSharp.Extensions;

using DPoint = System.Drawing.Point;
using DRect = System.Drawing.Rectangle;

namespace Basler_Camera.VisionSolution.Vision.Debugger.Controls
{
    public class ImageViewer : Control
    {
        private Bitmap _bmp;
        private Rect _roi;

        private bool _dragging;
        private DPoint _p0, _p1;

        public event Action<Rect> RoiChanged;

        public Rect Roi
        {
            get { return _roi; }
            set { _roi = value; Invalidate(); }
        }

        public ImageViewer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            DoubleBuffered = true;
            BackColor = Color.Black;
        }

        public void SetMat(Mat bgr)
        {
            if (_bmp != null)
            {
                _bmp.Dispose();
                _bmp = null;
            }

            if (bgr == null || bgr.Empty())
            {
                _bmp = null;
            }
            else
            {
                _bmp = BitmapConverter.ToBitmap(bgr);
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(Color.Black);
            if (_bmp == null) return;

            DRect dst = GetFitRect(_bmp.Width, _bmp.Height, this.ClientRectangle);
            e.Graphics.DrawImage(_bmp, dst);

            // ROI overlay
            if (_roi.Width > 0 && _roi.Height > 0)
            {
                DRect rr = ImgRectToControlRect(_roi, dst, _bmp.Width, _bmp.Height);
                using (var pen = new Pen(Color.Lime, 2))
                    e.Graphics.DrawRectangle(pen, rr);
            }

            // dragging preview
            if (_dragging)
            {
                Rect temp = PointsToRoi(_p0, _p1, dst, _bmp.Width, _bmp.Height);
                if (temp.Width > 0 && temp.Height > 0)
                {
                    DRect tr = ImgRectToControlRect(temp, dst, _bmp.Width, _bmp.Height);
                    using (var pen = new Pen(Color.Yellow, 2))
                        e.Graphics.DrawRectangle(pen, tr);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_bmp == null) return;

            _dragging = true;
            _p0 = e.Location;
            _p1 = e.Location;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging) return;

            _p1 = e.Location;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_dragging) return;

            _dragging = false;
            if (_bmp == null) return;

            DRect dst = GetFitRect(_bmp.Width, _bmp.Height, this.ClientRectangle);
            Rect r = PointsToRoi(_p0, e.Location, dst, _bmp.Width, _bmp.Height);

            if (r.Width > 2 && r.Height > 2)
            {
                _roi = r;
                if (RoiChanged != null) RoiChanged(_roi);
            }

            Invalidate();
        }

        private static DRect GetFitRect(int imgW, int imgH, DRect client)
        {
            float sx = (float)client.Width / imgW;
            float sy = (float)client.Height / imgH;
            float s = Math.Min(sx, sy);

            int w = (int)(imgW * s);
            int h = (int)(imgH * s);
            int x = client.X + (client.Width - w) / 2;
            int y = client.Y + (client.Height - h) / 2;
            return new DRect(x, y, w, h);
        }

        private static DRect ImgRectToControlRect(Rect r, DRect dst, int imgW, int imgH)
        {
            float sx = (float)dst.Width / imgW;
            float sy = (float)dst.Height / imgH;

            int x = dst.X + (int)(r.X * sx);
            int y = dst.Y + (int)(r.Y * sy);
            int w = (int)(r.Width * sx);
            int h = (int)(r.Height * sy);
            return new DRect(x, y, w, h);
        }

        private static Rect PointsToRoi(DPoint a, DPoint b, DRect dst, int imgW, int imgH)
        {
            int x0 = Math.Min(a.X, b.X);
            int y0 = Math.Min(a.Y, b.Y);
            int x1 = Math.Max(a.X, b.X);
            int y1 = Math.Max(a.Y, b.Y);

            x0 = Clamp(x0, dst.Left, dst.Right);
            x1 = Clamp(x1, dst.Left, dst.Right);
            y0 = Clamp(y0, dst.Top, dst.Bottom);
            y1 = Clamp(y1, dst.Top, dst.Bottom);

            if (dst.Width <= 0 || dst.Height <= 0) return new Rect();

            float sx = (float)imgW / dst.Width;
            float sy = (float)imgH / dst.Height;

            int ix = (int)((x0 - dst.Left) * sx);
            int iy = (int)((y0 - dst.Top) * sy);
            int iw = (int)((x1 - x0) * sx);
            int ih = (int)((y1 - y0) * sy);

            if (iw < 0) iw = 0;
            if (ih < 0) ih = 0;
            return new Rect(ix, iy, iw, ih);
        }

        private static int Clamp(int v, int lo, int hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }
    }
}
