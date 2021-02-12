using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NumSharp;
using LacmusPlugin;
using Tensorflow;
using static Tensorflow.Binding;

namespace LacmusRetinanetPlugin
{
    public class Model : IObjectDetectionModel
    {
        private const string _pbFile = "LacmusRetinanetPlugin.ModelWeights.frozen_inference_graph.pb";
        private const string _inputTensorName = "input_1";
        private const string _outputBboxTensorName = "Identity";
        private const string _outputScoreTensorName = "Identity_1";
        private const string _outputLabelsTensorName = "Identity_2";
        private float _minScore;
        private Graph _graph;
        private Session _session;

        public Model(float threshold)
        {
            tf.compat.v1.disable_eager_execution();
            _graph = new Graph();
            _minScore = threshold;
            
            //try to load pb graph from embedded resources
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(_pbFile))
            {
                using (var ms = new MemoryStream())
                {
                    stream?.CopyTo(ms);
                    var isOk = _graph.Import(ms.ToArray());
                    if(!isOk || _graph.ToArray().Length == 0)
                        throw new Exception("unable to import graph");
                    _session = tf.Session(_graph);
                }
            }
        }
        
        public IEnumerable<IObject> Infer(string imagePath, int width, int height)
        {
            var startTime = DateTime.Now;
            var imgArr = PreprocessImage(imagePath, width, height, out var scale);
            Console.WriteLine("Preprocess image at {0} s", DateTime.Now - startTime);
            _graph = _graph.as_default();
            Tensor inputTensor = _graph.OperationByName(_inputTensorName);
            Tensor tensorBoxes = _graph.OperationByName(_outputBboxTensorName);
            Tensor tensorScores = _graph.OperationByName(_outputScoreTensorName);
            Tensor tensorLabels = _graph.OperationByName(_outputLabelsTensorName);
            Tensor[] outTensorArr = new Tensor[] { tensorBoxes, tensorScores, tensorLabels };
            var results = _session.run(outTensorArr, new FeedItem(inputTensor, imgArr));
            return FilterDetections(results, scale);
        }
        public async Task<IEnumerable<IObject>> InferAsync(string imagePath, int width, int height)
        {
            return await Task.Run(() => Infer(imagePath, width, height));
        }
        public void Dispose()
        {
            _session.Dispose();
            _graph.Dispose();
            GC.Collect();
        }
        
        private NDArray PreprocessImage(string imagePath, int width, int height, out float scale)
        {
            scale = ComputeImageScale(width, height);
            var size = np.array((int)(height * scale), (int)(width * scale));
            var fileReader = tf.io.read_file(imagePath, "file_reader");
            var decodeJpeg = tf.image.decode_jpeg(fileReader, channels: 3, name: "DecodeJpeg", dct_method: "INTEGER_ACCURATE");
            var casted = tf.cast(decodeJpeg, TF_DataType.TF_FLOAT);
            var castedBgr = tf.reverse(casted, axis: np.array(-1));
            var std = tf.constant(np.array(103.939f, 116.779f, 123.68f));
            var castedBgrNormalized = tf.sub(castedBgr, std);
            var dimsExpander = tf.expand_dims(castedBgrNormalized, 0);
            var resizeJpeg = tf.image.resize_bilinear(dimsExpander, size, half_pixel_centers: true);
            return resizeJpeg.eval();
        }
        private float ComputeImageScale(int width, int height, int minSide = 2100, int maxSide = 2100)
        {
            var smallestSide = Math.Min(width, height);
            var scale = minSide / (float)smallestSide;
            var largestSide = Math.Max(width, height);
            if (largestSide * scale > maxSide)
            {
                scale = maxSide / (float)largestSide;
            }
            return scale;
        } 
        private IEnumerable<IObject> FilterDetections(NDArray[] resultArr, float scale)
        {
            var scores = resultArr[1].AsIterator<float>();
            var boxes = resultArr[0].GetData<float>();
            var id = np.squeeze(resultArr[2]).GetData<float>();
            var filteredObjects = new List<IObject>();
            for (int i = 0; i < scores.size; i++)
            {
                var score = scores.MoveNext();
                if (score < _minScore) 
                    continue;

                var isMerged = false;
                var xMin = boxes[i * 4] / scale;
                var yMin = boxes[i * 4 + 1] / scale;
                var xMax = boxes[i * 4 + 2] / scale;
                var yMax = boxes[i * 4 + 3] / scale;
                var label = "Pedestrian";
                var obj = new DetectedObject
                {
                    Label = label,
                    Score = score,
                    XMin = (int)xMin,
                    XMax = (int)xMax,
                    YMin = (int)yMin,
                    YMax = (int)yMax
                };
                
                foreach (var res in filteredObjects)
                {
                    if (res.Label != obj.Label)
                        continue;
                    if (res.XMin <= obj.XMin && res.XMax >= obj.XMin)
                    {
                        res.XMax = Math.Max(res.XMax, obj.XMax);
                        isMerged = true;
                    }
                    if (res.XMin <= obj.XMax && res.XMax >= obj.XMax)
                    {
                        res.XMin = Math.Min(res.XMin, obj.XMin);
                        isMerged = true;
                    }

                    if (res.YMin <= obj.YMin && res.YMax >= obj.YMin)
                    {
                        res.YMax = Math.Max(res.YMax, obj.YMax);
                        isMerged = true;
                    }
                    if (res.YMin <= obj.YMax && res.YMax >= obj.YMax)
                    {
                        res.YMin = Math.Min(res.YMin, obj.YMin);
                        isMerged = true;
                    }

                    if (obj.XMin <= res.XMin && obj.XMax >= res.XMax)
                    {
                        res.XMin = Math.Min(res.XMin, obj.XMin);
                        res.XMax = Math.Max(res.XMax, obj.XMax);
                        isMerged = true;
                    }
                    if (obj.YMin <= res.YMin && obj.YMax >= res.YMax)
                    {
                        res.YMin = Math.Min(res.YMin, obj.YMin);
                        res.YMax = Math.Max(res.YMax, obj.YMax);
                        isMerged = true;
                    }

                    if (isMerged)
                        res.Score = Math.Max(res.Score, obj.Score);
                }
                if (!isMerged)
                    filteredObjects.add(obj);
            }
            return filteredObjects;
        }
    }
}