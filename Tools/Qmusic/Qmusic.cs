using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CababgeBot.Tools.Qmusic
{
    class Qmusic
    {
        //public static Qmusic Instance { get; set; }
        public static Dictionary<ulong, Qmusic> Instances = new Dictionary<ulong, Qmusic>();

        public DiscordGuild Guild => this.Channel.Guild;
        public DiscordChannel Channel { get; private set; }
        public DiscordClient Client { get; }
        public VoiceNextExtension VNext => this.Client.GetVoiceNext();
        public VoiceNextConnection VCon => this.VNext.GetConnection(this.Guild);
        public Task PlaybackTask = null;

        private bool shouldPlay = true;
        private int qChannelIndex = -1;

        public void CancelPlayback() => this.shouldPlay = false;

        private RestClient Web { get; set; }
        private static RestClient RestClientGetAvailableChannels { get; set; } = new RestClient("https://api.qmusic.be/")
        {
            UserAgent = "AppleCoreMedia/1.0.0.16G77 (iPhone; U; CPU OS 12_4 like Mac OS X; nl_be)"
        };


        private const string uuid = "idfa:9FBA1A63-9417-47D8-9C8D-BD1D56EE7C9D";
        private const string appname = "rp_qmusic_app";
        private const string dist = "dpg";
        private const string sessionid = "34F117AB-A1C9-4D32-81EE-75CF87B58C01"; //Hmmm, might get hacked if this gets on git

        private static DateTime LastStreamsListUpdate = DateTime.MinValue;

        private static List<Aac> qmusicRadioChannels;
        public static List<Aac> QmusicRadioChannels
        {
            get
            {
                if (qmusicRadioChannels?.Any() != true || LastStreamsListUpdate.AddHours(1) < DateTime.UtcNow)
                {
                    LastStreamsListUpdate = DateTime.UtcNow;
                    UpdateQMusicRadioChannels();
                }

                return qmusicRadioChannels ?? new List<Aac>();
            }
            set => qmusicRadioChannels = value;
        }
        public bool IsPlaying { get; private set; }

        public Qmusic(DiscordChannel channel, DiscordClient client)
        {
            this.Client = client;
            this.Channel = channel;

            Instances.Add(this.Guild.Id, this);

            this.Web = new RestClient("https://playerservices.streamtheworld.com/")
            {
                UserAgent = "AppleCoreMedia/1.0.0.16G77 (iPhone; U; CPU OS 12_4 like Mac OS X; nl_be)",
                FollowRedirects = false
            };
            UpdateQMusicRadioChannels();
        }

        public async Task JoinChannel(DiscordChannel chan)
        {
            if (chan.Type != ChannelType.Voice)
                throw new InvalidOperationException("Cannot join a text channel");

            if (this.Channel?.Id == chan.Id && this.VNext.GetConnection(this.Guild) != null)
            {
                return;
            }

            if (this.VNext.GetConnection(this.Guild) != null)
            {
                await CancelAndFinishPlayback(); // wait for it to finish
            }

            this.Channel = chan;
            await this.VNext.ConnectAsync(chan);
        }

        public async Task SetQMusicChannelIndex(int channel)
        {
            if (channel != this.qChannelIndex)
            {
                bool wasPlaying = this.IsPlaying;
                if (wasPlaying)
                    await CancelAndFinishPlayback();

                this.qChannelIndex = channel;

                if (wasPlaying)
                    await Play();
            }
        }

        public async Task Play(DiscordChannel chan = null)
        {
            if (chan != null && chan.Id != this.Channel.Id)
            {
                await JoinChannel(chan);
            }

            if (this.IsPlaying)
            {
                await CancelAndFinishPlayback();
            }

            this.PlaybackTask = ReadMusicStream(GetStreamURL(this.qChannelIndex), this.VCon.GetTransmitStream());
        }

        public async Task CancelAndFinishPlayback()
        {
            CancelPlayback();
            if (this.PlaybackTask != null)
                await this.PlaybackTask;

            this.Client.DebugLogger.LogMessage(LogLevel.Warning, "QMusic", "Waiting for playback to finish... Channel: " + this.Channel, DateTime.UtcNow);

            SpinWait.SpinUntil(() => !this.IsPlaying);
            this.Client.DebugLogger.LogMessage(LogLevel.Warning, "QMusic", "Waiting for playback finished. Channel: " + this.Channel, DateTime.UtcNow);
        }

        public static bool TryGetInstance(ulong guildId, out Qmusic instance)
        {
            if (Instances.ContainsKey(guildId))
            {
                instance = Instances[guildId];
                return true;
            }
            instance = null;
            return false;
        }

        public string GetStreamURL(int index = -1)
        {
            string url = $"api/livestream-redirect/QMUSICAAC.aac?uuid={uuid}&pname={appname}&dist={dist}";
            if (QmusicRadioChannels.Count > 0 && index > -1 && index < QmusicRadioChannels.Count - 1)
            {
                url = QmusicRadioChannels[index].source.Replace("https://playerservices.streamtheworld.com/", "");
            }
            var request = new RestRequest(url, Method.GET);
            ConfigureHeader(ref request);

            var response = this.Web.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Found)
                return response.Headers.ToList().Find(x => x.Name == "Location").Value.ToString();
            return null;
        }

        public async Task ReadMusicStream(string url, VoiceTransmitStream vstream)
        {
            if (this.IsPlaying)
            {
                await CancelAndFinishPlayback();
            }

            this.shouldPlay = true;
            this.IsPlaying = true;

            //string requestString = $"GET /{split[split.Length-1]} HTTP/1.1\r\n";

            string hosteDNS = url.Replace("https://", "").Split('/')[0];
            var split = url.Split('/');
            string requestString = $"GET /{split[split.Length - 1]} HTTP/1.1\r\n";

            requestString += $"Host: {hosteDNS}:443\r\n";
            requestString += $"X-Playback-Session-Id: {sessionid}\r\n";
            //requestString += "Range: bytes=0-1\r\n"; //LMAO don't use this, will only give 256kb, some kind of special buffer??
            requestString += "icy-metadata:	1\r\n";
            requestString += "Accept: */*\r\n";
            requestString += "User-Agent: AppleCoreMedia/1.0.0.16G77 (iPhone; U; CPU OS 12_4 like Mac OS X; nl_be)\r\n";
            requestString += "Accept-Language: nl-be\r\n";
            requestString += "Accept-Encoding: identity\r\n";
            requestString += "Connection: keep-alive\r\n";
            requestString += "\r\n";

            TcpClient client = new TcpClient();
            client.Connect($"{hosteDNS}", 80);

            NetworkStream stream = client.GetStream();
            //stream = client.GetStream();

            StreamWriter writer = new StreamWriter(stream);

            // Send the request.
            writer.Write(requestString);
            writer.Flush();

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $@"-i - -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            var ffmpeg = Process.Start(psi);

            var ctoksrc = new CancellationTokenSource();
            var ctok = ctoksrc.Token;

            var t = Task.Run(() =>
            {
                var buf2 = new byte[1024];
                while (!ctok.IsCancellationRequested) // cancel via cancellation token so that the while loop which writes to discord cant get stuck on reading if no data is supplied anymore.
                {
                    var len = stream.Read(buf2, 0, buf2.Length);
                    ffmpeg.StandardInput.BaseStream.Write(buf2, 0, len);
                }
            });

            byte[] buf = new byte[8192 * 16];
            DateTime startTime = DateTime.UtcNow;   //timeout after 30 min


            while (this.shouldPlay && startTime.AddMinutes(30) > DateTime.UtcNow)
            {
                var ffout = ffmpeg.StandardOutput.BaseStream;

                var lenread = ffout.Read(buf, 0, buf.Length);

                vstream.Write(buf, 0, lenread);
            }

            ctoksrc.Cancel();

            stream.Close();
            writer.Close();
            client.Close();

            ffmpeg.Kill();
            bool exitedInTime = ffmpeg.WaitForExit(2500);

            if (!exitedInTime)
            {
                this.Client.DebugLogger.LogMessage(LogLevel.Error, "QMusic", "ffmpeg didnt exit in time!", DateTime.UtcNow);
            }


            await this.VCon.WaitForPlaybackFinishAsync();

            this.IsPlaying = false;
        }

        public static void UpdateQMusicRadioChannels()
        {
            var request = new RestRequest($"2.4/app/channels", Method.GET);
            //confHeader(ref request);

            var response = RestClientGetAvailableChannels.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return;

            var result = JsonConvert.DeserializeObject<channelsResponse>(response.Content);

            List<Aac> aacStreams = new List<Aac>();
            foreach (var d in result.data)
            {
                if (d.data != null && d.data.streams != null && d.data.streams.aac != null)
                {
                    aacStreams.Add(d.data.streams.aac.FirstOrDefault());
                }
            }
            qmusicRadioChannels = aacStreams;
        }
        public trackResponse GetTrackInfo(string channel)
        {
            var request = new RestRequest($"2.4/tracks/plays?_station_id={channel}&limit=20&next=1", Method.GET);

            var response = this.webq.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return null;
            return JsonConvert.DeserializeObject<trackResponse>(response.Content);
        }

        private void ConfigureHeader(ref RestRequest request)
        {
            request.AddHeader("Host", "playerservices.streamtheworld.com");
            request.AddHeader("X-Playback-Session-Id", sessionid);
            request.AddHeader("icy-metadata", "1");
            request.AddHeader("Accept-Encoding", "identity");
            request.AddHeader("Accept-Language", "nl-be");
            request.AddHeader("connection", "keep-alive");
            request.AddHeader("Accept", "*/*");
        }
    }
}
