using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LacmusPlugin;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

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
            using var image = Image.FromFile(imagePath);
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
        
        private static (Bitmap, int, int) ResizeImage(Image image)
        {
            PixelFormat format = image.PixelFormat;

            var output = new Bitmap(1984, 1984, format);

            var (w, h) = (image.Width, image.Height); // image width and height
            var (xRatio, yRatio) = (1984 / (float)w, 1984 / (float)h); // x, y ratios
            var ratio = Math.Min(xRatio, yRatio); // ratio = resized / original
            var (width, height) = ((int)(w * ratio), (int)(h * ratio)); // roi width and height
            var (x, y) = ((1984 / 2) - (width / 2), (1984 / 2) - (height / 2)); // roi x and y coordinates
            var roi = new Rectangle(x, y, width, height); // region of interest

            using (var graphics = Graphics.FromImage(output))
            {
                graphics.Clear(Color.Gray); // clear canvas

                graphics.SmoothingMode = SmoothingMode.None; // no smoothing
                graphics.InterpolationMode = InterpolationMode.Bilinear; // bilinear interpolation
                graphics.PixelOffsetMode = PixelOffsetMode.Half; // half pixel offset

                graphics.DrawImage(image, roi); // draw scaled
            }

            return (output, y, x);
        }
        
        private static Tensor<float> ExtractPixels(Bitmap bitmap)
        {
            var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var tensor = new DenseTensor<float>(new[] { 1, 1984, 1984, 3});

            unsafe // speed up conversion by direct work with memory
            {
                Parallel.For(0, bitmapData.Height, (y) =>
                {
                    var row = (byte*)bitmapData.Scan0 + (y * bitmapData.Stride);

                    Parallel.For(0, bitmapData.Width, (x) =>
                    {
                        tensor[0, y, x, 0] = row[x * bytesPerPixel + 2] / 255.0F; // r
                        tensor[0, y, x, 1] = row[x * bytesPerPixel + 1] / 255.0F; // g
                        tensor[0, y, x, 2] = row[x * bytesPerPixel + 0] / 255.0F; // b
                    });
                });

                bitmap.UnlockBits(bitmapData);
            }

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