using System;
using System.Collections.Concurrent;

namespace RayCasting.Core
{
    public class Camera
    {
        public Location2D Location2D { get; set; }
        public Map World { get; }
        public int Range { get; }
        public double FocalLength { get; }

        public double DirectionInDegrees
        {
            get => _directionInDegrees;
            set => _directionInDegrees = value % 360;
        }

        private double _directionInDegrees;

        public Camera(Location2D location, Map world, int range = 25, double focalLength = 0.8)
        {
            Location2D = location;
            World = world;
            Range = range;
            FocalLength = focalLength;
        }

        public RenderResult Snapshot(int renderWidth, bool includeDebugInfo = false)
        {
            var result = new RenderResult(renderWidth);

            for (var column = 0; column < renderWidth; column++)
            {
                var x = (double) column / renderWidth - 0.5;
                var angle = Math.Atan2(x, FocalLength);

                var castDirection = ComputeDirection(DirectionInDegrees, angle);
                var ray = Ray(column, new Ray.SamplePoint(Location2D), castDirection);

                result.Columns[column] = ray[^1];

                if (includeDebugInfo)
                {
                    ray.ForEach(i => result.AllSamplePoints.Add(i));
                }
            }

            return result;
        }

        private static CastDirection ComputeDirection(double directionDegrees, double angle)
        {
            // Covert to radians so angle calc works
            // The - 90.1 degrees is to re-orientate the player to be "facing upwards" in the world by default
            // rather than to the left (following array index direction).

            var radians = Math.PI / 180 * directionDegrees; 
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

        public struct RenderResult
        {
            public Ray.SamplePoint[] Columns { get; set; }
            public ConcurrentBag<Ray.SamplePoint> AllSamplePoints { get; }

            public RenderResult(int renderWidth)
            {
                Columns = new Ray.SamplePoint[renderWidth];
                AllSamplePoints = new ConcurrentBag<Ray.SamplePoint>();
            }
        }
    }
}