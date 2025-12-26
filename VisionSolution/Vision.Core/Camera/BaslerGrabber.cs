using System;
using System.Collections.Generic;
using System.Threading;
using Basler.Pylon;
using Basler_Camera.Models;

namespace Basler_Camera.Cameras
{
    public class BaslerGrabber
    {
        private readonly object _grabLock = new object();
        private Mutex _globalGrabMutex = null;

        public Response List(Request req)
        {
            IList<ICameraInfo> devices = CameraFinder.Enumerate();
            int count = (devices == null) ? 0 : devices.Count;

            Response r = new Response();
            r.Status = "OK";
            r.Mode = "list";
            r.Serial = req.Serial ?? "";
            r.OutDir = req.OutDir ?? "";
            r.Hit = "COUNT=" + count.ToString();
            return r;
        }

        public void GrabToFile(string serial, string savePath)
        {
            EnterGlobalGrabMutex();
            try
            {
                lock (_grabLock)
                {
                    using (Basler.Pylon.Camera cam = CreateCameraBySerial(serial))
                    {
                        cam.Open();

                        try
                        {
                            try { cam.Parameters[PLCamera.TriggerMode].TrySetValue("Off"); } catch { }

                            try { if (cam.StreamGrabber.IsGrabbing) cam.StreamGrabber.Stop(); } catch { }
                            cam.StreamGrabber.Start(1);

                            using (IGrabResult result = cam.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException))
                            {
                                if (result == null || !result.GrabSucceeded)
                                    throw new Exception("GrabFailed");

                                ImagePersistence.Save(ImageFileFormat.Png, savePath, result);
                            }
                        }
                        finally
                        {
                            try { if (cam.StreamGrabber.IsGrabbing) cam.StreamGrabber.Stop(); } catch { }
                            try { cam.Close(); } catch { }
                        }
                    }
                }
            }
            finally
            {
                ExitGlobalGrabMutex();
            }
        }

        private static Basler.Pylon.Camera CreateCameraBySerial(string serial)
        {
            IList<ICameraInfo> devices = CameraFinder.Enumerate();
            if (devices == null || devices.Count == 0)
                throw new Exception("NoCameraFound");

            if (string.IsNullOrWhiteSpace(serial))
                return new Basler.Pylon.Camera(devices[0]);

            for (int i = 0; i < devices.Count; i++)
            {
                try
                {
                    ICameraInfo d = devices[i];
                    if (d != null && d.ContainsKey("SerialNumber") && d["SerialNumber"] == serial)
                        return new Basler.Pylon.Camera(d);
                }
                catch { }
            }

            throw new Exception("CameraSerialNotFound:" + serial);
        }

        private void EnterGlobalGrabMutex()
        {
            _globalGrabMutex = new Mutex(false, @"Global\Basler_Grab_Mutex");
            if (!_globalGrabMutex.WaitOne(8000))
                throw new Exception("GrabMutexTimeout");
        }

        private void ExitGlobalGrabMutex()
        {
            try
            {
                if (_globalGrabMutex != null)
                {
                    try { _globalGrabMutex.ReleaseMutex(); } catch { }
                    try { _globalGrabMutex.Dispose(); } catch { }
                }
            }
            finally
            {
                _globalGrabMutex = null;
            }
        }
    }
}
