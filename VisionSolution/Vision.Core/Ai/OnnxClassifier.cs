using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using Basler_Camera.Models;

namespace Basler_Camera.Ai
{
    public class OnnxClassifier : IDisposable
    {
        private InferenceSession _session;
        private List<string> _labels;
        private string _modelPath;
        private string _labelsPath;

        public OnnxClassifier()
        {
            _session = null;
            _labels = null;
            _modelPath = "";
            _labelsPath = "";
        }

        public void Load(string modelPath, string labelsPath)
        {
            if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
                throw new Exception("ModelNotFound:" + modelPath);

            if (_session != null && _modelPath == modelPath && _labelsPath == labelsPath)
                return;

            Dispose();

            _session = new InferenceSession(modelPath);
            _labels = LabelsLoader.Load(labelsPath);
            _modelPath = modelPath;
            _labelsPath = labelsPath;
        }

        public AiResult PredictTopK(Mat bgr, int topK)
        {
            if (_session == null) throw new Exception("ModelNotLoaded");

            int inputW = 224, inputH = 224;

            using (Mat resized = new Mat())
            using (Mat rgb = new Mat())
            {
                Cv2.Resize(bgr, resized, new Size(inputW, inputH));
                Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

                DenseTensor<float> tensor = new DenseTensor<float>(new int[] { 1, 3, inputH, inputW });

                for (int y = 0; y < inputH; y++)
                {
                    for (int x = 0; x < inputW; x++)
                    {
                        Vec3b c = rgb.At<Vec3b>(y, x);
                        float r = c.Item0 / 255f;
                        float g = c.Item1 / 255f;
                        float b = c.Item2 / 255f;

                        tensor[0, 0, y, x] = r;
                        tensor[0, 1, y, x] = g;
                        tensor[0, 2, y, x] = b;
                    }
                }

                string inputName = _session.InputMetadata.Keys.First();

                List<NamedOnnxValue> inputs = new List<NamedOnnxValue>();
                inputs.Add(NamedOnnxValue.CreateFromTensor<float>(inputName, tensor));

                IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);

                float[] outArr = results.First().AsEnumerable<float>().ToArray();
                results.Dispose();

                double[] probs = Softmax(outArr);

                List<Tuple<int, double>> idx = new List<Tuple<int, double>>();
                for (int i = 0; i < probs.Length; i++)
                    idx.Add(Tuple.Create(i, probs[i]));

                idx.Sort((a, b2) => b2.Item2.CompareTo(a.Item2));

                AiResult ar = new AiResult();
                int k = Math.Max(1, topK);
                for (int i = 0; i < k && i < idx.Count; i++)
                {
                    int id = idx[i].Item1;
                    double p = idx[i].Item2;
                    string label = LabelOf(id);
                    ar.TopK.Add(new KeyValuePair<string, double>(label, p));
                }

                ar.Top1Label = ar.TopK[0].Key;
                ar.Top1Prob = ar.TopK[0].Value;
                return ar;
            }
        }

        private string LabelOf(int index)
        {
            if (_labels == null || _labels.Count == 0) return "Class" + index;
            if (index < 0 || index >= _labels.Count) return "Class" + index;
            return _labels[index];
        }

        private static double[] Softmax(float[] logits)
        {
            double max = logits.Max();
            double sum = 0.0;
            double[] exps = new double[logits.Length];

            for (int i = 0; i < logits.Length; i++)
            {
                double e = Math.Exp(logits[i] - max);
                exps[i] = e;
                sum += e;
            }

            if (sum <= 0) sum = 1.0;
            for (int i = 0; i < exps.Length; i++)
                exps[i] /= sum;

            return exps;
        }

        public void Dispose()
        {
            try { if (_session != null) _session.Dispose(); } catch { }
            _session = null;
            _labels = null;
            _modelPath = "";
            _labelsPath = "";
        }
    }
}
