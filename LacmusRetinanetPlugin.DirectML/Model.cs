using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LacmusPlugin;
using NumSharp;
using Tensorflow;
using static Tensorflow.Binding;

namespace LacmusRetinanetPlugin.DirectML
{
    public class Model : IObjectDetectionModel
    {
        private const string _pbFile = "LacmusRetinanetPlugin.DirectML.ModelWeights.frozen_inference_graph.pb";
        private readonly float _minScore;
        private Graph _graph;
        private Graph _preprocessingGraph;
        private Session _session;
        private Session _preprocessingSession;
        
        private readonly Tensor _inputModelTensor;
        private readonly Tensor[] _outputModelTensors;
        private readonly Tensor _inputPreprocessTensor;
        private readonly Tensor _outputPreprocessTensor;
        private readonly Tensor _imageSizeTensor;

        public Model(float threshold)
        {
            tf.compat.v1.disable_eager_execution();
            _minScore = threshold;
            
            _graph = LoadModelGraph(_pbFile);
            _preprocessingGraph = BuildPreprocessingGraph();

            _session = tf.Session(_graph);
            _preprocessingSession = tf.Session(_preprocessingGraph);
            
            _inputModelTensor = _graph.OperationByName("input_1");
            _outputModelTensors = new Tensor[]
            {
                _graph.OperationByName("filtered_detections/map/TensorArrayStack/TensorArrayGatherV3"),
                _graph.OperationByName("filtered_detections/map/TensorArrayStack_1/TensorArrayGatherV3"),
                _graph.OperationByName("filtered_detections/map/TensorArrayStack_2/TensorArrayGatherV3")
            };

            _inputPreprocessTensor = _preprocessingGraph.get_operation_by_name("input");
            _outputPreprocessTensor = _preprocessingGraph.get_operation_by_name("output");
            _imageSizeTensor = _preprocessingGraph.get_operation_by_name("size");
        }

        public IEnumerable<IObject> Infer(string imagePath, int width, int height)
        {
            var startTime = DateTime.Now;
            var scale = ComputeImageScale(width, height);
            var size = np.array((int) (height * scale), (int) (width * scale));
            
            _preprocessingGraph.as_default();
            var inputFeed = new FeedItem(_inputPreprocessTensor, imagePath);
            var sizeFeed = new FeedItem(_imageSizeTensor, size);
            var imgArr = _preprocessingSession.run(_outputPreprocessTensor, inputFeed, sizeFeed);
            _graph.as_default();
            var results = _session.run(_outputModelTensors, new FeedItem(_inputModelTensor, imgArr));
            return FilterDetections(results, scale);
        }

        public async Task<IEnumerable<IObject>> InferAsync(string imagePath, int width, int height)
        {
            return await Task.Run(() => Infer(imagePath, width, height));
        }
        public void Dispose()
        {
            _session.graph.Dispose();
            //_session.close();
            _session.__del__();
            _session.Dispose();
            
            _preprocessingSession.graph.Dispose();
            //_preprocessingSession.close();
            _preprocessingSession.__del__();
            _preprocessingSession.Dispose();
            
            _graph.Dispose();
            _graph = null;
            _preprocessingGraph = null;
            tf.reset_default_graph();
        }

        private Graph LoadModelGraph(string pbFileName)
        {
            //try to load pb graph from embedded resources
            Graph graph = new Graph();
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(pbFileName))
            {
                using (var ms = new MemoryStream())
                {
                    stream?.CopyTo(ms);
                    var isOk = graph.Import(ms.ToArray());
                    if(!isOk || graph.ToArray().Length == 0)
                        throw new Exception("unable to import graph");
                }
            }
            return graph;
        }
        
        private Graph BuildPreprocessingGraph()
        {
            var namePlaceholder = tf.placeholder(TF_DataType.TF_STRING, name: "input");
            var size = tf.placeholder(TF_DataType.TF_INT32, new TensorShape(2), name: "size");
            var fileReader = tf.io.read_file(namePlaceholder, name:"file_reader");
            var decodeJpeg = tf.image.decode_jpeg(fileReader, channels: 3, name: "DecodeJpeg", dct_method: "INTEGER_ACCURATE");
            var casted = tf.cast(decodeJpeg, TF_DataType.TF_FLOAT, name: "cast");
            var castedBgr = tf.reverse(casted, axis: np.array(-1), name: "cast_bgr");
            var std = tf.constant(np.array(103.939f, 116.779f, 123.68f), name: "std");
            var castedBgrNormalized = tf.sub(castedBgr, std, name: "normalized");
            var dimsExpander = tf.expand_dims(castedBgrNormalized, 0, name: "dims_expander");
            var resizeJpeg = tf.image.resize_bilinear(dimsExpander, size, half_pixel_centers: true, name: "output");
            return resizeJpeg.graph;
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