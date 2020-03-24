using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using CabbageBot.Tools.WowUpdateChecker;

namespace CabbageBot.Commands
{
    [Group("wow", CanInvokeWithoutSubcommand = true)]
    [Description("wow tool")]
    public class WowCommands
    {
        public async Task ExecuteGroupAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("The following sub commands are available: ``updates``");
        }

        [Command("updates"), Description("Subscribe to Wow updates")]
        public async Task Unlock(CommandContext ctx)
        {
            ulong userid = 0;
            if (ctx.Member != null)
                userid = ctx.Member.Id;
            else
                userid = ctx.User.Id;

            //first retrieve the interactivity module from the client
            var interactivity = ctx.Client.GetInteractivityModule();

            //specify the emoji
            var accept = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            var reject = DiscordEmoji.FromName(ctx.Client, ":x:");

            //announce
            var requestMsg = await ctx.RespondAsync($"Would you like to get notified whenever World of Warcraft (Retail) gets updated?");
            await Task.Delay(100);
            await requestMsg.CreateReactionAsync(accept); //check
            await Task.Delay(50);
            await requestMsg.CreateReactionAsync(reject); //cross
            await Task.Delay(50);

            var em = await interactivity.WaitForReactionAsync(xe => xe == accept || xe == reject, ctx.User, TimeSpan.FromSeconds(10));
            if (em != null)
            {
                if (UpdateChecker.Instance == null)
                    new UpdateChecker();
                //user reacted
                if (em.Emoji == accept)
                {
                    await ctx.RespondAsync("Subscribtion activated!");
                    if (UpdateChecker.Instance.SubscriberUsers.FindIndex(x => x == userid) == -1)
                        UpdateChecker.Instance.SubscriberUsers.Add(userid);
                }else
                {
                    var uIndex = UpdateChecker.Instance.SubscriberUsers.FindIndex(x => x == userid);
                    if (uIndex > -1)
                    {
                        UpdateChecker.Instance.SubscriberUsers.RemoveAt(uIndex);
                        await ctx.RespondAsync("Subscribtion removed!");
                    }
                    else
                    {
                        //nothing?
                    }
                        
                }
                await requestMsg.DeleteAsync();
            }
            else
            {
                //timeout
                await requestMsg.DeleteAsync();
            }
        }
    }
}