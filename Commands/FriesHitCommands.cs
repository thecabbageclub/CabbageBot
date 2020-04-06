using CabbageBot.Tools.McDonald.FriesHit;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CabbageBot.Commands
{

    [Group("frieshit")]
    //[Group("qmusic", CanInvokeWithoutSubcommand = true)]
    [Description("FriesHit")]
    public class FriesHitCommands : BaseCommandModule
    {
        public async Task ExecuteGroupAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("The following sub commands are available: ``leaderboard``");
        }


        [Command("leaderboard"), Aliases("topscore,lead,top"), Description("Shows leaderboard")]
        public async Task List(CommandContext ctx)
        {
            if (FriesHit.Instance == null)
                new FriesHit();

            string listString = "";

            var leaderboard = FriesHit.Instance.getLeaderbordList(10);

            for (int i = 0; i < leaderboard.topScoreData.Count; i++)
            {
                listString += $"{leaderboard.topScoreData[i].score.PadLeft(6, ' ')} - {leaderboard.topScoreData[i].firstName} .{leaderboard.topScoreData[i].lastName} - {(i + 1).ToString("00")}\n";
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = "FriesHit Leaderboard",
                Description = listString,
                Color = DiscordColor.Yellow
            };
            await ctx.RespondAsync(null, false, embed);
        }
    }
}
