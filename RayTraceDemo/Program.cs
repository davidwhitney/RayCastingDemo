using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.Primitives;

namespace RayTraceDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var world = new Map(new [] {
                "    c     ",
                "          ",
                "       ## ",
                "          ",
                "          ",
                " ###      ",
                "          ",
                "          ",
                "          ",
                "          ",
            });
            
            const int resolution = 320;

            world.DefaultCamera.DirectionInDegrees = 90;

            for (var column = 0.00; column < resolution; column++)
            {
                var x = column / resolution - 0.5;
                var angle = Math.Atan2(x, world.DefaultCamera.FocalLength);
                var ray = world.DefaultCamera.SetDirection(angle).Cast();
                
                Console.WriteLine($"Column {column} {x} {angle}°: {ray.First().Height}");
            }

            Console.WriteLine("Any KEY to exit");
            Console.ReadKey();
        }
    }

    public class Map
    {
        public List<string> Topology { get; }
        public int Size { get; }
        public Camera DefaultCamera { get; }

        public Map(IEnumerable<string> topology)
        {
            Topology = topology.ToList();
            Size = Topology.First().Length;

            var playerRow = Topology.Single(line => line.Contains("c"));
            var cameraY = Topology.IndexOf(playerRow);
            var cameraX = Topology[cameraY].IndexOf("c", StringComparison.Ordinal);
            Topology[cameraY] = Topology[cameraY].Replace("c", " ");
            DefaultCamera = new Camera(cameraX, cameraY, this);
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

        public Camera(int x, int y, Map world, int range = 14, double focalLength = 0.8)
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

        public IEnumerable<Intersection> Cast() => Ray(new Intersection(Location2D));

        public IEnumerable<Intersection> Ray(Intersection origin)
        {
            var stepX = Step(CurrentDirection.Sin, CurrentDirection.Cos, origin.Location.X, origin.Location.Y);
            var stepY = Step(CurrentDirection.Cos, CurrentDirection.Sin, origin.Location.Y, origin.Location.X, true);
            
            var nextStep = stepX.Length2 < stepY.Length2
                ? Inspect(stepX, 1, 0, origin.Distance, stepX.Location.Y)
                : Inspect(stepY, 0, 1, origin.Distance, stepY.Location.X);

            return nextStep.Distance > Range
                ? new List<Intersection> { origin }
                : new List<Intersection> { origin }.Concat(Ray(nextStep));
        }

        public Intersection Step(double rise, double run, double x, double y, bool inverted = false)
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
            step.Shading = shiftX == 1 
                ? CurrentDirection.Cos < 0 ? 2 : 0 
                : CurrentDirection.Sin < 0 ? 2 : 1;

            step.Offset = offset - Math.Floor(offset);
            return step;
        }

        public int CalculateHeight(double xDouble, double yDouble)
        {
            var x = (int) Math.Floor(xDouble);
            var y = (int) Math.Floor(yDouble);

            if (x < 0 || x > this.World.Size - 1 || y < 0 || y > this.World.Size - 1)
            {
                return -1;
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

        public int Shading { get; set; }
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
