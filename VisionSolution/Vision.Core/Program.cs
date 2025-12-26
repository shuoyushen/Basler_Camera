using Basler_Camera.App;
using Basler_Camera.Cli;
using Basler_Camera.Models;
using Basler_Camera.Output;
using Basler_Camera.VisionSolution;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Basler_Camera
{
    internal class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            // ===== Debug 窗口模式：Basler_Camera.exe debug =====
            if (args != null && args.Any(a => string.Equals(a, "debug", StringComparison.OrdinalIgnoreCase)))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new DebugMainForm());
                return 0; // 窗口正常退出认为成功
            }

            // ===== 原有 Console 逻辑：完全保留 =====
            Response resp = null;

            try
            {
                Request req = ArgsParser.Parse(args);
                resp = new AppRunner().Run(req);
            }
            catch (Exception ex)
            {
                resp = new Response
                {
                    Status = "FAIL",
                    Error = ex.GetType().Name + ":" + ex.Message,
                    Mode = (args != null && args.Length > 0) ? (args[0] ?? "") : ""
                };
            }

            KeyValuePrinter.Print(resp);
            return (resp != null && resp.Status == "OK") ? 0 : 1;
        }
    }
}
