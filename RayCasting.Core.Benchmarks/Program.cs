using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace RayCasting.Core.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<RenderSmallWorld>();
        }
    }

    public class RenderSmallWorld
    {
        private readonly BitmapRenderer _renderer;
        private readonly Camera _camera;

        public RenderSmallWorld()
        {
            var world = new Map(new[]
            {
                "########################################",
                "#                                      #",
                "#      #########                       #",
                "#         ###                          #",
                "###       ###                          #",
                "###                                    #",
                "##                                     #",
                "#                               ########",
                "#                               ########",
                "#                                      #",
                "###                                    #",
                "###        c                           #",
                "########################################",
            });

            _camera = new Camera(world.CameraLocation, world) { DirectionInDegrees = 0 };
            _renderer = new BitmapRenderer(1440, 2560);

        }

        [Benchmark]
        public void RenderToBitmap()
        {
            var result = _camera.Snapshot(_renderer.Width);
            var pixels = _renderer.RenderBitmap(result.Columns, _camera);
        }
    }
}