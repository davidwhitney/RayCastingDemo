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

            var camera = new Camera(world.CameraLocation.X, world.CameraLocation.Y, world) { DirectionInDegrees = 180 };
            var rayContacts = new List<PathNode>();

            for (var column = 0; column < renderWidth; column++)
            {
                var x = (double) column / renderWidth - 0.5;
                var angle = Math.Atan2(x, camera.FocalLength);
                var rayPath = camera.SetDirection(angle).Ray();
                rayContacts.Add(rayPath.Last());
            }

            DrawToImage(renderHeight, renderWidth, rayContacts, camera);

            Console.Write("\r\n");
            Console.ReadKey();
        }

        private static void DrawToImage(int renderHeight, int renderWidth, IReadOnlyList<PathNode> rayContacts, Camera camera)
        {
            var pixels = new Rgba32[renderWidth, renderHeight];

            for (var x = 0; x < rayContacts.Count; x++)
            {
                var ray = rayContacts[x];

                var thisHeight = (renderHeight * ray.Height) / (ray.Distance / 2.5);
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

                    pixels[x, index] = texture;
                }
            }
            
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

            var processStartInfo = new ProcessStartInfo { FileName = Path.Combine(Environment.CurrentDirectory, "out.jpg"), UseShellExecute = true };
            Process.Start(processStartInfo);
        }

        private static Rgba32 SelectTexture(PathNode ray, Camera camera)
        {
            var percentage = (ray.Distance / camera.Range) * 100;
            var brightness = 255 - ((255.00 / 100) * percentage);
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

        public Camera(double x, double y, Map world, int range = 25, double focalLength = 0.8)
        {
            Location2D = new Location2D { X = x, Y = y };
            World = world;
            Range = range;
            FocalLength = focalLength;
        }

        public Camera SetDirection(double angle)
        {
            var directionInDegrees = DirectionInDegrees + angle;
            CurrentDirection = new CastDirection(directionInDegrees);
            return this;
        }

        public List<PathNode> Ray() => Ray(new PathNode(Location2D));

        public List<PathNode> Ray(PathNode origin)
        {
            var rayPath = new List<PathNode>();
            var currentStep = origin;

            while (true)
            {
                rayPath.Add(currentStep);

                var stepX = ComputeNextStepLocation(CurrentDirection.Sin, CurrentDirection.Cos, currentStep.Location.X, currentStep.Location.Y);
                var stepY = ComputeNextStepLocation(CurrentDirection.Cos, CurrentDirection.Sin, currentStep.Location.Y, currentStep.Location.X, true);

                var nextStep = stepX.Length2 < stepY.Length2
                    ? Inspect(stepX, 1, 0, currentStep.Distance, stepX.Location.Y)
                    : Inspect(stepY, 0, 1, currentStep.Distance, stepY.Location.X);

                if (nextStep.Distance > Range)
                {
                    return rayPath;
                }

                if (nextStep.IsNotAVerticalSurface)
                {
                    currentStep = nextStep;
                    continue;
                }

                rayPath.Add(nextStep);
                return rayPath;
            }
        }

        public PathNode ComputeNextStepLocation(double rise, double run, double x, double y, bool inverted = false)
        {
            if (run == 0.0)
            {
                return PathNode.NoWall;
            }

            var dx = run > 0 ? Math.Floor(x + 1) - x : Math.Ceiling(x - 1) - x;
            var dy = dx * (rise / run);

            return new PathNode
            {
                Location = new Location2D 
                {
                    X = inverted ? y + dy : x + dx,
                    Y = inverted ? x + dx : y + dy
                },
                Length2 = dx * dx + dy * dy
            };
        }

        public PathNode Inspect(PathNode step, int shiftX, int shiftY, double distance, double offset)
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

    public class PathNode
    {
        public Location2D Location { get; set; }
        public double Length2 { get; set; }
        public double Height { get; set; }
        public double Distance { get; set; }

        public double Offset { get; set; }

        public PathNode()
        {
        }

        public PathNode(Location2D location2D, int length2 = 0, int height = 0, int distance = 0)
        {
            Location = location2D;
            Length2 = length2;
            Height = height;
            Distance = distance;
        }

        public static PathNode NoWall { get; } = new PathNode(new Location2D());

        public bool IsNotAVerticalSurface => Height <= 0;
    }
    
    public struct Location2D
    {
        public double X;
        public double Y;
    }
}
