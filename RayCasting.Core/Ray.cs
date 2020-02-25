using System.Collections.Generic;

namespace RayCasting.Core
{
    public class Ray : List<Ray.SamplePoint>
    {
        public int Column { get; }

        public Ray(int column)
        {
            Column = column;
        }

        public struct SamplePoint
        {
            public Location2D Location { get; set; }
            public double Length { get; set; }
            public double Distance { get; set; }
            public Surface Surface { get; set; }

            public SamplePoint(Location2D location2D, double length = 0, double distance = 0)
            {
                Location = location2D;
                Length = length;
                Distance = distance;
                Surface =  Surface.Nothing;
            }
        }
    }
}