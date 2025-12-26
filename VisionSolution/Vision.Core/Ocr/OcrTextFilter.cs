using System;

namespace Basler_Camera.Ocr
{
    public static class OcrTextFilter
    {
        public static bool IsGibberish(string text, double conf,
            double minConf, int minUsefulChars, double otherRatioMax, int maxRun)
        {
            if (conf < minConf) return true;
            if (string.IsNullOrWhiteSpace(text)) return true;

            int letters = 0, digits = 0, other = 0, total = 0;

            foreach (char c in text)
            {
                if (c == '\r') continue;
                total++;

                if (char.IsLetter(c)) letters++;
                else if (char.IsDigit(c)) digits++;
                else if (char.IsWhiteSpace(c)) { }
                else other++;
            }

            if (total == 0) return true;

            int useful = letters + digits;
            if (useful < Math.Max(1, minUsefulChars)) return true;

            double otherRatio = (double)other / (double)total;
            if (otherRatio > otherRatioMax) return true;

            maxRun = Math.Max(2, maxRun);
            int longest = 1, run = 1;
            for (int i = 1; i < text.Length; i++)
            {
                if (text[i] == text[i - 1])
                {
                    run++;
                    if (run > longest) longest = run;
                }
                else run = 1;
            }
            if (longest >= maxRun) return true;

            return false;
        }

        public static string NormalizeForRules(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.ToLowerInvariant();

            char[] chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i])) chars[i] = ' ';
            }

            string[] parts = new string(chars).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }
    }
}
