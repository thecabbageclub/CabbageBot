// 
// The code required to run these commands are private :/
// 

#if ISFERIB
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using CabbageBot.Config;
using LidlPlusReversed;

namespace CabbageBot.Commands
{
    [Group("lidl", CanInvokeWithoutSubcommand = true)]
    [Description("Lidl tool")]
    public class LidlePlusCommands
    {
        //Timeout Text Messages (SMS)
        private static Dictionary<ulong, DateTime> SmsTimeoutDict = new Dictionary<ulong, DateTime>();
        private static TimeSpan SmsTimeout = config.LidlPlusSMSTimeout;


        public async Task ExecuteGroupAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("The following sub commands are available: ``sms``");
        }

        [Command("sms"), Aliases("text"), Description("Sends a LidlePlus text message to a given phone number")]
        public async Task Sms(CommandContext ctx, string phonenr)
        {
            long longnr = 0;

            await ctx.TriggerTypingAsync();

            ulong userid = 0;

            if (ctx.Member != null)
                userid = ctx.Member.Id;
            else if (ctx.User != null)
                userid = ctx.User.Id;

            if (SmsTimeoutDict.ContainsKey(userid) && SmsTimeoutDict[userid].Add(SmsTimeout) > DateTime.UtcNow)
            {
                await ctx.RespondAsync($"Calm down <@{userid}>, Please try again in ``{((SmsTimeoutDict[userid].Add(SmsTimeout)) - DateTime.UtcNow)}``");
                return;
            }

            if(LidlPlus.Instance == null)
                new LidlPlus();

            if(!long.TryParse(phonenr.Replace("+",""), out longnr))
            {
                await ctx.RespondAsync($"Text message faild");
                return;
            }

            if (SmsTimeoutDict.ContainsKey(userid))
                SmsTimeoutDict.Remove(userid);

            SmsTimeoutDict.Add(userid, DateTime.UtcNow);

            LidlPlus.Instance.RequestPhoneCode(longnr);

            await ctx.RespondAsync($"Text message sent to {phonenr}!");
        }
    }
}
#endif