using System;
using System.Collections.Generic;
using System.Linq;

namespace RayTraceDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var world = new List<string>
            {
                "          ",
                "          ",
                "       ## ",
                "          ",
                "          ",
                " ###      ",
                "          ",
                "     c    ",
                "          ",
                "          ",
            };

            var playerRow = world.Single(line => line.Contains("c"));
            var cameraY = world.IndexOf(playerRow);
            var cameraX = world[cameraY].IndexOf("c", StringComparison.Ordinal);

            var camera = new Camera(cameraX, cameraY);

            const int resolution = 320;
            const double focalLength = 0.8;
            const int range = 14;

            for (var column = 0; column < resolution; column++)
            {
                var x = column / resolution - 0.5;
                var angle = Math.Atan2(x, focalLength);
                var ray = map.cast(camera, camera.DirectionInDegrees + angle, camera.Range);
            }
        }

        public class Camera
        {
            public Location Location { get; set; }
            public int DirectionInDegrees { get; set; }
            public int Range { get; set; }
            public double FocalLength { get; }

            public Camera(int x, int y, int range = 14, double focalLength = 0.8)
            {
                Location = new Location {X = x, Y = y};
                Range = range;
                FocalLength = focalLength;
            }
        }

        public struct Location
        {
            public int X;
            public int Y;
        }
    }
}
