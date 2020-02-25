using System;
using System.Diagnostics;
using System.IO;
using RayCasting.Core;

namespace RayCasting.Cli
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var world = new Map(new[]
            {
                "########################################",
                "#                                      #",
                "#      #########                       #",
                "#         ###                          #",
                "###       ###            #             #",
                "###                      #             #",
                "##                     ###             #",
                "#          c           ###      ########",
                "#                      ###      ########",
                "#                        #             #",
                "###                                    #",
                "###                                    #",
                "########################################",
            });

            var camera = new Camera(world.CameraLocation, world) {DirectionInDegrees = 0};
            var renderer = new BitmapRenderer(1440, 2560);
            var result = camera.Snapshot(renderer.Width, true);
            var pixels = renderer.RenderBitmap(result.Columns, camera);
            
            var jpegByteArray = JpegSaver.SaveToJpeg(pixels);
            var asAsciiArt = SillyAsciiArtCreator.GenerateArt(jpegByteArray);

            if (Console.IsOutputRedirected)
            {
                Console.WriteLine(asAsciiArt);
                return;
            }

            Console.WriteLine("Rays cast to render image:");
            Console.WriteLine(world.ToDebugString(result.AllSamplePoints));

            Console.WindowHeight = 80;
            Console.WindowWidth = 140;
            Console.WriteLine(asAsciiArt);

            var path = Path.Combine(Environment.CurrentDirectory, "out.jpg");
            File.WriteAllBytes(path, jpegByteArray);
            Process.Start(new ProcessStartInfo {FileName = path, UseShellExecute = true});
        }
    }
}
