using System;
using System.Collections.Generic;
using System.Linq;

namespace RayTraceDemo.RayCasting
{
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

        public string ToDebugString(IEnumerable<Ray.SamplePoint> markLocations)
        {
            var copy = new List<string>(Topology);
            foreach (var item in markLocations)
            {
                var intX = (int) item.Location.X;
                var intY = (int) item.Location.Y;

                var temp = copy[intY].ToCharArray();
                temp[intX] = '.';
                copy[intY] = new string(temp);
            }
            return string.Join(Environment.NewLine, copy);
        }
    }
}