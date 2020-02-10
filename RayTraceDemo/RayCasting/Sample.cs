using System.Collections.Generic;

namespace RayTraceDemo.RayCasting
{
    public class Ray : List<Ray.SamplePoint>
    {
        public class SamplePoint
        {
            public Location2D Location { get; set; }
            public double Length { get; set; }
            public double Height { get; set; }
            public double Distance { get; set; }

            public SamplePoint()
            {
            }

            public SamplePoint(Location2D location2D, int length = 0, int height = 0, int distance = 0)
            {
                Location = location2D;
                Length = length;
                Height = height;
                Distance = distance;
            }

            public bool IsNotAVerticalSurface => Height <= 0;
        }
    }
}