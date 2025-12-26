namespace Basler_Camera.Ocr
{
    public static class OcrHitRules
    {
        public static string ClassifyHit(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string t = OcrTextFilter.NormalizeForRules(text);

            if (t.Contains("usb") && t.Contains("mass") && t.Contains("storage")) return "UsbMassStorage";
            if (t.Contains("reconnecting") && t.Contains("usb")) return "ReconnectingUsb";
            if (t.Contains("low") && t.Contains("battery")) return "LowBattery";
            if (t.Contains("error") || t.Contains("fault") || t.Contains("alarm")) return "Error";

            return "";
        }
    }
}
