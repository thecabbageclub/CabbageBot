using CababgeBot.Tools.Qmusic;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CabbageBot.Commands
{

    [Group("qmusic")]
    //[Group("qmusic", CanInvokeWithoutSubcommand = true)]
    [Description("Qmusic tool")]
    public class VoiceCommands : BaseCommandModule
    {
        private static readonly Dictionary<ulong, int> ChannelStreamSettings = new Dictionary<ulong, int>();

        public async Task ExecuteGroupAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("The following sub commands are available: ``list``, ``channel``, ``join``, ``leave``, ``play``");
        }


        [Command("list"), Description("Lists all available radio streams")]
        public async Task List(CommandContext ctx)
        {
            string listString = "";

            for (int i = 0; i < Qmusic.QmusicRadioChannels.Count; i++)
            {
                var split = Qmusic.QmusicRadioChannels[i].data.streams.aac.FirstOrDefault().source.Split('/');
                listString += $"{(i + 1).ToString("00")} - {split[split.Length - 1].Replace("AAC.aac", "").ToLower()}\n";
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


            if (index > -1 && index < Qmusic.QmusicRadioChannels.Count)
            {
                if (ChannelStreamSettings.ContainsKey(ctx.Guild.Id))
                    ChannelStreamSettings[ctx.Guild.Id] = index;
                else
                    ChannelStreamSettings.Add(ctx.Guild.Id, index);

                var split = Qmusic.QmusicRadioChannels[index].data.streams.aac.FirstOrDefault().source.Split('/');
                await ctx.RespondAsync($"Channel change to ``{split[split.Length - 1].Replace("AAC.aac", "").ToLower()}``");

                if (Qmusic.TryGetInstance(ctx.Guild.Id, out Qmusic instance)) // if instance is found
                {
                    await ctx.RespondAsync($"Waiting for playback to finish...");
                    instance.CancelPlayback();
                    await Play(ctx);
                    await ctx.RespondAsync($"Switched!");
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

            if (!Qmusic.TryGetInstance(ctx.Guild.Id, out Qmusic qInstance))
            {
                qInstance = new Qmusic(chn, ctx.Client);
            }

            await qInstance.JoinChannel(chn);
            await ctx.RespondAsync($"Connected to `{chn.Name}`");
        }

        [Command("info"), Description("Get info about current track")]
        public async Task Info(CommandContext ctx)
        {
            if (Qmusic.TryGetInstance(ctx.Guild.Id, out Qmusic instance))
            {
                try
                {
                    //TODO: fix all of this mess xD
                    var resp = instance.GetTrackInfo(instance.QChannelIndex);
                    var embed = new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Title = resp.played_tracks[0].title,
                        Description = resp.played_tracks[0].artist.name + "\n" + resp.played_tracks[0].spotify_url,
                        ImageUrl = "https://api.qmusic.be/" + resp.played_tracks[0].thumbnail
                    };
                    await ctx.RespondAsync(embed: embed);
                }
                catch (Exception e)
                {
                    await ctx.RespondAsync("Failed obtaining info");
                }
                
            }
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

            if (Qmusic.TryGetInstance(ctx.Guild.Id, out Qmusic qInstance))
            {
                await ctx.RespondAsync("Waiting for playback to finish...");
                await qInstance.CancelAndFinishPlayback();
            }

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


            //

            // play
            Exception exc = null;

            try
            {
                await vnc.SendSpeakingAsync(true);

                var txStream = vnc.GetTransmitStream();

                if (!Qmusic.TryGetInstance(ctx.Guild.Id, out Qmusic qInstance)) // if no instance exists
                {
                    qInstance = new Qmusic(vnc.Channel, ctx.Client);
                }
                else if (qInstance.Channel.Id != ctx.Member.VoiceState.Channel.Id) // if an instance exists AND the channel differs
                {
                    await ctx.RespondAsync("Error: Already in a different channel!");
                    return;
                }

                if (ChannelStreamSettings.ContainsKey(ctx.Guild.Id))
                    await qInstance.SetQMusicChannelIndex(ChannelStreamSettings[ctx.Guild.Id]);
                else
                    await qInstance.SetQMusicChannelIndex(-1);

                await qInstance.Play(vnc.Channel);

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
