using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LacmusPlugin
{
    public interface IObjectDetectionModel : IDisposable
    {
        public IEnumerable<IObject> Infer(string imagePath, int width, int height);
        public Task<IEnumerable<IObject>> InferAsync(string imagePath, int width, int height);
    }
}