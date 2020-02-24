using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RayCasting.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = System.Drawing.Image;
using Keys = System.Windows.Forms.Keys;
using Timer = System.Timers.Timer;

namespace RayCasting.WinForms
{
    public partial class Form1 : Form
    {
        private readonly BitmapRenderer _renderer;
        private readonly Camera _camera;
        private readonly PictureBox _p;
        private readonly Timer _timer;
        private readonly Image<Rgba32> _bgImg;

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

            _renderer = new BitmapRenderer(1440, 2560);
            _bgImg = LoadAndSizeBackground(_renderer.SampleWidth, _renderer.SampleHeight);

            _p = new PictureBox {Width = _renderer.Width, Height = _renderer.Height };
            Controls.Add(_p);
            
            _camera = new Camera(world.CameraLocation, world) { DirectionInDegrees = 0 };

            KeyDown += KeyDownHandler;
            _p.MouseDown += OnMouseDown;
            _p.MouseUp += OnMouseUp;
            _p.MouseMove += Move;

            const int fps = 60;
            _timer = new Timer((1000 / fps)) {AutoReset = true};
            _timer.Elapsed += OnInterval;
            _timer.Start();
        }

        private Point _previousLocation;
        private bool _captured;

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            _previousLocation = e.Location;
            _captured = true;
            Cursor.Hide();
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _captured = false;
            Cursor.Show();
        }

        private void Move(object sender, MouseEventArgs e)
        {
            if (!_captured) return;

            var difference = e.Location - (Size)_previousLocation;
            _camera.DirectionInDegrees += (double)difference.X / 30;
            _previousLocation = e.Location;
        }


        private static Image<Rgba32> LoadAndSizeBackground(int renderWidth, int renderHeight)
        {
            var img = SixLabors.ImageSharp.Image.Load<Rgba32>(File.ReadAllBytes("bg.jpg"));
            img.Mutate(x => x.Resize(renderWidth, renderHeight));
            return img;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _timer.Dispose();
        }

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            var degrees = _camera.DirectionInDegrees + 90.0;
            var x = Math.Cos(degrees * Math.PI / 180) * 0.35;
            var y = Math.Sin(degrees * Math.PI / 180) * 0.35;

            switch (e.KeyCode)
            {
                case Keys.Up:
                    _camera.Location2D = new Location2D {X = _camera.Location2D.X - x, Y = _camera.Location2D.Y - y};
                    break;
                case Keys.Down:
                    _camera.Location2D = new Location2D {X = _camera.Location2D.X + x, Y = _camera.Location2D.Y + y};
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

            var result = _camera.Snapshot(_renderer.SampleWidth);
            var pixels = _renderer.RenderBitmap(result.Columns, _camera);
            
            var img = _bgImg.Clone();
            for (var y = 0; y < _renderer.SampleHeight; y++)
            {
                for (var x = 0; x < _renderer.SampleWidth; x++)
                {
                    if (pixels[x, y] == null)
                    {
                        continue;
                    }

                    img[x, y] = pixels[x, y].Value;
                }
            }

            if (_renderer.SampleScale != 1)
            {
                img.Mutate(x => x.Resize(_renderer.Width, _renderer.Height));
            }
            
            var memoryStream = new MemoryStream();
            img.SaveAsBmp(memoryStream);
            _p.Image = Image.FromStream(memoryStream);
            img.Dispose();

            Monitor.Exit(_p);
        }
    }
}
