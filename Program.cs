using CabbageBot.Commands;
using CabbageBot.Tools.WowUpdateChecker;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CabbageBot
{
    public class Program
    {
        public DiscordClient Client { get; set; }
        public CommandsNextExtension Commands { get; set; }
        public InteractivityExtension Interactivity { get; set; }
        public VoiceNextExtension Voice { get; set; }

        public static void Main(string[] args)
        {
            // dot dot dot
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            //init WowUpdateChecker bot
            var WowBot = new UpdateChecker();
            Thread wowUpdateThread = new Thread(UpdateChecker.Instance.start);
            wowUpdateThread.Start();

            //execute async
            var prog = new Program();
            prog.RunBotAsync().GetAwaiter().GetResult();
        }

        public async Task RunBotAsync()
        {
            //Configs
            var json = "";
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            var cfgjson = JsonConvert.DeserializeObject<ConfigJson>(json);
            var cfg = new DiscordConfiguration
            {
                Token = cfgjson.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            //init
            this.Client = new DiscordClient(cfg);

            //init WowUpdateChecker
            UpdateChecker.Instance.Client = this.Client;

            //handlers
            this.Client.Ready += Client_Ready;
            this.Client.GuildAvailable += Client_GuildAvailable;
            this.Client.ClientErrored += Client_ClientError;

            //interactivity
            this.Client.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.Ignore,
                //PaginationTimeout = TimeSpan.FromMinutes(5),
                Timeout = TimeSpan.FromMinutes(2) //2min timeout
            });

            //command settings
            var ccfg = new CommandsNextConfiguration
            {

                StringPrefixes = new[] { cfgjson.CommandPrefix },
                EnableDms = true,   //DM's allowed?? might be handy for privacy.. but we cannot check roles....
                EnableMentionPrefix = true
            };

            this.Commands = this.Client.UseCommandsNext(ccfg);

            this.Commands.CommandExecuted += Commands_CommandExecuted;
            this.Commands.CommandErrored += Commands_CommandErrored;

            //Command classes
#if ISFERIB
            this.Commands.RegisterCommands<DeLijnCommands>();
            this.Commands.RegisterCommands<LidlePlusCommands>(); //Try not to get sued here ;)
#endif
            this.Commands.RegisterCommands<BitcoinCommands>();
            this.Commands.RegisterCommands<WowCommands>();

            this.Commands.RegisterCommands<VoiceCommands>(); //NOTE: Q-music comming soon?


            this.Voice = this.Client.UseVoiceNext();

            //connect!
            await this.Client.ConnectAsync();

            //inf sleep to avoid exit
            await Task.Delay(-1);
        }

        private Task Client_Ready(ReadyEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "CabbageBot", "Client is ready to process events.", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "CabbageBot", $"Guild available: {e.Guild.Name}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_ClientError(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "CabbageBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "CabbageBot", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);
            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            //error handeling
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "CabbageBot", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            if (e.Exception is ChecksFailedException ex)
            {
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000) //RGB Red
                };
                await e.Context.RespondAsync("", embed: embed);
            }
        }
    }

    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }
    }
}