using System;
using System.Drawing;
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
        private Timer _timer;

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

            _bgBytes = File.ReadAllBytes("bg.jpg");
            _p = new PictureBox {Width = _renderWidth, Height = _renderHeight};
            Controls.Add(_p);
            
            _camera = new Camera(world.CameraLocation, world) { DirectionInDegrees = 0 };
            _renderer = new BitmapRenderer(_renderHeight, _renderWidth);

            KeyDown += KeyDownHandler;

            _timer = new Timer((1000 / 30)) {AutoReset = true};
            _timer.Elapsed += OnInterval;
            _timer.Start();
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
            if(Monitor.TryEnter(_p))
            {
                var result = _camera.Render(_renderWidth);
                var pixels = _renderer.RenderBitmap(result.Columns, _camera);

                using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(_bgBytes);
                img.Mutate(x => x.Resize(_renderWidth, _renderHeight));

                for (var y = 0; y < _renderHeight; y++)
                {
                    for (var x = 0; x < _renderWidth; x++)
                    {
                        var rgba32 = pixels[x, y];
                        if (rgba32 == null)
                        {
                            continue;
                        }

                        img[x, y] = rgba32.Value;
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
}
