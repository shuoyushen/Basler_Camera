using System;
using System.IO;

namespace Basler_Camera.Ocr
{
    public class OcrStateStabilizer
    {
        private const string Prefix = "state_cache_";

        private class Cache
        {
            public string Stable;
            public string Candidate;
            public int Hits;

            public Cache()
            {
                Stable = "";
                Candidate = "";
                Hits = 0;
            }
        }

        public Tuple<string, string, int> Update(string outDir, string serial, string instantState, int confirmHits)
        {
            confirmHits = Math.Max(1, confirmHits);

            string key = string.IsNullOrWhiteSpace(serial) ? "noserial" : serial.Trim();
            string path = Path.Combine(outDir, Prefix + key + ".txt");

            Cache c = Read(path);

            if (string.IsNullOrWhiteSpace(instantState))
            {
                c.Candidate = "";
                c.Hits = 0;
                Write(path, c);
                return Tuple.Create(c.Stable, c.Candidate, c.Hits);
            }

            if (string.Equals(c.Stable, instantState, StringComparison.OrdinalIgnoreCase))
            {
                c.Candidate = "";
                c.Hits = 0;
                Write(path, c);
                return Tuple.Create(c.Stable, c.Candidate, c.Hits);
            }

            if (string.Equals(c.Candidate, instantState, StringComparison.OrdinalIgnoreCase))
            {
                c.Hits++;
            }
            else
            {
                c.Candidate = instantState;
                c.Hits = 1;
            }

            if (c.Hits >= confirmHits)
            {
                c.Stable = c.Candidate;
                c.Candidate = "";
                c.Hits = 0;
            }

            Write(path, c);
            return Tuple.Create(c.Stable, c.Candidate, c.Hits);
        }

        private static Cache Read(string path)
        {
            try
            {
                if (!File.Exists(path)) return new Cache();

                string[] lines = File.ReadAllLines(path);
                Cache c = new Cache();

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    string k = line.Substring(0, idx).Trim();
                    string v = line.Substring(idx + 1).Trim();

                    if (k.Equals("stable", StringComparison.OrdinalIgnoreCase)) c.Stable = v;
                    else if (k.Equals("candidate", StringComparison.OrdinalIgnoreCase)) c.Candidate = v;
                    else if (k.Equals("hits", StringComparison.OrdinalIgnoreCase))
                    {
                        int h;
                        if (int.TryParse(v, out h)) c.Hits = h;
                    }
                }

                return c;
            }
            catch
            {
                return new Cache();
            }
        }

        private static void Write(string path, Cache c)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

                File.WriteAllLines(path, new[]
                {
                    "stable=" + (c.Stable ?? ""),
                    "candidate=" + (c.Candidate ?? ""),
                    "hits=" + c.Hits.ToString()
                });
            }
            catch { }
        }
    }
}
