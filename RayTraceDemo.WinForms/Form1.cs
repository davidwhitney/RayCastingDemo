using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RayTraceDemo.RayCasting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = System.Drawing.Image;
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


            KeyDown += KeyPress;

            var timer = new Timer((1000 / 60)) {AutoReset = true};
            timer.Elapsed += OnInterval;
            timer.Start();
        }

        private void KeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                _camera.Location2D = new Location2D {X = _camera.Location2D.X, Y = _camera.Location2D.Y - 0.05};
            }

            if (e.KeyCode == Keys.Down)
            {
                _camera.Location2D = new Location2D {X = _camera.Location2D.X, Y = _camera.Location2D.Y + 0.05};
            }

            if (e.KeyCode == Keys.Left)
            {
                _camera.DirectionInDegrees -= 1;
            }
            if (e.KeyCode == Keys.Right)
            {
                _camera.DirectionInDegrees += 1;
            }
        }

        private void OnInterval(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_p)
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
            }
        }
    }
}
