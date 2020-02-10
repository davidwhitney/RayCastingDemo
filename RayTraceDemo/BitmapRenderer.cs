using System;
using System.Collections.Generic;
using RayTraceDemo.RayCasting;
using SixLabors.ImageSharp.PixelFormats;

namespace RayTraceDemo
{
    public class BitmapRenderer
    {
        public int RenderHeight { get; }
        public int RenderWidth { get; }

        public BitmapRenderer(int renderHeight, int renderWidth)
        {
            RenderHeight = renderHeight;
            RenderWidth = renderWidth;
        }

        public Rgba32?[,] RenderBitmap(IReadOnlyList<Ray.SamplePoint> columnData, Camera camera)
        {
            var pixels = new Rgba32?[RenderWidth, RenderHeight];

            for (var column = 0; column < columnData.Count; column++)
            {
                var samplePoint = columnData[column];

                var height = (RenderHeight * samplePoint.Surface.Height) / (samplePoint.Distance / 2.5);
                height = height <= 0 ? 0 : height;
                height = Math.Ceiling(height);
                height = height > RenderHeight ? RenderHeight : height;

                var offset = (int) Math.Floor((RenderHeight - height) / 2);

                var texture = SelectTexture(samplePoint, camera);

                for (var y = 0; y < height; y++)
                {
                    var yCoord = RenderHeight - y - 1;
                    yCoord = yCoord < 0 ? 0 : yCoord;
                    yCoord -= offset;

                    pixels[column, yCoord] = texture;
                }
            }

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