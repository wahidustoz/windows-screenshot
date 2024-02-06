using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;


internal class Program
{
    private static void Main(string[] args)
    {
#if WINDOWS

        var input = GetInputFromArgs(args);
    
        Timer timer = new() { Interval = input.Interval };
        timer.Elapsed += async (a, b) => await Task.Run(async () => 
        {
            if(NetworkInterface.GetIsNetworkAvailable() is false)
            {
                Console.WriteLine("We dont have connection, skipping for next round...");
            }
            else
            {
                try 
                {
                    var ip = await GetIpOrDefaultAsync();
                    Rectangle screenBounds = GetPrimaryScreenBounds();
                    using Bitmap screenshot = new(screenBounds.Width, screenBounds.Height);

                    using var graphics = Graphics.FromImage(screenshot);
                    graphics.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);

                    using var ms = new MemoryStream();
                    screenshot.Save(ms, ImageFormat.Png);
                    byte[] imageBytes = ms.ToArray();
                    var base64Image = Convert.ToBase64String(imageBytes);

                    string hostName = Dns.GetHostName();

                    var payload = new StringContent(JsonSerializer.Serialize(new
                    {
                        image = base64Image,
                        hostName,
                        ip
                    }));
                    
                    using var client = new HttpClient();
                    byte[] payloadBytes = Encoding.UTF8.GetBytes(base64Image);
                    string url = input.Destination;
                    HttpResponseMessage response = await client.PostAsync(url, payload);
                    Console.WriteLine($"Response: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error: {JsonSerializer.Serialize(ex)}");
                }
            }
        });
        timer.Start();

        while(true);
#endif
    }

    static (int Interval, string Destination) GetInputFromArgs(string[] args)
    {
        var destinationUrl = args.FirstOrDefault(a => a.StartsWith("--destination=")) 
            ?? throw new Exception("You must provide a destination url with --destination option");

        var intervalString = args.FirstOrDefault(a => a.StartsWith("--interval="));
        int.TryParse(intervalString.Replace("--interval=", ""), out int interval);

        return (interval > 0 ? interval * 1000 : 3600 * 1000, destinationUrl.Replace("--destination=", ""));
    }

    static async Task<string> GetIpOrDefaultAsync()
    {
        var ipResponse = await new HttpClient().GetStringAsync("http://ipinfo.io/ip");
        
        if(IPAddress.TryParse(ipResponse, out IPAddress ip))
            return ip.ToString();

        return default;
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
