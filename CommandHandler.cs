using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;


namespace ResellerBot;

public class CommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _handler;
    private readonly IServiceProvider _services;


    public CommandHandler(DiscordSocketClient client, IServiceProvider services, InteractionService handler)
    {
        _client = client;
        _services = services;
        _handler = handler;
        client.ChannelCreated += ChannelCreated;
    }

    public async Task InitializeAsync()
    {
        // Process when the client is ready, so we can register our commands.
        _client.Ready += ReadyAsync;
        _handler.Log += Program.LogAsync;

        // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
        await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        // Process the InteractionCreated payloads to execute Interactions commands
        _client.InteractionCreated += HandleInteraction;
    }


    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
            var context = new SocketInteractionContext(_client, interaction);

            // Execute the incoming command.
            var result = await _handler.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        if (interaction.HasResponded)
                        {
                            await interaction.FollowupAsync(result.ErrorReason, ephemeral: true);
                        }
                        else
                        {
                            await interaction.RespondAsync(result.ErrorReason, ephemeral: true);
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        catch
        {
            // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
            // response, or at least let the user know that something went wrong during the command execution.
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync()
                    .ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }

    private async Task ReadyAsync()
    {
        await _handler.RegisterCommandsGloballyAsync();
    }


    private async Task ChannelCreated(SocketChannel channel)
    {
        if (channel is SocketTextChannel textChannel)
        {
            ulong categoryIdToDetect = 1163983820245180446; // Replace with the desired category ID

            if (textChannel.CategoryId == categoryIdToDetect)
            {

                DiscordButtonComponent button1 = new DiscordButtonComponent(ButtonStyle.Primary, "1", "Button 1");
                DiscordButtonComponent button2 = new DiscordButtonComponent(ButtonStyle.Primary, "2", "Button 2");

                //Make the MessageBuilder and add on the buttons
                //A Message can have up to 5 x 5 worth of buttons. Thats 5 rows, each with 5 buttons
                var message = new DiscordMessageBuilder()
                    .AddEmbed (new DiscordEmbedBuilder())
                        .WithColor(Color.DarkBlue)
                        .WithTitle("This is a message with buttons")
                        .WithDescription("Please select a button")
                    .AddComponents(button1, button2);

                await textChannel.SendMessageAsync();
            }
        }
    }
}