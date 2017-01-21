using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Sweetie_bot
{
    using Discord;
    using Discord.Audio;
    using System.IO;
    using System.Diagnostics;

    public class Audio
    {
        private static DiscordClient _client = null;
        private static IAudioClient _audioClient = null;
        private static Channel _audioChannel = null;
        private static bool _nextSong = false;
        private static ConcurrentQueue<string> _songQueue;

        public static async Task Initialize(DiscordClient client)
        {
            _client = client;
            _audioChannel = _client.Servers.FirstOrDefault().VoiceChannels.FirstOrDefault();
            AudioService audioService = _client.GetService<AudioService>();
            bool startLoop = _audioClient == null;
            _audioClient = await audioService.Join(_audioChannel);
            if (_songQueue == null)
            {
                _songQueue = new ConcurrentQueue<string>();
                _nextSong = true;
            }

#pragma warning disable CS4014 // Not awaited to allow for method to close
            if (startLoop) { DoWorkAsyncInfiniteLoop(); }
#pragma warning restore CS4014 
        }

        public static void Enque(string songUrl)
        {
            _songQueue.Enqueue(songUrl);
        }

        public static int QueueCount()
        {
            return _songQueue.Count;
        }

        private static async Task DoWorkAsyncInfiniteLoop()
        {
            while (true)
            {
                if (_audioClient.State == ConnectionState.Disconnected)
                {
                    Debug.WriteLine("Reconnecting");
                    _audioClient = await JoinAudioChannel(_audioChannel);
                }

                await Task.Delay(100);

                if (_nextSong)
                {
                    _nextSong = false;
                    string songUrl;
                    if (_songQueue.TryDequeue(out songUrl))
#pragma warning disable CS4014 // not awaited to because of infiniant loop call.
                        SendAudio(songUrl);
#pragma warning restore CS4014 
                }
            }
        }

        private static async Task SendAudio(string pathOrUrl)
        {
            Process process;
            string[] url = pathOrUrl.Split(new string[] { "watch?v=" }, StringSplitOptions.RemoveEmptyEntries);
            string outputFile = "\"" + url[url.Length - 1] + ".m4a\"";
            string mp3OutputFile = "songs/" + outputFile.Replace(".m4a", ".mp3").Replace("\"", "");
            bool outputFileExists = File.Exists(outputFile.Replace("\"", ""));
            bool mp3OutputFileExists = File.Exists(mp3OutputFile);

            if (!outputFileExists && !mp3OutputFileExists)
            {
                if (pathOrUrl.Contains("watch?v="))
                {
                    process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "youtube-dl",
                        Arguments = $"-f 140 {pathOrUrl}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    });

                    string output = process.StandardOutput.ReadToEnd();
                    string[] outputLines = output.Split('\n');
                    for (int i = 0; i < outputLines.Length; ++i)
                    {
                        if (outputLines[i].Contains(".m4a"))
                        {
                            string outLine = outputLines[i].Split(new string[] { "[ffmpeg] Correcting container in " }, StringSplitOptions.RemoveEmptyEntries)[0];
                            //outLine = outLine.Remove(outLine.Length - 1, 1);
                            if (outLine.EndsWith(".m4a\""))
                                pathOrUrl = outLine;
                        }
                    }
                }
            }

            if (File.Exists(pathOrUrl.Replace("\"", "")) ||
                outputFileExists ||
                mp3OutputFileExists)
            {
                if (!outputFileExists && !mp3OutputFileExists)
                    File.Move(pathOrUrl.Replace("\"", ""), outputFile.Replace("\"", ""));

                SendMP3AudioFile(outputFile);
                //SendAudioFile(outputFile);
            }
            await Task.Delay(1);
        }

        private static string ConvertToMp3(string pathOrUrl)
        {
            string mp3path = "songs/" + pathOrUrl.Replace(".m4a", ".mp3");
            if (!File.Exists(mp3path.Replace("\"", "")))
            {
                Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i {pathOrUrl} " +
                    $"-codec:v copy -codec:a libmp3lame -q:a 2 {mp3path}",
                    UseShellExecute = false
                    //RedirectStandardOutput = true
                });
                process.WaitForExit();
            }

            pathOrUrl = pathOrUrl.Replace("\"", "");
            if (File.Exists(pathOrUrl))
                File.Delete(pathOrUrl);

            return mp3path.Replace("\"", "");
        }

        private static async Task<IAudioClient> JoinAudioChannel(Channel channel)
        {
            return await _client.GetService<AudioService>().Join(channel);
        }

        private static async void SendMP3AudioFile(string filePath)
        {
            Channel channel = _audioClient.Channel;
            filePath = ConvertToMp3(filePath);

            int channelCount = _client.GetService<AudioService>().Config.Channels;
            NAudio.Wave.WaveFormat OutFormat = new NAudio.Wave.WaveFormat(48000, 16, channelCount);
            using (NAudio.Wave.Mp3FileReader MP3Reader = new NAudio.Wave.Mp3FileReader(filePath))
            {
                using (NAudio.Wave.MediaFoundationResampler resampler = new NAudio.Wave.MediaFoundationResampler(MP3Reader, OutFormat))
                {
                    resampler.ResamplerQuality = 60;
                    int blockSize = OutFormat.AverageBytesPerSecond / 50;
                    byte[] buffer = new byte[blockSize];
                    int byteCount;

                    while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0)
                    {


                        if (byteCount < blockSize)
                        {
                            for (int i = byteCount; i < blockSize; ++i)
                                buffer[i] = 0;
                        }

                        if (_audioClient.State == ConnectionState.Disconnecting || _audioClient.State == ConnectionState.Disconnected)
                            System.Threading.Thread.Sleep(1000);

                        try
                        {
                            _audioClient.Send(buffer, 0, blockSize);
                        }
#pragma warning disable CS0168 // Variable is declared but never used, supressed error because it must be declared to be caught
                        catch (OperationCanceledException e)
#pragma warning restore CS0168 
                        {
                            //if (!(_audioClient.State == ConnectionState.Disconnecting || _audioClient.State == ConnectionState.Disconnected))
                            //{
                            _audioClient = await JoinAudioChannel(channel);
                            System.Threading.Thread.Sleep(1000);
                            _audioClient.Send(buffer, 0, blockSize);
                            //}
                        }
                    }
                    //await _audioClient.Disconnect();
                }
            }
            _nextSong = true;
        }

        private static void SendAudioFile(string filePath)
        {
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {filePath} " +
                        "-f s16le -ar 48000 -ac 2 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            System.Threading.Thread.Sleep(5000);

            int blockSize = 3840;
            byte[] buffer = new byte[blockSize];
            int byteCount;

            while (true)
            {
                byteCount = process.StandardOutput.BaseStream.Read(buffer, 0, blockSize);
                if (byteCount == 0)
                    break;

                _audioClient.Send(buffer, 0, byteCount);
            }
            _nextSong = true;
            //process.WaitForExit();
            //_audioClient.Wait();
        }
    }
}
