using Discord;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using ResellerBot;
using Serilog;
using Serilog.Events;


class Program
{

    private readonly IServiceProvider _services;
    private readonly DiscordSocketConfig _socketConfig = new()
    {
        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent,
        AlwaysDownloadUsers = true
    };
    private static DiscordWebhookClient _webhook;

    static void Main(string[] args)
    {
        if (ConfigHelper.Get() == null)
        {
            string token = "";
            bool finished = false;
            do
            {
                Console.Write("Enter your bot token: ");
                token = Console.ReadLine();
            } while (string.IsNullOrEmpty(token));

            var config = new Config
            {
                Token = token,
            };

            do
            {
                Console.Write("Separate with `,`\nAdmins Id('s): ");
                var admins = Console.ReadLine();

                if (!string.IsNullOrEmpty(admins))
                {

                    if (!admins.Contains(','))
                    {
                        if (ulong.TryParse(admins, out var id))
                        {
                            config.Administrators.Add(id);
                            finished = true;
                        }

                    }
                    else
                    {
                        var adminsList = admins.Split(',').ToList();
                        foreach (var admin in adminsList)
                        {
                            if (ulong.TryParse(admin, out var id))
                            {
                                config.Administrators.Add(id);
                            }
                        }

                        finished = true;
                    }


                }

            } while (!finished);
            config.Save();
        }

        new Program().RunAsync().GetAwaiter().GetResult();
    }

    public Program()
    {
        _services = new ServiceCollection()
            .AddSingleton(_socketConfig)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<CommandHandler>()
            .BuildServiceProvider();
    }

    public static async Task LogCommand(string msg)
    {
        var config = ConfigHelper.Get()!;
        if (string.IsNullOrEmpty(config.Webhook)) return;
        if (_webhook == null)
        {
            _webhook = new DiscordWebhookClient(config.Webhook);
        }
        var embed = new EmbedBuilder()
            .WithColor(Color.DarkBlue)
            .WithDescription(msg)
            .WithCurrentTimestamp()
            .Build();

        await _webhook.SendMessageAsync(embeds: new[] { embed });
    }

    public async Task RunAsync()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        var config = ConfigHelper.Get()!;

        var client = _services.GetRequiredService<DiscordSocketClient>();

        client.Log += LogAsync;

        // Here we can initialize the service that will register and execute our commands
        await _services.GetRequiredService<CommandHandler>()
            .InitializeAsync();

        // Bot token can be provided from the Configuration object we set up earlier
        await client.LoginAsync(TokenType.Bot, config.Token);
        await client.StartAsync();

        await client.SetActivityAsync(new Game(".gg/chasefn", type: ActivityType.Watching));

        // Never quit the program until manually forced to.
        await Task.Delay(Timeout.Infinite);
    }

    public static async Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };

        Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
    }
}

