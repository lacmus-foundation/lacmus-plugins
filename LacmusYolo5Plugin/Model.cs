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

namespace LacmusYolo5Plugin
{
    public class Model : IObjectDetectionModel
    {
        private const string PbFile = "LacmusYolo5Plugin.ModelWeights.frozen_inference_graph.pb";
        private readonly float _minScore;
        private Graph _graph;
        private Graph _preprocessingGraph;
        private readonly Session _session;
        private readonly Session _preprocessingSession;
        
        private readonly Tensor _inputModelTensor;
        private readonly Tensor[] _outputModelTensors;
        private readonly Tensor _inputPreprocessTensor;
        private readonly Tensor _outputPreprocessTensor;
        private readonly Tensor _imageSizeTensor;
        private readonly Tensor _imagePadTensor;

        public Model(float threshold)
        {
            tf.compat.v1.disable_eager_execution();
            _minScore = threshold;
            
            _graph = LoadModelGraph(PbFile);
            _preprocessingGraph = BuildPreprocessingGraph();

            _session = tf.Session(_graph);
            _preprocessingSession = tf.Session(_preprocessingGraph);
            
            _inputModelTensor = _graph.OperationByName("x");
            _outputModelTensors = new Tensor[]
            {
                _graph.OperationByName("Identity"),   //boxes (float32)
                _graph.OperationByName("Identity_1"), //scores (float32)
                _graph.OperationByName("Identity_2"), //classes (float32)
                _graph.OperationByName("Identity_3")  //valid_detections (int32)
            };

            _inputPreprocessTensor = _preprocessingGraph.get_operation_by_name("input");
            _outputPreprocessTensor = _preprocessingGraph.get_operation_by_name("output");
            _imageSizeTensor = _preprocessingGraph.get_operation_by_name("size");
            _imagePadTensor = _preprocessingGraph.get_operation_by_name("pad");
        }

        public IEnumerable<IObject> Infer(string imagePath, int width, int height)
        {
            var scale = ComputeImageScale(width, height);
            var size = np.array((int) (height * scale), (int) (width * scale));
            var padX = 1984 - (int) (height * scale);
            var padY = 1984 - (int) (width * scale);
            var pad = np.array(new [,]{{0, 0}, {0, padX}, {0, padY}, {0, 0}});
            
            _preprocessingGraph.as_default();
            var inputFeed = new FeedItem(_inputPreprocessTensor, imagePath);
            var sizeFeed = new FeedItem(_imageSizeTensor, size);
            var padFeed = new FeedItem(_imagePadTensor, pad);
            var imgArr = _preprocessingSession.run(_outputPreprocessTensor, inputFeed, sizeFeed, padFeed);
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
            var pad = tf.placeholder(TF_DataType.TF_INT32, new TensorShape(4, 2), name: "pad");
            
            var fileReader = tf.io.read_file(namePlaceholder, name:"file_reader");
            var decodeJpeg = tf.image.decode_jpeg(fileReader, channels: 3, name: "DecodeJpeg", dct_method: "INTEGER_ACCURATE");
            var casted = tf.cast(decodeJpeg, TF_DataType.TF_FLOAT, name: "cast");
            var castedNormalized = tf.div(casted, np.array(255.0f), name: "normalized");
            var dimsExpander = tf.expand_dims(castedNormalized, 0, name: "dims_expander");
            var resizeJpeg = tf.image.resize_bilinear(dimsExpander, size, half_pixel_centers: true, name: "resize");
            var imagePad = tf.pad(resizeJpeg, pad, name: "output");
            return imagePad.graph;
        }
        private float ComputeImageScale(int width, int height, int minSide = 1984, int maxSide = 1984)
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
            var boxes = resultArr[0].GetData<float>();
            var scores = resultArr[1].GetData<float>();
            var validDetections = np.squeeze(resultArr[3]).AsIterator<int>().MoveNext();
            var filteredObjects = new List<IObject>();
            
            for (var i = 0; i < validDetections; i++)
            {
                var score = scores[i];
                if (score < _minScore)
                    continue;
                
                var centerX = 1984 * (boxes[i * 4] + boxes[i * 4 + 2]) / 2;
                var centerY = 1984 * (boxes[i * 4 + 1] + boxes[i * 4 + 3]) / 2;
                var w = 1984 * (boxes[i * 4 + 2] - boxes[i * 4]);
                var h = 1984 * (boxes[i * 4 + 3] - boxes[i * 4 + 1]);
                
                var x1 = centerX - w / 2;
                var x2 = centerX + w / 2;
                var y1 = centerY - h / 2;
                var y2 = centerY + h / 2;

                var label = "Pedestrian";
                var obj = new DetectedObject
                {
                    Label = label,
                    Score = score,
                    XMin = (int)(Math.Min(x1, x2) / scale),
                    XMax = (int)(Math.Max(x1, x2) / scale),
                    YMin = (int)(Math.Min(y1, y2) / scale),
                    YMax = (int)(Math.Max(y1, y2) / scale)
                };
                filteredObjects.add(obj);
            }

            return filteredObjects;
        }
    }
}