using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RayTraceDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var world = new Map(new [] {
                "####################",
                "#                  #",
                "#      ######      #",
                "#         ###      #",
                "###       ###      #",
                "###                #",
                "###                #",
                "###                #",
                "###         c      #",
                "####################",
            });

            const int renderWidth = 2560;
            const int renderHeight = 1440;

            var camera = new Camera(world.CameraLocation.X, world.CameraLocation.Y, world) { DirectionInDegrees = 180 };
            var rayContacts = new List<Intersection>();

            for (var column = 0.0; column < renderWidth; column++)
            {
                var x = column / renderWidth - 0.5;
                var angle = Math.Atan2(x, camera.FocalLength);
                var ray = camera.SetDirection(angle).Ray().ToList();
                rayContacts.Add(ray.Last());
            }
            
            DrawToImage(renderHeight, renderWidth, rayContacts, camera);

            Console.Write("\r\n");
            Console.ReadKey();
        }

        private static void DrawToImage(int renderHeight, int renderWidth, IReadOnlyList<Intersection> rayContacts, Camera camera)
        {
            var pixels = new Rgba32[renderHeight, renderWidth];

            for (var x = 0; x < rayContacts.Count; x++)
            {
                var ray = rayContacts[x];

                var thisHeight = (renderHeight * ray.Height) / (ray.Distance / 3);
                thisHeight = ray.Height <= 0 ? 0 : thisHeight;
                
                var texture = SelectTexture(ray, camera);

                var height = Math.Ceiling(thisHeight);
                height = height > renderHeight ? renderHeight : height;

                var offset = (int)Math.Floor((renderHeight - height) / 2);

                for (var y = 0; y < height; y++)
                {
                    var index = renderHeight - y - 1;
                    index = index < 0 ? 0 : index;

                    index -= offset;

                    pixels[index, x] = texture;
                }
            }
            
            using var img = new Image<Rgba32>(renderWidth, renderHeight);
            
            for (var y = 0; y < renderHeight; y++)
            {
                for (var x = 0; x < renderWidth; x++)
                {
                    img[x, y] = pixels[y, x];
                }
            }

            var memoryStream = new MemoryStream();
            img.SaveAsJpeg(memoryStream);
            memoryStream.Position = 0;
            File.WriteAllBytes("out.jpg", memoryStream.ToArray());

            var processStartInfo = new ProcessStartInfo { FileName = Path.Combine(Environment.CurrentDirectory, "out.jpg"), UseShellExecute = true };
            Process.Start(processStartInfo);
        }

        private static Rgba32 SelectTexture(Intersection ray, Camera camera)
        {
            var percentage = (ray.Distance / camera.Range) * 100;
            var brightness = 255 - ((255 / 100) * percentage);
            return new Rgba32((byte)brightness, (byte)brightness, (byte)brightness);
        }
    }

    public class Map
    {
        public List<string> Topology { get; }
        public int Size { get; }
        public Location2D CameraLocation { get; }

        public Map(IEnumerable<string> topology)
        {
            Topology = topology.ToList();
            Size = Topology.First().Length;

            var playerRow = Topology.Single(line => line.Contains("c"));
            var cameraY = Topology.IndexOf(playerRow);
            var cameraX = Topology[cameraY].IndexOf("c", StringComparison.Ordinal);
            CameraLocation = new Location2D { X = cameraX, Y = cameraY };
            Topology[cameraY] = Topology[cameraY].Replace("c", " ");
        }
    }

    public class Camera
    {
        public Location2D Location2D { get; set; }
        public double DirectionInDegrees { get; set; }
        public CastDirection CurrentDirection { get; set; }
        public int Range { get; set; }
        public double FocalLength { get; }

        public Map World { get; }

        public Camera(double x, double y, Map world, int range = 14, double focalLength = 0.8)
        {
            Location2D = new Location2D { X = x, Y = y };
            World = world;
            Range = range;
            FocalLength = focalLength;
        }

        public Camera SetDirection(double angle)
        {
            CurrentDirection = new CastDirection(DirectionInDegrees + angle);
            return this;
        }

        public IEnumerable<Intersection> Ray() => Ray(new Intersection(Location2D));

        public IEnumerable<Intersection> Ray(Intersection origin)
        {
            var stepX = ComputeNextStepLocation(CurrentDirection.Sin, CurrentDirection.Cos, origin.Location.X, origin.Location.Y);
            var stepY = ComputeNextStepLocation(CurrentDirection.Cos, CurrentDirection.Sin, origin.Location.Y, origin.Location.X, true);
            
            var nextStep = stepX.Length2 < stepY.Length2
                ? Inspect(stepX, 1, 0, origin.Distance, stepX.Location.Y)
                : Inspect(stepY, 0, 1, origin.Distance, stepY.Location.X);

            if (nextStep.Distance > Range)
            {
                return new List<Intersection> {origin};
            }

            if (nextStep.Height > 0)
            {
                return new List<Intersection> { nextStep };
            }

            return new List<Intersection> {origin}.Concat(Ray(nextStep));
        }

        public Intersection ComputeNextStepLocation(double rise, double run, double x, double y, bool inverted = false)
        {
            if (run == 0.0)
            {
                return Intersection.NoWall;
            }

            var dx = run > 0 ? Math.Floor(x + 1) - x : Math.Ceiling(x - 1) - x;
            var dy = dx * (rise / run);

            return new Intersection
            {
                Location = new Location2D 
                {
                    X = inverted ? y + dy : x + dx,
                    Y = inverted ? x + dx : y + dy
                },
                Length2 = dx * dx + dy * dy
            };
        }

        public Intersection Inspect(Intersection step, int shiftX, int shiftY, double distance, double offset)
        {
            var dx = CurrentDirection.Cos < 0 ? shiftX : 0;
            var dy = CurrentDirection.Sin < 0 ? shiftY : 0;
            
            step.Height = CalculateHeight(step.Location.X - dx, step.Location.Y - dy);
            step.Distance = distance + Math.Sqrt(step.Length2);
            step.Offset = offset - Math.Floor(offset);

            return step;
        }

        public int CalculateHeight(double xDouble, double yDouble)
        {
            var x = (int) Math.Floor(xDouble);
            var y = (int) Math.Floor(yDouble);
            
            if (x < 0 || x > World.Size - 1 || y < 0 || y > World.Size - 1)
            {
                return 0;
            }

            return World.Topology[y][x] == '#' ? 1 : 0;
        }
    }

    public class CastDirection
    {
        public double Sin { get; }
        public double Cos { get; }

        public CastDirection(double angle)
        {
            Sin = Math.Sin(angle);
            Cos = Math.Cos(angle);
        }
    }

    public class Intersection
    {
        public Location2D Location { get; set; }
        public double Length2 { get; set; }
        public double Height { get; set; }
        public double Distance { get; set; }

        public double Offset { get; set; }

        public Intersection()
        {
        }

        public Intersection(Location2D location2D, int length2 = 0, int height = 0, int distance = 0)
        {
            Location = location2D;
            Length2 = length2;
            Height = height;
            Distance = distance;
        }

        public static Intersection NoWall { get; } = new Intersection(new Location2D());
    }
    
    public struct Location2D
    {
        public double X;
        public double Y;
    }
}
