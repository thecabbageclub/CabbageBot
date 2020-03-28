using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Linq;
using System.Threading;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;

namespace CabbageBot.Tools.WowUpdateChecker
{
    class UpdateChecker
    {
        private static string BattleNetUpdateURL = "http://us.patch.battle.net:1119/wow/versions";

        public static UpdateChecker Instance { get; set; }
        public bool isRunning { get; set; }
        public List<ulong> SubscriberUsers { get; set; }
        public DiscordClient Client { get; set; }

        private DateTime LastCheck { get; set; }
        private VersionFile LastUSVersion { get; set; } //US usualy gets updated before EU does


        public UpdateChecker()
        {
            Instance = this;
            this.isRunning = true;
            this.LastCheck = DateTime.MinValue;
            this.SubscriberUsers = new List<ulong>();
        }

        public void start()
        {
            while(this.isRunning)
            {
                if(LastCheck.AddMinutes(5) < DateTime.UtcNow)
                {
                    this.LastCheck = DateTime.UtcNow;
                    var newVersions = GetCurrentVersion();
                    if (LastUSVersion == null)
                        this.LastUSVersion = newVersions.Find(x => x.Location == "us");

                    if (LastUSVersion.BuildNumber != newVersions.Find(x => x.Location == "us").BuildNumber)
                    {
                        //change detection (update or might even a downgrade, this happends when blizzard fucks $#!@ up
                        this.LastUSVersion = newVersions.Find(x => x.Location == "us");
                        foreach (var user in this.SubscriberUsers)
                        {
                            //var newDM = this.Client.CreateDmAsync(this.Client.GetUserAsync(user).GetAwaiter().GetResult()).GetAwaiter().GetResult();
                            //newDM.SendMessageAsync($"World of Warcraft (Retail) has been updated to {this.LastUSVersion.Version} ({this.LastUSVersion.BuildNumber})");
                        }
                    }
                }
                Thread.Sleep(LastCheck.AddMinutes(5) - DateTime.UtcNow);
            }
        }

        private List<VersionFile> GetCurrentVersion()
        {
            WebClient client = new WebClient();
            string VersionContent = client.DownloadString(BattleNetUpdateURL);
            string VersionContentLine = "";
            List<VersionFile> FileVersionList = new List<VersionFile>();
            VersionFile CurrentVersion = new VersionFile();

            //Region!STRING:0|BuildConfig!HEX:16|CDNConfig!HEX:16|KeyRing!HEX:16|BuildId!DEC:4|VersionsName!String:0|ProductConfig!HEX:16

            for (int i = 2; i < VersionContent.Split('\n').Count(); i++) //Skip first 2 lines
            {
                CurrentVersion = new VersionFile();
                VersionContentLine = VersionContent.Split('\n')[i];
                if (VersionContentLine.Count() < 1)
                    break;
                CurrentVersion.Location = VersionContentLine.Split('|')[0];
                CurrentVersion.hash1 = VersionContentLine.Split('|')[1];
                CurrentVersion.hash2 = VersionContentLine.Split('|')[2];
                CurrentVersion.hash3 = VersionContentLine.Split('|')[6];
                CurrentVersion.BuildNumber = VersionContentLine.Split('|')[4];
                CurrentVersion.Version = VersionContentLine.Split('|')[5];
                FileVersionList.Add(CurrentVersion);
            }
            return FileVersionList;
        }

    }


    class VersionFile
    {
        public string Location { get; set; }
        public string hash1 { get; set; }
        public string hash2 { get; set; }
        public string hash3 { get; set; }
        public string BuildNumber { get; set; }
        public string Version { get; set; }
    }
 }
