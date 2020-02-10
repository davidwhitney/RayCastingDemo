using System;
using System.Diagnostics;
using System.IO;
using RayTraceDemo.RayCasting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
                "#                               ########",
                "#                               ########",
                "#          c                            #",
                "###                                    #",
                "###                                    #",
                "########################################",
            });

            const int renderWidth = 2560;
            const int renderHeight = 1440;

            var camera = new Camera(world.CameraLocation, world) {DirectionInDegrees = 4};
            var renderer = new BitmapRenderer(renderHeight, renderWidth);

            var result = camera.Render(renderWidth);

            Console.WriteLine("Rays cast to render image:");
            Console.WriteLine(world.ToDebugString(result.AllSamplePoints));
            
            var pixels = renderer.RenderBitmap(result.Columns, camera);
            var path = SaveToJpeg(renderHeight, renderWidth, pixels);

            Process.Start(new ProcessStartInfo {FileName = path, UseShellExecute = true});

            Console.ReadKey();
        }


        private static string SaveToJpeg(int renderHeight, int renderWidth, Rgba32?[,] pixels)
        {
            using var img = Image.Load<Rgba32>(File.ReadAllBytes("bg.jpg"));
            img.Mutate(x => x.Resize(renderWidth, renderHeight));

            for (var y = 0; y < renderHeight; y++)
            {
                for (var x = 0; x < renderWidth; x++)
                {
                    var rgba32 = pixels[x, y];
                    if (rgba32 == null)
                    {
                        continue;
                    }
                    
                    img[x, y] = rgba32.Value;
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
