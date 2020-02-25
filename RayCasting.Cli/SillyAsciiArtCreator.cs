using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace RayCasting.Cli
{
    public class SillyAsciiArtCreator
    {
        private static readonly Dictionary<float, string> Map;

        static SillyAsciiArtCreator()
        {
            Map = new Dictionary<float, string>
            {
                {200, " "},
                {190, " "},
                {180, " "},
                {170, " "},
                {160, " "},
                {150, "."},
                {140, "o"},
                {130, "O"},
                {120, "+"},
                {110, "#"},
                {100, "@"},
                {080, "%"},
                {060, "░"},
                {040, "▒"},
                {020, "▓"},
                {000, "█"},
            };
        }

        public static string GenerateArt(byte[] jpg)
        {
            var image = Image.Load(jpg);
            image.Mutate(x=>x.Resize(100, 50));

            var map = Map.OrderBy(kvp => kvp.Key);
            var sb = new StringBuilder();

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    var currentChar = MapColourToCharacter(map, pixel);

                    sb.Append(currentChar);
                }

                sb.Append(Environment.NewLine);
            }

            image.Dispose();

            return PostProcessOutput(sb);
        }

        private static string MapColourToCharacter(IEnumerable<KeyValuePair<float, string>> map, Rgba32 pixel)
        {
            var brightness = pixel.R;
            var currentChar = "";
            foreach (var kvp in map)
            {
                if (kvp.Key <= brightness)
                {
                    currentChar = kvp.Value;
                }
            }
            return currentChar;
        }

        private static string PostProcessOutput(StringBuilder sb)
        {
            var output = sb.ToString();
            sb.Clear();
            output = output.Substring(0, output.Length - Environment.NewLine.Length);
            return output;
        }
    }
}
