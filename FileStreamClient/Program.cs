﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;

namespace FileStreamClient
{
    class Program
    {
        public static string SourceFile { get; set; }
        public static string Endpoint { get; set; }
        public static int ChunkSizeKB { get; set; } = 64;
        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            ReadParams(args);
            while (true)
            {
                try
                {
                    Task.Run(async () => await new Program().UploadFile(cts.Token), cts.Token).Wait(cts.Token);
                    Task.Run(async () => await new Program().UploadChunk(cts.Token), cts.Token).Wait(cts.Token);
                    //Task.Run(async () => await new Program().StreamChunk(cts.Token), cts.Token).Wait(cts.Token);

                    //Task.Run(async () => await new Program().FormFile(cts.Token), cts.Token).Wait(cts.Token);
                    //Task.Run(async () => await new Program().ByteArray(cts.Token), cts.Token).Wait(cts.Token);
                }
                catch(Exception ex)
                {
                    WriteLine(ex.StackTrace);
                }
            }
        }

        private async Task ByteArray(CancellationToken cancellationToken)
        {
            WriteLine($"------ Uploading with in-body byte[] ------");
            Write("Enter the number of times to loop");
            var repeat = int.Parse(ReadLine() ?? "0");
            WriteLine($"Preparing stream...");
            var sw = new Stopwatch();
            sw.Start();
            await using var file = new FileStream(SourceFile, FileMode.Open);
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms, cancellationToken);
            var content = new ByteArrayContent(ms.ToArray());
            await SendFile(cancellationToken, content, sw, Endpoint + "ByteArray", repeat);
        }

        private async Task FormFile(CancellationToken cancellationToken)
        {
            WriteLine($"------ Uploading with FormFile ------");
            Write("Enter the number of times to loop");
            var repeat = int.Parse(ReadLine() ?? "0");
            WriteLine($"Preparing stream...");
            for (var i = 0; i < repeat; i++)
            {
                var sw = new Stopwatch();
                sw.Start();
                await using var file = new FileStream(SourceFile, FileMode.Open);
                var content = new MultipartFormDataContent($"---{Guid.NewGuid()}")
                {
                    {
                        new StreamContent(file, 64), Guid.NewGuid().ToString(),
                            Path.GetFileName(SourceFile) ?? Guid.NewGuid().ToString()
                    }
                };
                await SendFile(cancellationToken, content, sw, Endpoint + "FormFile", repeat);
            }
        }

        private async Task UploadFile(CancellationToken cancellationToken)
        {
            WriteLine($"------ Uploading in one piece ------");
            WriteLine("Enter the number of times to loop");
            var repeat = int.Parse(ReadLine() ?? "0");
            WriteLine($"Preparing stream...");
            for (var i = 0; i < repeat; i++)
            {
                var sw = new Stopwatch();
                sw.Start();
                await using var file = new FileStream(SourceFile, FileMode.Open);
                var content = new MultipartFormDataContent($"---{Guid.NewGuid()}")
                {
                    {
                        new StreamContent(file), Guid.NewGuid().ToString(),
                            Path.GetFileName(SourceFile) ?? Guid.NewGuid().ToString()
                    }
                };
                await SendFile(cancellationToken, content, sw, Endpoint + "Streaming", repeat);
            }
        }

        private async Task UploadChunk(CancellationToken cancellationToken)
        {
            WriteLine($"------ Uploading in {ChunkSizeKB}KB chunks using form ------");
            WriteLine("Enter the number of times to loop");
            var repeat = int.Parse(ReadLine() ?? "0");
            WriteLine($"Preparing stream...");
            for (var i = 0; i < repeat; i++)
            {
                var sw = new Stopwatch();
                sw.Start();
                var content = new MultipartFormDataContent($"---{Guid.NewGuid()}");
                await using var file = new FileStream(SourceFile, FileMode.Open);
                var offset = 0;
                while (offset < file.Length)
                {
                    var count = ChunkSizeKB * 1024;
                    var chunk = new byte[count];
                    var readBytes = await file.ReadAsync(chunk, 0, count, cancellationToken);
                    content.Add(new StreamContent(new MemoryStream(chunk, 0, readBytes)), Guid.NewGuid().ToString(), Path.GetFileName(SourceFile) ?? Guid.NewGuid().ToString());
                    offset += count;
                }
                await SendFile(cancellationToken, content, sw, Endpoint + "Streaming", repeat);
            }
        }

        private async Task StreamChunk(CancellationToken cancellationToken)
        {
            WriteLine($"------ Uploading in 1 chunk using stream ------");
            WriteLine("Enter the number of times to loop");
            var repeat = int.Parse(ReadLine() ?? "0");
            WriteLine($"Preparing stream...");
            for (var i = 0; i < repeat; i++)
            {
                var sw = new Stopwatch();
                sw.Start();
                //var content = new MultipartFormDataContent($"---{Guid.NewGuid()}");
                await using var file = new FileStream(SourceFile, FileMode.Open);
                var content = new MultipartFormDataContent($"---{Guid.NewGuid()}")
                {
                    new StreamContent(file)
                };

                await SendFile(cancellationToken, content, sw, Endpoint + "Streaming", repeat);
            }
        }

        private async Task SendFile(CancellationToken cancellationToken, HttpContent content, Stopwatch sw,
            string endpoint, int repeat)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            requestMessage.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            requestMessage.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

            requestMessage.Headers.TryAddWithoutValidation("CorrelationId", Guid.NewGuid().ToString());
            var client = new HttpClient();

            WriteLine($"Prepare request took {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            var response = await client.SendAsync(requestMessage, cancellationToken);
            WriteLine($"Sending took {sw.ElapsedMilliseconds}ms");
            WriteLine($"{response.StatusCode} \n {await response.Content.ReadAsStringAsync(cancellationToken)}");
        }

        private static void ReadParams(string[] args)
        {
            var endpoint = string.Empty;
            for (var i = 0; i < args.Count(); i++)
            {
                switch (args[i])
                {
                    case "-f":
                        SourceFile = args[++i].TrimStart('"').TrimEnd('"');
                        continue;
                    case "-cz":
                        ChunkSizeKB = int.Parse(args[++i].TrimStart('"').TrimEnd('"'));
                        continue;
                    default:
                        Endpoint = args[i].TrimStart('"').TrimEnd('"');
                        continue;
                }
            }

        }
    }
}
