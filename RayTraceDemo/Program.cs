using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RayTraceDemo.RayCasting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RayTraceDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var world = new Map(new[]
            {
                "########################################",
                "#                                      #",
                "#      #########                       #",
                "#         ###                          #",
                "###       ###                          #",
                "###                                    #",
                "##                                     #",
                "#                                      #",
                "#                                      #",
                "#            c                         #",
                "###                                    #",
                "###                                    #",
                "########################################",
            });

            const int renderWidth = 2560;
            const int renderHeight = 1440;

            var camera = new Camera(world.CameraLocation.X, world.CameraLocation.Y, world) {DirectionInDegrees = 4};

            var allSamples = new List<Ray.SamplePoint>();
            var finalSamplePoints = new List<Ray.SamplePoint>();

            for (var column = 0; column < renderWidth; column++)
            {
                var x = (double) column / renderWidth - 0.5;
                var angle = Math.Atan2(x, camera.FocalLength);

                var ray = camera.SetDirection(angle).Ray();

                allSamples.AddRange(ray);
                finalSamplePoints.Add(ray.Last());
            }

            var renderer = new BitmapRenderer(renderHeight, renderWidth);
            var pixels = renderer.RenderBitmap(finalSamplePoints, camera);
            var path = SaveToJpeg(renderHeight, renderWidth, pixels);

            Process.Start(new ProcessStartInfo {FileName = path, UseShellExecute = true});

            Console.WriteLine(world.ToDebugString(allSamples));
            Console.ReadKey();
        }


        private static string SaveToJpeg(int renderHeight, int renderWidth, Rgba32[,] pixels)
        {
            using var img = new Image<Rgba32>(renderWidth, renderHeight);

            for (var y = 0; y < renderHeight; y++)
            {
                for (var x = 0; x < renderWidth; x++)
                {
                    img[x, y] = pixels[x, y];
                }
            }

            var memoryStream = new MemoryStream();
            img.SaveAsJpeg(memoryStream);
            memoryStream.Position = 0;
            File.WriteAllBytes("out.jpg", memoryStream.ToArray());

            return Path.Combine(Environment.CurrentDirectory, "out.jpg");
        }
    }
}
