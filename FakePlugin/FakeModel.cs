using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LacmusPlugin;

namespace FakePlugin
{
    public class FakeModel : IObjectDetectionModel
    {
        public IEnumerable<IObject> Infer(string imagePath, int width, int height)
        {
            var fakeObjects = new List<IObject>
            {
                new FakeObject
                {
                    Label = "FakeObject",
                    Score = 0.5f,
                    XMax = 100,
                    XMin = 10,
                    YMax = 200,
                    YMin = 20
                },
                new FakeObject
                {
                    Label = "FakeObject",
                    Score = 0.95f,
                    XMax = 50,
                    XMin = 5,
                    YMax = 20,
                    YMin = 2
                }
            };
            return fakeObjects;
        }
        public async Task<IEnumerable<IObject>> InferAsync(string imagePath, int width, int height)
        {
            return await Task.Run(() => Infer(imagePath, width, height));
        }
        public void Dispose()
        {
            return;
        }
    }
}