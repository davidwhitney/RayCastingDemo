using System;

namespace RayTraceDemo.RayCasting
{
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

        public Ray Ray() => Ray(new Ray.SamplePoint(Location2D));

        public Ray Ray(Ray.SamplePoint origin)
        {
            var rayPath = new Ray();
            var currentStep = origin;

            while (true)
            {
                rayPath.Add(currentStep);

                var stepX = ComputeNextStepLocation(CurrentDirection.Sin, CurrentDirection.Cos, currentStep.Location.X, currentStep.Location.Y);
                var stepY = ComputeNextStepLocation(CurrentDirection.Cos, CurrentDirection.Sin, currentStep.Location.Y, currentStep.Location.X, true);

                var nextStep = stepX.Length < stepY.Length
                    ? Inspect(stepX, 1, 0, currentStep.Distance)
                    : Inspect(stepY, 0, 1, currentStep.Distance);


                if (nextStep.IsNotAVerticalSurface)
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

        public Ray.SamplePoint ComputeNextStepLocation(double rise, double run, double x, double y, bool inverted = false)
        {
            var dx = run > 0 ? Math.Floor(x + 1) - x : Math.Ceiling(x - 1) - x;
            var dy = dx * (rise / run);

            return new Ray.SamplePoint
            {
                Location = new Location2D 
                {
                    X = inverted ? y + dy : x + dx,
                    Y = inverted ? x + dx : y + dy
                },
                Length = dx * dx + dy * dy
            };
        }

        public Ray.SamplePoint Inspect(Ray.SamplePoint step, int shiftX, int shiftY, double distance)
        {
            var dx = CurrentDirection.Cos < 0 ? shiftX : 0;
            var dy = CurrentDirection.Sin < 0 ? shiftY : 0;
            
            step.Height = CalculateHeight(step.Location.X - dx, step.Location.Y - dy);
            step.Distance = distance + Math.Sqrt(step.Length);

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
    }
}