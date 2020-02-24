using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;

namespace RayCasting.Core
{
    public class BitmapRenderer
    {
        public int SampleHeight { get; }
        public int SampleWidth { get; }
        public int SampleScale { get; } = 1;

        public int Height => SampleHeight * SampleScale;
        public int Width => SampleWidth * SampleScale;

        public BitmapRenderer(int sampleHeight, int sampleWidth, int sampleScale = 1)
        {
            SampleScale = sampleScale;
            SampleHeight = sampleHeight / SampleScale;
            SampleWidth = sampleWidth / SampleScale;
        }

        public Rgba32?[,] RenderBitmap(IReadOnlyList<Ray.SamplePoint> columnData, Camera camera)
        {
            var pixels = new Rgba32?[SampleWidth, SampleHeight];

            Parallel.For(0, columnData.Count, column =>
            {
                var samplePoint = columnData[column];

                var height = (SampleHeight * samplePoint.Surface.Height) / (samplePoint.Distance / 2.5);
                height = height <= 0 ? 0 : height;
                height = Math.Ceiling(height);
                height = height > SampleHeight ? SampleHeight : height;

                var offset = (int) Math.Floor((SampleHeight - height) / 2);

                var texture = SelectTexture(samplePoint, camera);

                for (var y = 0; y < height; y++)
                {
                    var yCoordinate = SampleHeight - y - 1;
                    yCoordinate = yCoordinate < 0 ? 0 : yCoordinate;
                    yCoordinate -= offset;

                    pixels[column, yCoordinate] = texture;
                }
            });
            
            return pixels;
        }

        private static Rgba32 SelectTexture(Ray.SamplePoint samplePoint, Camera camera)
        {
            var percentage = (samplePoint.Distance / camera.Range) * 100;
            var brightness = 200 - ((200.00 / 100) * percentage);
            return new Rgba32((byte) brightness, (byte) brightness, (byte) brightness);
        }
    }
}