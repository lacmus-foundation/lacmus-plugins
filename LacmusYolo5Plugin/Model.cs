using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LacmusPlugin;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LacmusYolo5Plugin
{
    public class Model : IObjectDetectionModel
    {
        private const string OnnxFile = "LacmusYolo5Plugin.ModelWeights.frozen_inference_graph.onnx";
        private readonly float _minScore;
        private readonly InferenceSession _inferenceSession;

        public Model(float threshold)
        {
            _minScore = threshold;
            _inferenceSession = LoadModel(OnnxFile);
        }

        public IEnumerable<IObject> Infer(string imagePath, int width, int height)
        {
            using var image = Image.Load<Rgb24>(imagePath);
            var (resized, top, left) = ResizeImage(image); 
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("x", ExtractPixels(resized))
            };
            var result = _inferenceSession.Run(inputs).ToList();
            return FilterDetections(
                result, image.Width, image.Height, top, left);
        }

        public async Task<IEnumerable<IObject>> InferAsync(string imagePath, int width, int height)
        {
            return await Task.Run(() => Infer(imagePath, width, height));
        }
        public void Dispose()
        {
            _inferenceSession.Dispose();
        }

        private static InferenceSession LoadModel(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(fileName);
            using var ms = new MemoryStream();
            stream?.CopyTo(ms);
            return new InferenceSession(ms.ToArray());
        }
        
        private static (Image<Rgb24>, int, int) ResizeImage(Image<Rgb24> image)
        {
            using var output = new Image<Rgb24>(1984, 1984, Color.Gray);
            var (w, h) = (image.Width, image.Height); // image width and height
            var (xRatio, yRatio) = (1984 / (float)w, 1984 / (float)h); // x, y ratios
            var ratio = Math.Min(xRatio, yRatio); // ratio = resized / original
            var (width, height) = ((int)(w * ratio), (int)(h * ratio)); // roi width and height
            var (x, y) = ((1984 / 2) - (width / 2), (1984 / 2) - (height / 2)); // roi x and y coordinates
            
            image.Mutate(i => i.Resize(width, height));
            output.Mutate(i => i.DrawImage(image, new Point(x, y), 1.0f));

            return (output, y, x);
        }
        
        private static Tensor<float> ExtractPixels(Image<Rgb24> image)
        {
            var tensor = new DenseTensor<float>(new[] { 1, 1984, 1984, 3});
            const float scale = 1 / 255.0f;
            
            _ = Parallel.For(0, image.Height, y =>
                Parallel.For(0, image.Width, x =>
                {
                    tensor[0, y, x, 0] = image[x, y].R * scale; // r
                    tensor[0, y, x, 1] = image[x, y].G * scale; // g
                    tensor[0, y, x, 2] = image[x, y].B * scale; // b
                }));

            return tensor;
        }
        
        private IEnumerable<IObject> FilterDetections(IReadOnlyList<DisposableNamedOnnxValue> resultArr, int imageWidth, int imageHeight, int top, int left)
        {
            var boxes = resultArr[0].Value as DenseTensor<float>;
            var scores = resultArr[1].Value as DenseTensor<float>;
            var validDetections = resultArr[3].Value as DenseTensor<int>;
            var filteredObjects = new List<DetectedObject>();
            
            if (boxes == null) return filteredObjects;
            if (scores == null) return filteredObjects;
            if (validDetections == null) return filteredObjects;

            for (var i = 0; i < validDetections[0]; i++)
            {
                var score = scores[0, i];
                if (score < _minScore)
                    continue;
                
                var x0 = boxes[0, i, 0];
                var x1 = boxes[0, i, 2];
                var y0=boxes[0, i, 1];
                var y1=boxes[0, i, 3];
                x0 = (x0 - (float)left / 1984) / (1 - 2 * (float)left / 1984);
                x1 = (x1 - (float)left / 1984) / (1 - 2 * (float)left / 1984);
                y0 = (y0 - (float)top / 1984) / (1 - 2 * (float)top / 1984);
                y1 = (y1 - (float)top / 1984) / (1 - 2 * (float)top / 1984);

                var label = "Pedestrian";
                var obj = new DetectedObject
                {
                    Label = label,
                    Score = score,
                    XMin = (int)(x0 * imageWidth),
                    XMax = (int)(x1 * imageWidth),
                    YMin = (int)(y0 * imageHeight),
                    YMax = (int)(y1 * imageHeight)
                };
                filteredObjects.Add(obj);
            }

            return filteredObjects;
        }
    }
}