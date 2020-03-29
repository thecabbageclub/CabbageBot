using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using CababgeBot.Tools.Qmusic;
using System.Net.Sockets;
using System.Threading;

namespace CabbageBot.Commands
{

    [Group("qmusic")]
    //[Group("qmusic", CanInvokeWithoutSubcommand = true)]
    [Description("Qmusic tool")]
    public class VoiceCommands : BaseCommandModule
    {
        private static DateTime LastStreamsListUpdate = DateTime.MinValue;
        private static Dictionary<ulong, int> ChannelStreamSettings = new Dictionary<ulong, int>();
        private static Dictionary<ulong, bool> ChannelStreamPlaying = new Dictionary<ulong, bool>();

        public async Task ExecuteGroupAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("The following sub commands are available: ``list``, ``channel``, ``join``, ``leave``, ``play``");
        }


        [Command("list"), Description("Lists all available radio streams")]
        public async Task List(CommandContext ctx)
        {
            if (Qmusic.Instance == null)
                new Qmusic();

            if(LastStreamsListUpdate.AddHours(1) < DateTime.UtcNow)
                Qmusic.Instance.GetStreamsList();

            string listString = "";

            for(int i = 0; i < Qmusic.Instance.channels.Count; i++)
            {
                var split = Qmusic.Instance.channels[i].source.Split('/');
                listString += $"{(i+1).ToString("00")} - {split[split.Length - 1].Replace("AAC.aac","").ToLower()}\n";
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = "QMusic Channel List",
                Description = listString,
                Color = DiscordColor.Red
            };
            await ctx.RespondAsync(null, false, embed);
        }

        [Command("channel"), Description("User to select a radio channel")]
        public async Task Channel(CommandContext ctx, int index = -1)
        {
            if (index == -1)
            {
                await ctx.RespondAsync("Please enter a channel ID from the ``list`` command");
                return;
            }

            index -= 1; //user gets a +1 view, cuz normal ppl start counting from 1 (instead of 0)

            if (Qmusic.Instance == null)
                new Qmusic();

            if (index > -1 && index < Qmusic.Instance.channels.Count)
            {
                if (ChannelStreamSettings.ContainsKey(ctx.Guild.Id))
                    ChannelStreamSettings[ctx.Guild.Id] = index;
                else
                    ChannelStreamSettings.Add(ctx.Guild.Id, index);

                var split = Qmusic.Instance.channels[index].source.Split('/');
                await ctx.RespondAsync($"Channel change to ``{split[split.Length - 1].Replace("AAC.aac", "").ToLower()}``");

                //call play
                if (ChannelStreamPlaying.ContainsKey(ctx.Guild.Id))
                {
                    if (ChannelStreamPlaying[ctx.Guild.Id])
                    {
                        ChannelStreamPlaying[ctx.Guild.Id] = false; //interrupt old one
                        Play(ctx);
                    }
                }
            }
            else
            {
                await ctx.RespondAsync($"Invalid channel");
            }
        }

        [Command("join"), Description("Joins a voice channel.")]
        public async Task Join(CommandContext ctx, DiscordChannel chn = null)
        {
            // check whether VNext is enabled
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }

            // check whether we aren't already connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc != null)
            {
                // already connected
                await ctx.RespondAsync("Already connected in this guild.");
                return;
            }

            // get member's voice state
            var vstat = ctx.Member?.VoiceState;
            if (vstat?.Channel == null && chn == null)
            {
                // they did not specify a channel and are not in one
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            // channel not specified, use user's
            if (chn == null)
                chn = vstat.Channel;

            // connect
            vnc = await vnext.ConnectAsync(chn);
            await ctx.RespondAsync($"Connected to `{chn.Name}`");
        }

        [Command("leave"), Description("Leaves a voice channel.")]
        public async Task Leave(CommandContext ctx)
        {
            // check whether VNext is enabled
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }

            // check whether we are connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                // not connected
                await ctx.RespondAsync("Not connected in this guild.");
                return;
            }

            if (ChannelStreamPlaying.ContainsKey(ctx.Guild.Id))
                ChannelStreamPlaying[ctx.Guild.Id] = false;

            // disconnect
            vnc.Disconnect();
            await ctx.RespondAsync("Disconnected");
        }

        [Command("play"), Description("Plays Qmusic radio")]
        public async Task Play(CommandContext ctx)
        {
            // check whether VNext is enabled
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }

            // check whether we aren't already connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                // already connected
                await ctx.RespondAsync("Not connected in this guild.");
                return;
            }


            if (!ChannelStreamPlaying.ContainsKey(ctx.Guild.Id))
                ChannelStreamPlaying.Add(ctx.Guild.Id, false);
            else if (ChannelStreamPlaying[ctx.Guild.Id])
            {
                Thread.Sleep(1000); //Sleep 1 sec to make sure other thread exits?
                ChannelStreamPlaying[ctx.Guild.Id] = false;
            }
                

            //

            // play
            Exception exc = null;

            try
            {
                await vnc.SendSpeakingAsync(true);

                var txStream = vnc.GetTransmitStream();

                if (Qmusic.Instance == null)
                    new Qmusic();

                if (!ChannelStreamPlaying.ContainsKey(ctx.Guild.Id))
                    ChannelStreamPlaying.Add(ctx.Guild.Id, true);
                else
                    ChannelStreamPlaying[ctx.Guild.Id] = true;

                string url = "";

                if (ChannelStreamSettings.ContainsKey(ctx.Guild.Id))
                    url = Qmusic.Instance.GetStreamURL(ChannelStreamSettings[ctx.Guild.Id]);
                else
                    url = Qmusic.Instance.GetStreamURL();

                var isPlaying = ChannelStreamPlaying[ctx.Guild.Id];
                await Qmusic.Instance.ReadMusicStream(url, ref ChannelStreamPlaying, ref txStream, ctx.Guild.Id);

            }
            catch (Exception ex) { exc = ex; }
            finally
            {
                await vnc.SendSpeakingAsync(false);
            }

            if (exc != null)
                await ctx.RespondAsync($"An exception occured during playback: `{exc.GetType()}: {exc.Message}`");
        }
    }
}
