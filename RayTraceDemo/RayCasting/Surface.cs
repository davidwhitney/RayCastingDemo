namespace RayTraceDemo.RayCasting
{
    public class Surface
    {
        public double Height { get; set; }

        public bool CanBeSeenPast => Height < 1;
        public bool HasNoHeight => Height <= 0;

        // Other surface properties here

        public static Surface Nothing { get; } = new Surface();
    }
}