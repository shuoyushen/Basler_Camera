using System;
using System.Collections.Generic;
using System.IO;

namespace Basler_Camera.Ai
{
    public static class LabelsLoader
    {
        public static List<string> Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new Exception("LabelsNotFound:" + path);

            string[] lines = File.ReadAllLines(path);
            List<string> labels = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                string s = (lines[i] ?? "").Trim();
                if (s.Length == 0) continue;
                labels.Add(s);
            }
            return labels;
        }
    }
}
