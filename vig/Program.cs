using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vig
{
    internal class Program
    {

        // Default settings
        static int metalOffset = 30;
        static double glossMult = 1.0;
        static int blurRadius = 3;
        static double contrast = 0.5;
        static double metalMult = 7.6; // the lower this value is, the more transparent the _cm files are
        static int thumbW = 164, thumbH = 12;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: vig.exe <image.png> [--metal 30] [--gloss 1.0] [--blur 3] [--contrast 0.5] [--thumb 164x12]");
                return;
            }

            string input = args[0];
            if (!File.Exists(input))
            {
                Console.WriteLine("❌ File not found: " + input);
                return;
            }


            // Parse args
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--metal": metalOffset = int.Parse(args[++i]); break;
                    case "--metalmult": metalMult = double.Parse(args[++i]); break;
                    case "--gloss": glossMult = double.Parse(args[++i]); break;
                    case "--blur": blurRadius = int.Parse(args[++i]); break;
                    case "--contrast": contrast = double.Parse(args[++i]); break;
                    case "--thumb":
                        string[] parts = args[++i].Split('x');
                        if (parts.Length == 2) { thumbW = int.Parse(parts[0]); thumbH = int.Parse(parts[1]); }
                        break;
                }
            }

            string baseName = Path.GetFileNameWithoutExtension(input);
            string outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated");
            Directory.CreateDirectory(outDir);

            Console.WriteLine($"🎨 Input: {input}");
            Console.WriteLine($"⚙️  Metal +{metalOffset}, Gloss×{glossMult}, Blur={blurRadius}, Contrast={contrast}, Thumb={thumbW}x{thumbH}");

            using (Bitmap src = ResizeToStandard(new Bitmap(input)))
            {
                string fileName = string.Empty;

                fileName = Path.Combine(outDir, baseName + "_AxisXZ_cm.png");
                Save(src, fileName, FileType.CM);

                fileName = Path.Combine(outDir, baseName + "_AxisY_cm.png");
                Save(src, fileName, FileType.CM);

                fileName = Path.Combine(outDir, baseName + "_AxisXZ_ng.png");
                Save(src, fileName, FileType.NG);

                fileName = Path.Combine(outDir, baseName + "_AxisXZ_add.png");
                Save(src , fileName, FileType.ADD);

                fileName = Path.Combine(outDir, baseName + "_AxisXZ_distance.png");
                Save(src, fileName, FileType.DISTANCE);

                fileName = Path.Combine(outDir, baseName + "_thumbnail.png");
                Save(src,fileName, FileType.THUMB);
            }

            Console.WriteLine($"✅  All PNGs written to: {Path.GetFullPath(outDir)}");
        }
        public static void Save(Bitmap source, string fileName, FileType fileType)
        {
            Bitmap result = null;
            Console.WriteLine("Writing {0}", fileName);
            switch (fileType)
            {
                case FileType.ADD:
                    result = MakeCavity(source, blurRadius);
                    break;
                case FileType.NG:
                    result = MakeNormalGloss(source, glossMult);
                    break;
                case FileType.CM:
                    result = MakeColorMetal(source, metalOffset, metalMult);
                    break;
                case FileType.DISTANCE:
                    result = MakeDistance(source, blurRadius * 4, contrast);
                    break;
                case FileType.THUMB:
                    result = new Bitmap(source, thumbW, thumbH);
                    break;
            }
            result.Save(fileName, ImageFormat.Png);
            Console.Write(" done.", fileName);
        }
        // ---- _cm ----
        static Bitmap MakeColorMetal(Bitmap src, int metalOffset, double metalMult)
        {
            // Copy original RGB untouched
            Bitmap result = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, 0, 0, src.Width, src.Height);
            }

            Rectangle rect = new Rectangle(0, 0, result.Width, result.Height);
            BitmapData data = result.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = data.Stride;
            int bytes = Math.Abs(stride) * result.Height;
            byte[] buffer = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, bytes);

            // Compute toned-down metalness alpha
            for (int y = 0; y < result.Height; y++)
            {
                int offset = y * stride;
                for (int x = 0; x < result.Width; x++)
                {
                    int idx = offset + x * 4;
                    byte b = buffer[idx];
                    byte gVal = buffer[idx + 1];
                    byte r = buffer[idx + 2];
                    int gray = (int)(0.3 * r + 0.59 * gVal + 0.11 * b);

                    int alpha = (int)Math.Min(255, (gray * metalMult) + metalOffset);
                    buffer[idx + 3] = (byte)alpha;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, bytes);
            result.UnlockBits(data);
            return result;
        }


        /*
        static Bitmap MakeColorMetal(Bitmap src, int metalOffset)
        {
            // Copy original RGB as-is
            Bitmap result = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(result))
                g.DrawImage(src, 0, 0, src.Width, src.Height);

            Rectangle rect = new Rectangle(0, 0, result.Width, result.Height);
            BitmapData data = result.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = data.Stride;
            int bytes = Math.Abs(stride) * result.Height;
            byte[] buffer = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, bytes);

            // Write metalness into alpha channel only (leave RGB untouched)
            // this is still a bit of a problem
            for (int y = 0; y < result.Height; y++)
            {
                int offset = y * stride;
                for (int x = 0; x < result.Width; x++)
                {
                    int idx = offset + x * 4;
                    byte b = buffer[idx];
                    byte gVal = buffer[idx + 1];
                    byte r = buffer[idx + 2];
                    int gray = (int)(0.3 * r + 0.59 * gVal + 0.11 * b);
                    buffer[idx + 3] = (byte)Math.Min(255, gray + metalOffset); // alpha only
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, bytes);
            result.UnlockBits(data);

            return result;
        }
        */
        // ---- _ng ----
        static Bitmap MakeNormalGloss(Bitmap src, double glossMult)
        {
            Bitmap gray = Desaturate(src);
            Bitmap normal = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);

            int[,] gx = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            int[,] gy = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

            for (int y = 1; y < src.Height - 1; y++)
                for (int x = 1; x < src.Width - 1; x++)
                {
                    int dx = 0, dy = 0;
                    for (int j = -1; j <= 1; j++)
                        for (int i = -1; i <= 1; i++)
                        {
                            int val = gray.GetPixel(x + i, y + j).R;
                            dx += gx[j + 1, i + 1] * val;
                            dy += gy[j + 1, i + 1] * val;
                        }
                    float nx = -dx / 255f;
                    float ny = -dy / 255f;
                    float nz = 1f;

                    float len = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                    nx /= len; ny /= len; nz /= len;

                    byte r = (byte)((nx * 0.5f + 0.5f) * 255);
                    byte g = (byte)((ny * 0.5f + 0.5f) * 255);
                    byte b = (byte)((nz * 0.5f + 0.5f) * 255);

                    int gloss = Math.Min(255, (int)(gray.GetPixel(x, y).R * glossMult));
                    normal.SetPixel(x, y, Color.FromArgb(gloss, r, g, b));
                }
            gray.Dispose();
            return normal;
        }

        // ---- _add ----
        static Bitmap MakeCavity(Bitmap src, int radius)
        {
            Bitmap gray = Desaturate(src);
            Bitmap inv = Invert(gray);
            Bitmap blur = GaussianBlur(inv, radius);
            gray.Dispose(); inv.Dispose();
            return blur;
        }

        // ---- _distance ----
        static Bitmap MakeDistance(Bitmap src, int radius, double contrast)
        {
            // Step 1: downsample (cheap blur)
            int smallW = src.Width / 4;
            int smallH = src.Height / 4;
            using (Bitmap small = new Bitmap(smallW, smallH))
            using (Graphics gSmall = Graphics.FromImage(small))
            {
                gSmall.InterpolationMode = InterpolationMode.HighQualityBilinear;
                gSmall.DrawImage(src, 0, 0, smallW, smallH);

                // Step 2: upscale back (smooth)
                Bitmap blurred = new Bitmap(src.Width, src.Height);
                using (Graphics g = Graphics.FromImage(blurred))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(small, 0, 0, src.Width, src.Height);
                }

                // Step 3: mild contrast reduction
                for (int y = 0; y < blurred.Height; y++)
                    for (int x = 0; x < blurred.Width; x++)
                    {
                        Color c = blurred.GetPixel(x, y);
                        int mid = 128;
                        int r = mid + (int)((c.R - mid) * contrast);
                        int g = mid + (int)((c.G - mid) * contrast);
                        int b = mid + (int)((c.B - mid) * contrast);
                        blurred.SetPixel(x, y, Color.FromArgb(255, r, g, b));
                    }

                return blurred;
            }
        }
        static Bitmap Desaturate(Bitmap src)
        {
            Bitmap gray = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    int g = (int)(0.3 * c.R + 0.59 * c.G + 0.11 * c.B);
                    gray.SetPixel(x, y, Color.FromArgb(g, g, g));
                }
            return gray;
        }

        static Bitmap Invert(Bitmap src)
        {
            Bitmap res = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    int v = 255 - src.GetPixel(x, y).R;
                    res.SetPixel(x, y, Color.FromArgb(v, v, v));
                }
            return res;
        }

        static Bitmap GaussianBlur(Bitmap image, int radius)
        {
            if (radius < 1) return (Bitmap)image.Clone();
            int w = image.Width, h = image.Height;
            Bitmap blurred = new Bitmap(w, h);

            double sigma = radius / 2.0;
            int size = radius * 2 + 1;
            double[,] kernel = new double[size, size];
            double sum = 0;
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                {
                    double v = Math.Exp(-(x * x + y * y) / (2 * sigma * sigma));
                    kernel[y + radius, x + radius] = v;
                    sum += v;
                }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    kernel[y, x] /= sum;

            for (int y = radius; y < h - radius; y++)
                for (int x = radius; x < w - radius; x++)
                {
                    double r = 0, g = 0, b = 0;
                    for (int j = -radius; j <= radius; j++)
                        for (int i = -radius; i <= radius; i++)
                        {
                            Color c = image.GetPixel(x + i, y + j);
                            double k = kernel[j + radius, i + radius];
                            r += c.R * k; g += c.G * k; b += c.B * k;
                        }
                    blurred.SetPixel(x, y, Color.FromArgb(255, Clamp(r), Clamp(g), Clamp(b)));
                }
            return blurred;
        }
        static Bitmap ResizeToStandard(Bitmap input, int width = 2048, int height = 2048)
        {
            // Create a new 32-bit ARGB bitmap at target size
            Bitmap resized = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(input, 0, 0, width, height);
            }

            return resized;
        }

        static int Clamp(double v) => v < 0 ? 0 : v > 255 ? 255 : (int)v;
    }
}
public enum FileType
{
    NONE,
    CM,
    NG,
    ADD,
    DISTANCE,
    THUMB
}