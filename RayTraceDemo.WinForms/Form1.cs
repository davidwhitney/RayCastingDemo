using System.IO;
using System.Threading;
using System.Windows.Forms;
using RayTraceDemo.RayCasting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = System.Drawing.Image;
using Keys = System.Windows.Forms.Keys;
using Timer = System.Timers.Timer;

namespace RayTraceDemo.WinForms
{
    public partial class Form1 : Form
    {
        private readonly BitmapRenderer _renderer;
        private readonly Camera _camera;
        private readonly int _renderWidth;
        private readonly int _renderHeight;
        private readonly PictureBox _p;
        private readonly byte[] _bgBytes;
        private readonly Timer _timer;
        private Image<Rgba32> _bgImg;

        public Form1()
        {
            InitializeComponent();

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

            _renderWidth = 2560;
            _renderHeight = 1440;

            _bgBytes = LoadAndSizeBackground(_renderWidth, _renderHeight).ToArray();
            _bgImg = SixLabors.ImageSharp.Image.Load<Rgba32>(_bgBytes);

            _p = new PictureBox {Width = _renderWidth, Height = _renderHeight};
            Controls.Add(_p);
            
            _camera = new Camera(world.CameraLocation, world) { DirectionInDegrees = 0 };
            _renderer = new BitmapRenderer(_renderHeight, _renderWidth);

            KeyDown += KeyDownHandler;

            const int fps = 30;
            _timer = new Timer((1000 / fps)) {AutoReset = true};
            _timer.Elapsed += OnInterval;
            _timer.Start();
        }

        private static MemoryStream LoadAndSizeBackground(int renderWidth, int renderHeight)
        {
            var bgBytes = File.ReadAllBytes("bg.jpg");
            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(bgBytes);
            var memoryStream = new MemoryStream();
            img.Mutate(x => x.Resize(renderWidth, renderHeight));
            img.SaveAsBmp(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _timer.Dispose();
        }

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    _camera.Location2D = new Location2D {X = _camera.Location2D.X, Y = _camera.Location2D.Y - 0.05};
                    break;
                case Keys.Down:
                    _camera.Location2D = new Location2D {X = _camera.Location2D.X, Y = _camera.Location2D.Y + 0.05};
                    break;
                case Keys.Left:
                    _camera.DirectionInDegrees -= 1;
                    break;
                case Keys.Right:
                    _camera.DirectionInDegrees += 1;
                    break;
            }
        }

        private void OnInterval(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!Monitor.TryEnter(_p)) return;

            var result = _camera.Render(_renderWidth);
            var pixels = _renderer.RenderBitmap(result.Columns, _camera);
            
            var img = _bgImg.Clone();
            for (var y = 0; y < _renderHeight; y++)
            {
                for (var x = 0; x < _renderWidth; x++)
                {
                    if (pixels[x, y] == null)
                    {
                        continue;
                    }

                    img[x, y] = pixels[x, y].Value;
                }
            }

            var memoryStream = new MemoryStream();
            img.SaveAsBmp(memoryStream);
            _p.Image = Image.FromStream(memoryStream);
            img.Dispose();

            Monitor.Exit(_p);
        }
    }
}
