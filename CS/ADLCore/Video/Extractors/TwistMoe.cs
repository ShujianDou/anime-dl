﻿using ADLCore.Ext;
using ADLCore.Video.Constructs;
using Brotli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ADLCore.Video.Extractors
{
    public class TwistMoe : ExtractorBase
    {
        private WebHeaderCollection whc;
        private HttpWebRequest wRequest;
        private WebResponse response;
        private TwistMoeAnimeInfo info;
        private Byte[] KEY = Convert.FromBase64String("MjY3MDQxZGY1NWNhMmIzNmYyZTMyMmQwNWVlMmM5Y2Y=");
        //episodes to download: 0-12, 1-12, 5-6 etc.
        //TODO: Implement download ranges for GoGoStream and TwistMoe (and novel downloaders)

        //key  MjY3MDQxZGY1NWNhMmIzNmYyZTMyMmQwNWVlMmM5Y2Y= -> search for atob(e) and floating-player
        public TwistMoe(ArgumentObject args, int ti = -1, Action<int, string> u = null) : base(args, ti, u)
        {
            GenerateHeaders();
        }

        public override void Begin()
        {
            videoInfo = new Constructs.Video();
            Download(ao.term, ao.mt, ao.cc);
        }

        //TODO: Implement dual threaded downloading for multithreading.
        public override bool Download(string path, bool mt, bool continuos)
        {
            for(int idx = 0; idx < info.episodes.Count; idx++)
            {
                string source = Encoding.UTF8.GetString(M3U.DecryptAES128(Convert.FromBase64String(info.episodes[idx].source), KEY, null, new byte[8], 256));
                downloadVideo("https://cdn.twist.moe" + source, idx);
            }
            return true;
        }

        private void downloadVideo(string url, int number)
        {
            int downloadPartAmount = 100000; //500k bytes/0.5mb (at a time)
            int[] downloadRange = new int[2];
            wRequest = (HttpWebRequest)WebRequest.Create(url);
            wRequest.Headers = whc;
            wRequest.Host = "cdn.twist.moe";
            wRequest.Referer = $"https://twist.moe/{info.slug}";
            wRequest.AddRange(0, 999999999999);
            WebResponse a = wRequest.GetResponse();
            
            downloadRange[1] = int.Parse(a.Headers["Content-Length"]);
            downloadRange[0] = 0;
            Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Anime{Path.DirectorySeparatorChar}Twist{Path.DirectorySeparatorChar}");
            if (File.Exists($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Anime{Path.DirectorySeparatorChar}Twist{Path.DirectorySeparatorChar}{info.title.RemoveSpecialCharacters()}_{number}.mp4"))
                downloadRange[0] = File.ReadAllBytes($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Anime{Path.DirectorySeparatorChar}Twist{Path.DirectorySeparatorChar}{info.title.RemoveSpecialCharacters()}_{number}.mp4").Length;
            FileStream fs = new FileStream($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Anime{Path.DirectorySeparatorChar}Twist{Path.DirectorySeparatorChar}{info.title.RemoveSpecialCharacters()}_{number}.mp4", FileMode.OpenOrCreate);
            fs.Position = downloadRange[0];
            while (downloadRange[0] < downloadRange[1])
            {
                System.IO.Stream ab;
            Retry:;
                try
                {
                    wRequest = (HttpWebRequest)WebRequest.Create(url);
                    wRequest.Headers = whc;
                    wRequest.Host = "cdn.twist.moe";
                    wRequest.Referer = $"https://twist.moe/{info.slug}";
                    wRequest.AddRange(downloadRange[0], downloadRange[0] + downloadPartAmount);
                    a = wRequest.GetResponse();
                    ab = a.GetResponseStream();
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ab.CopyTo(ms);
                        downloadRange[0] += ms.ToArray().Length;
                        ms.Seek(0, SeekOrigin.Begin);
                        ms.CopyTo(fs);
                    }
                }
                catch(Exception x)
                {
                    goto Retry;
                }
            }
        }

        public override void GenerateHeaders()
        {
            whc = new WebHeaderCollection();
            whc.Add("DNT", "1");
            whc.Add("Sec-Fetch-Dest", "document");
            whc.Add("Sec-Fetch-Site", "none");

            //Get anime slug to use for api
            string k = ao.term.TrimToSlash(false).SkipCharSequence("https://twist.moe/a/".ToCharArray());
            string uri = $"https://api.twist.moe/api/anime/{k}";
            wRequest = (HttpWebRequest)WebRequest.Create(uri);
            wRequestSet();
            WebResponse wb = wRequest.GetResponse();
            string decodedContent = M3U.DecryptBrotliStream(wb.GetResponseStream());
            info = JsonSerializer.Deserialize<TwistMoeAnimeInfo>(decodedContent);

            wRequest = (HttpWebRequest)WebRequest.Create($"https://api.twist.moe/api/anime/{k}/sources");
            wRequestSet();
            wb = wRequest.GetResponse();
            decodedContent = M3U.DecryptBrotliStream(wb.GetResponseStream());
            info.episodes = JsonSerializer.Deserialize<List<Episode>>(decodedContent);
        }

        private void wRequestSet(bool api = true)
        {
            //wRequest.Headers = whc;
            wRequest.Headers.Add("cache-control", "max-age=0");
            wRequest.Headers.Add("upgrade-insecure-requests", "1");
            wRequest.UseDefaultCredentials = true;
            wRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            wRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.2; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 OPR/73.0.3856.344";
            
            wRequest.Host = $"{(api == true ? "api" : "cdn")}.twist.moe";
            wRequest.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            //            wRequest.Referer = "https://twist.moe";

        }

        public override dynamic Get(HentaiVideo obj, bool dwnld)
        {
            throw new NotImplementedException();
        }

        public override string GetDownloadUri(string path)
        {
            throw new NotImplementedException();
        }

        public override string GetDownloadUri(HentaiVideo path)
        {
            throw new NotImplementedException();
        }

        public override string Search(string name, bool d = false)
        {
            throw new NotImplementedException();
        }
    }
}
