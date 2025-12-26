using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Basler_Camera.Models;

namespace Basler_Camera.Cli
{
    public static class ArgsParser
    {
        public static Request Parse(string[] args)
        {
            if (args == null || args.Length < 1)
                throw new Exception("Usage");

            Request req = new Request();
            req.Mode = TrimQ(args[0]).ToLowerInvariant();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            req.OutDir = baseDir;
            req.ModelPath = Path.Combine(baseDir, "model.onnx");
            req.LabelsPath = Path.Combine(baseDir, "labels.txt");

            if (req.Mode != "list")
                req.ImagePath = (args.Length >= 2) ? TrimQ(args[1]) : "";

            List<string> rest = new List<string>();
            if (req.Mode != "list" && args.Length > 2)
            {
                for (int i = 2; i < args.Length; i++) rest.Add(args[i]);
            }
            if (req.Mode == "list" && args.Length > 1)
            {
                for (int i = 1; i < args.Length; i++) rest.Add(args[i]);
            }

            // Parse flags with improved error handling
            for (int i = 0; i < rest.Count; i++)
            {
                string a = rest[i] ?? "";
                string low = a.ToLowerInvariant();

                if (StartsWith(low, "--debug="))
                {
                    req.DebugLevel = ClampDebug(ParseIntSafe(a.Substring("--debug=".Length), 1));
                }
                else if (low == "--debug")
                {
                    if (i + 1 < rest.Count)
                    {
                        req.DebugLevel = ClampDebug(ParseIntSafe(rest[i + 1], 1));
                        i++;
                    }
                    else req.DebugLevel = 1;
                }
                else if (StartsWith(low, "--outdir="))
                {
                    string v = TrimQ(a.Substring("--outdir=".Length));
                    if (string.IsNullOrWhiteSpace(v))
                    {
                        throw new Exception("Invalid output directory path.");
                    }
                    req.OutDir = v;
                }
                // Continue processing other flags similarly...
            }

            // ROI: pick first 4 consecutive ints among tokens not starting with "--"
            List<string> nonFlags = new List<string>();
            for (int i = 0; i < rest.Count; i++)
            {
                string t = rest[i] ?? "";
                if (t.StartsWith("--")) continue;
                nonFlags.Add(TrimQ(t));
            }

            // Validate ROI
            if (nonFlags.Count >= 4)
            {
                int x, y, w, h;
                if (int.TryParse(nonFlags[0], out x) &&
                    int.TryParse(nonFlags[1], out y) &&
                    int.TryParse(nonFlags[2], out w) &&
                    int.TryParse(nonFlags[3], out h))
                {
                    req.HasUserRoi = true;
                    req.Rx = x; req.Ry = y; req.Rw = w; req.Rh = h;
                }
                else
                {
                    throw new Exception("Invalid ROI parameters. Ensure they are four valid integers.");
                }
            }
            else
            {
                throw new Exception("Missing ROI parameters. Ensure there are four integer values for ROI.");
            }

            return req;
        }


        private static string TrimQ(string s)
        {
            if (s == null) return "";
            return s.Trim().Trim('"');
        }

        private static bool StartsWith(string s, string prefix)
        {
            return s != null && s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static int ClampDebug(int lv)
        {
            if (lv < 0) return 0;
            if (lv > 2) return 2;
            return lv;
        }


        private static int ParseIntSafe(string s, int def)
        {
            if (s == null) return def;
            s = TrimQ(s);
            int v;
            return int.TryParse(s, out v) ? v : def;
        }

        private static double ParseDoubleSafe(string s, double def)
        {
            if (s == null) return def;
            s = TrimQ(s);
            double v;
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : def;
        }
    }
}
