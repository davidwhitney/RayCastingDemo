using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace RayCasting.Cli
{
    public static class JpegSaver
    {
        public static byte[] SaveToJpeg(Rgba32?[,] pixels)
        {
            var width = pixels.GetLength(0);
            var height = pixels.GetLength(1);

            using var img = Image.Load<Rgba32>(File.ReadAllBytes("bg.jpg"));
            img.Mutate(x => x.Resize(width, height));

            Parallel.For(0, height, y =>
            {
                for (var x = 0; x < width; x++)
                {
                    var rgba32 = pixels[x, y];
                    if (rgba32 == null)
                    {
                        continue;
                    }

                    img[x, y] = rgba32.Value;
                }
            });

            var memoryStream = new MemoryStream();
            img.SaveAsJpeg(memoryStream);
            return memoryStream.ToArray();
        }
    }
}