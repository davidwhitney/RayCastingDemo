namespace RayCasting.Core
{
    public struct Surface
    {
        public double Height { get; set; }

        public bool HasNoHeight => Height <= 0;

        // Other surface properties here

        public static Surface Nothing { get; } = new Surface();
    }
}