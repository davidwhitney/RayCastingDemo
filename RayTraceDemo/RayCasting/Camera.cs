using System;
using System.Collections.Generic;
using System.Linq;

namespace RayTraceDemo.RayCasting
{
    public class Camera
    {
        public Location2D Location2D { get; set; }
        public double DirectionInDegrees { get; set; }
        public int Range { get; set; }
        public double FocalLength { get; }
        public Map World { get; }

        public Camera(Location2D location, Map world, int range = 25, double focalLength = 0.8)
        {
            Location2D = location;
            World = world;
            Range = range;
            FocalLength = focalLength;
        }

        public RenderResult Render(int renderWidth)
        {
            var result = new RenderResult();

            for (var column = 0; column < renderWidth; column++)
            {
                var x = (double)column / renderWidth - 0.5;
                var angle = Math.Atan2(x, FocalLength);

                var castDirection = ComputeDirection(angle);
                var ray = Ray(column, new Ray.SamplePoint(Location2D), castDirection);

                result.AllSamplePoints.AddRange(ray);
                result.Columns.Add(ray.Last());
            }

            return result;
        }

        private CastDirection ComputeDirection(double angle)
        {
            // Covert to radians so angle calc works
            // The - 90.1 degrees is to re-orientate the player to be "facing upwards" in the world by default
            // rather than to the left (following array index direction).

            var radians = (Math.PI / 180) * (DirectionInDegrees - 90.1); 
            var directionInDegrees = radians + angle;
            return new CastDirection(directionInDegrees);
        }

        private Ray Ray(int column, Ray.SamplePoint origin, CastDirection castDirection)
        {
            var rayPath = new Ray(column);
            var currentStep = origin;

            while (true)
            {
                rayPath.Add(currentStep);

                var stepX = ComputeNextStepLocation(castDirection.Sin, castDirection.Cos, currentStep.Location.X, currentStep.Location.Y);
                var stepY = ComputeNextStepLocation(castDirection.Cos, castDirection.Sin, currentStep.Location.Y, currentStep.Location.X, true);

                var nextStep = stepX.Length < stepY.Length
                    ? Inspect(stepX, 1, 0, currentStep.Distance, castDirection)
                    : Inspect(stepY, 0, 1, currentStep.Distance, castDirection);


                if (nextStep.Surface.HasNoHeight)
                {
                    currentStep = nextStep;
                    continue;
                }

                if (nextStep.Distance > Range)
                {
                    return rayPath;
                }

                rayPath.Add(nextStep);
                return rayPath;
            }
        }

        private static Ray.SamplePoint ComputeNextStepLocation(double rise, double run, double x, double y, bool inverted = false)
        {
            var dx = run > 0 ? Math.Floor(x + 1) - x : Math.Ceiling(x - 1) - x;
            var dy = dx * (rise / run);

            var length = dx * dx + dy * dy;
            var location2D = new Location2D
            {
                X = inverted ? y + dy : x + dx,
                Y = inverted ? x + dx : y + dy
            };

            return new Ray.SamplePoint(location2D, length);
        }

        private Ray.SamplePoint Inspect(Ray.SamplePoint step, int shiftX, int shiftY, double distance, CastDirection castDirection)
        {
            var dx = castDirection.Cos < 0 ? shiftX : 0;
            var dy = castDirection.Sin < 0 ? shiftY : 0;
            
            step.Surface = DetectSurface(step.Location.X - dx, step.Location.Y - dy);
            step.Distance = distance + Math.Sqrt(step.Length);

            return step;
        }

        private Surface DetectSurface(double xDouble, double yDouble)
        {
            var x = (int) Math.Floor(xDouble);
            var y = (int) Math.Floor(yDouble);
            
            if (x < 0 || x > World.Size - 1 || y < 0 || y > World.Size - 1)
            {
                return Surface.Nothing;
            }

            return World.SurfaceAt(x, y);
        }

        private struct CastDirection
        {
            public double Sin { get; }
            public double Cos { get; }

            public CastDirection(double angle)
            {
                Sin = Math.Sin(angle);
                Cos = Math.Cos(angle);
            }
        }

        public class RenderResult
        {
            public List<Ray.SamplePoint> Columns { get; } = new List<Ray.SamplePoint>();
            public List<Ray.SamplePoint> AllSamplePoints { get; } = new List<Ray.SamplePoint>();
        }
    }
}