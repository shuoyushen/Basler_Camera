using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Basler_Camera.Models
{
    public class AiResult
    {
        public string Top1Label { get; set; }
        public double Top1Prob { get; set; }
        public List<KeyValuePair<string, double>> TopK { get; set; }

        public AiResult()
        {
            Top1Label = "";
            Top1Prob = 0.0;
            TopK = new List<KeyValuePair<string, double>>();
        }

        public string TopKString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < TopK.Count; i++)
            {
                if (i > 0) sb.Append("|");
                sb.Append(TopK[i].Key);
                sb.Append(":");
                sb.Append(TopK[i].Value.ToString("0.000", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
    }
}
