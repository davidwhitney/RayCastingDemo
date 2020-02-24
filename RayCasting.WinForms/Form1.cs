using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly PictureBox _renderedImage;
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
                "#        c                      ########",
                "#                                      #",
                "###                                    #",
                "###                                    #",
                "########################################",
            });

            _renderer = new BitmapRenderer(1440, 2560);
            _bgImg = LoadAndSizeBackground(_renderer.SampleWidth, _renderer.SampleHeight);
            _camera = new Camera(world.CameraLocation, world) { DirectionInDegrees = 0 };

            _renderedImage = new PictureBox {Width = _renderer.Width, Height = _renderer.Height};
            Controls.Add(_renderedImage);

            KeyDown += KeyDownHandler;
            _renderedImage.MouseDown += OnMouseDown;
            _renderedImage.MouseUp += OnMouseUp;
            _renderedImage.MouseMove += MeasureMovement;

            const int fps = 60;
            _timer = new Timer((1000 / fps)) {AutoReset = true};
            _timer.Elapsed += OnInterval;
            _timer.Start();
        }

        private Point _originLocation;
        private Point _previousLocation;
        private bool _captured;

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            _originLocation = Cursor.Position;
            _previousLocation = e.Location;
            _captured = true;
            Cursor.Hide();
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _captured = false;
            Cursor.Position = _originLocation;
            Cursor.Show();
        }

        private void MeasureMovement(object sender, MouseEventArgs e)
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
            var degrees = _camera.DirectionInDegrees;
            var x = Math.Cos(degrees * Math.PI / 180) * 0.35;
            var y = Math.Sin(degrees * Math.PI / 180) * 0.35;

            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.W:
                    _camera.Location2D = new Location2D {X = _camera.Location2D.X + x, Y = _camera.Location2D.Y + y};
                    break;
                case Keys.Down:
                case Keys.S:
                    _camera.Location2D = new Location2D {X = _camera.Location2D.X - x, Y = _camera.Location2D.Y - y};
                    break;
                case Keys.Left:
                case Keys.A:
                    _camera.DirectionInDegrees -= 1;
                    break;
                case Keys.Right:
                case Keys.D:
                    _camera.DirectionInDegrees += 1;
                    break;
            }
        }

        private void OnInterval(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!Monitor.TryEnter(_renderedImage)) return;

            var result = _camera.Snapshot(_renderer.SampleWidth);
            var pixels = _renderer.RenderBitmap(result.Columns, _camera);
            
            var img = _bgImg.Clone();

            Parallel.For(0, _renderer.SampleHeight, y =>
            {
                for (var x = 0; x < _renderer.SampleWidth; x++)
                {
                    if (pixels[x, y] == null)
                    {
                        continue;
                    }

                    img[x, y] = pixels[x, y].Value;
                }
            });

            if (_renderer.SampleScale != 1)
            {
                img.Mutate(x => x.Resize(_renderer.Width, _renderer.Height));
            }
            
            var memoryStream = new MemoryStream();
            img.SaveAsBmp(memoryStream);
            img.Dispose();

            _renderedImage.Image = Image.FromStream(memoryStream);

            Monitor.Exit(_renderedImage);
        }
    }
}
