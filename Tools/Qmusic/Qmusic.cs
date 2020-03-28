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
        public static Qmusic Instance { get; set; }

        private RestClient web { get; set; }
        private RestClient webq { get; set; }
        private string uuid = "idfa:9FBA1A63-9417-47D8-9C8D-BD1D56EE7C9D";
        private string appname = "rp_qmusic_app";
        private string dist = "dpg";
        private string sessionid = "34F117AB-A1C9-4D32-81EE-75CF87B58C01"; //Hmmm, might get hacked if this gets on git
        public List<Aac> channels { get; set; }
        public bool isPlaying { get; set; }

        public Qmusic()
        {
            Instance = this;
            this.web = new RestClient("https://playerservices.streamtheworld.com/");
            this.webq = new RestClient("https://api.qmusic.be/");
            this.web.UserAgent = "AppleCoreMedia/1.0.0.16G77 (iPhone; U; CPU OS 12_4 like Mac OS X; nl_be)";
            this.webq.UserAgent = "AppleCoreMedia/1.0.0.16G77 (iPhone; U; CPU OS 12_4 like Mac OS X; nl_be)";
            this.web.FollowRedirects = false;
            GetStreamsList();
        }

        public string GetStreamURL(int index = -1)
        {
            string url = $"api/livestream-redirect/QMUSICAAC.aac?uuid={this.uuid}&pname={this.appname}&dist={this.dist}";
            if (this.channels.Count > 0 && index > -1 && index < this.channels.Count - 1)
            {
                url = this.channels[index].source.Replace("https://playerservices.streamtheworld.com/", "");
            }
            var request = new RestRequest(url, Method.GET);
            confHeader(ref request);

            var response = this.web.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Found)
                return response.Headers.ToList().Find(x => x.Name == "Location").Value.ToString();
            return null;
        }

        public Task ReadMusicStream(string url, ref Dictionary<ulong, bool> status, ref VoiceTransmitStream vstream, ulong GuildID)
        {
            this.isPlaying = true;

            //string requestString = $"GET /{split[split.Length-1]} HTTP/1.1\r\n";

            string hosteDNS = url.Replace("https://", "").Split('/')[0];
            var split = url.Split('/');
            string requestString = $"GET /{split[split.Length - 1]} HTTP/1.1\r\n";

            requestString += $"Host: {hosteDNS}:443\r\n";
            requestString += $"X-Playback-Session-Id: {this.sessionid}\r\n";
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

            // TODO might be able to remove this
            ffmpeg.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                Console.WriteLine(e.Data);
            };

            var ctoksrc = new CancellationTokenSource();
            var ctok = ctoksrc.Token;

            var t = Task.Run(() =>
            {
                var buf2 = new byte[1024];
                while (!ctok.IsCancellationRequested)
                {
                    var len = stream.Read(buf2, 0, buf2.Length);
                    ffmpeg.StandardInput.BaseStream.Write(buf2, 0, len);
                }
            });

            //byte[] buf = new byte[8192 * 16];
            byte[] buf = new byte[8192];
            DateTime startTime = DateTime.UtcNow;   //timeout after 30 min

            while (status.ContainsKey(GuildID) && status[GuildID] && startTime.AddMinutes(30) > DateTime.UtcNow)
            {
                var ffout = ffmpeg.StandardOutput.BaseStream;

                var lenread = ffout.Read(buf, 0, buf.Length);
                vstream.Write(buf, 0, lenread);
            }
            status[GuildID] = false;
            ctoksrc.Cancel();
            stream.Close();
            writer.Close();
            client.Close();
            return Task.CompletedTask;
        }

        public List<Aac> GetStreamsList()
        {
            var request = new RestRequest($"2.4/app/channels", Method.GET);
            //confHeader(ref request);

            var response = this.webq.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return null;
            var result = JsonConvert.DeserializeObject<channelsResponse>(response.Content);

            List<Aac> aacStreams = new List<Aac>();
            foreach (var d in result.data)
            {
                if (d.data != null && d.data.streams != null && d.data.streams.aac != null)
                {
                    aacStreams.Add(d.data.streams.aac.FirstOrDefault());
                }
            }
            this.channels = aacStreams;
            return this.channels;
        }
        private void confHeader(ref RestRequest request)
        {
            request.AddHeader("Host", "playerservices.streamtheworld.com");
            request.AddHeader("X-Playback-Session-Id", this.sessionid);
            request.AddHeader("icy-metadata", "1");
            request.AddHeader("Accept-Encoding", "identity");
            request.AddHeader("Accept-Language", "nl-be");
            request.AddHeader("connection", "keep-alive");
            request.AddHeader("Accept", "*/*");
        }
    }
}
