using System;
using System.Collections.Generic;
using System.Text;
using RestSharp;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace CababgeBot.Tools.Qmusic
{
    class Qmusic
    {
        private RestClient web { get; set; }
        private string uuid = "idfa:9FBA1A63-9417-47D8-9C8D-BD1D56EE7C9D"; 
        private string appname = "rp_qmusic_app";
        private string dist = "dpg";
        private string sessionid = "34F117AB-A1C9-4D32-81EE-75CF87B58C01"; //Hmmm, might get hacked if this gets on git

        public bool isPlaying { get; set; }
        public Qmusic()
        {
            this.web = new RestClient("https://playerservices.streamtheworld.com/");
            this.web.UserAgent = "AppleCoreMedia/1.0.0.16G77 (iPhone; U; CPU OS 12_4 like Mac OS X; nl_be)";
            this.web.FollowRedirects = false;
        }

        public string GetStreamURL()
        {
            var request = new RestRequest($"api/livestream-redirect/QMUSICAAC.aac?uuid={uuid}&pname={appname}&dist={dist}", Method.GET);
            confHeader(ref request);

            var response = web.Execute(request);
            if(response.StatusCode == System.Net.HttpStatusCode.Found)
                return response.Headers.ToList().Find(x => x.Name == "Location").Value.ToString();
            return null;
        }

        public void ReadMusicStream(string url)
        {
            this.isPlaying = true;

            using (TcpClient client = new TcpClient())
            {

                string hosteDNS = url.Replace("https://", "").Split('/')[0];
                string requestString = $"GET /QMUSICAAC.aac HTTP/1.1\r\n";

                requestString += "Host: unknown:443\r\n";
                requestString += $"X-Playback-Session-Id: {sessionid}\r\n";
                requestString += "Range: bytes=0-1\r\n";
                requestString += "Accept: */*\r\n";
                requestString += "User-Agent: AppleCoreMedia/1.0.0.16G77 (iPhone; U; CPU OS 12_4 like Mac OS X; nl_be)\r\n";
                requestString += "Accept-Language: nl-be\r\n";
                requestString += "Accept-Encoding: identity\r\n";
                requestString += "Connection: keep-alive\r\n";
                requestString += "\r\n";

                client.Connect("21263.live.streamtheworld.com", 80);

                using (NetworkStream stream = client.GetStream())
                {
                    // Send the request.
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write(requestString);
                    writer.Flush();

                    StreamReader reader = new StreamReader(stream);

                    byte[] buffer = new byte[6800]; //6.8 kb/second?

                    DateTime StartTime = DateTime.UtcNow;

                    List<byte> BufferFile = new List<byte>();
                    int count = 0;
                    while (!reader.EndOfStream && this.isPlaying)
                    {
                        
                        stream.Read(buffer, 0, buffer.Length);
                        BufferFile.AddRange(buffer);
                        
                        //send data around
                        foreach(var b in buffer)
                        {
                            Console.Write(b.ToString("X2"));
                        }
                        Console.WriteLine();

                        //debugging
                        if (count > 10)
                            break;
                        count++;

                        //if (StartTime.AddSeconds(10) < DateTime.UtcNow)
                        //    this.isPlaying = false;
                    }
                    File.WriteAllBytes("test.mp4", BufferFile.ToArray());
                }
            }
            return;
        }

        private void confHeader(ref RestRequest request)
        {
            request.AddHeader("Host", "playerservices.streamtheworld.com");
            request.AddHeader("X-Playback-Session-Id", sessionid);
            request.AddHeader("icy-metadata","1");
            request.AddHeader("Accept-Encoding", "identity");
            request.AddHeader("Accept-Language", "nl-be");
            request.AddHeader("connection", "keep-alive");
            request.AddHeader("Accept", "*/*");
        }
    }
}
