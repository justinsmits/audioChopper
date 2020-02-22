using System;
using CSCore;
using CSCore.Codecs;
using CSCore.MediaFoundation;
using CommandLine;
using System.Collections.Generic;

namespace SplitAudio
{
    class Program
    {

        class Options
        {
            [Option('r', "read", Required = true, HelpText = "Input files to be processed.")]
            public IEnumerable<string> InputFiles { get; set; }

            [Option('t', "time", Required = true, HelpText = "Comma delimted input string of times to split out. Format follows <starttime>|<time span>. Ex: 01:24|03:12,01:22:14|4:56")]
            public IEnumerable<String> InputTimes { get; set; }

            // Omitting long name, defaults to name of property, ie "--verbose"
            [Option(
              Default = false,
              HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

            [Option("stdin",
              Default = false,
              HelpText = "Read from stdin")]
            public bool stdin { get; set; }

            [Value(0, MetaName = "offset", HelpText = "File offset.")]
            public long? Offset { get; set; }
        }


        static void Main(string[] args)
        {


            CommandLine.Parser.Default.ParseArguments<Options>(args)
    .WithParsed(RunOptions)
    .WithNotParsed(HandleParseError);

            return;
            TimeSpan startTimeSpan = TimeSpan.FromSeconds(4920);
            TimeSpan endTimeSpan = TimeSpan.FromSeconds(5160);

            using (IWaveSource source = CodecFactory.Instance.GetCodec(@"C:\Source2\282.mp3"))
            using (MediaFoundationEncoder mediaFoundationEncoder =
                MediaFoundationEncoder.CreateWMAEncoder(source.WaveFormat, @"C:\Source2\282cut.mp3"))
            {
                AddTimeSpan(source, mediaFoundationEncoder, startTimeSpan, endTimeSpan);
            }
        }

        static void RunOptions(Options opts)
        {
            //handle options
        }
        static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
        }

        private static void AddTimeSpan(IWaveSource source, MediaFoundationEncoder mediaFoundationEncoder, TimeSpan startTimeSpan, TimeSpan endTimeSpan)
        {
            source.SetPosition(startTimeSpan);

            int read = 0;
            long bytesToEncode = source.GetRawElements(endTimeSpan - startTimeSpan);

            var buffer = new byte[source.WaveFormat.BytesPerSecond];
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                int bytesToWrite = (int)Math.Min(read, bytesToEncode);
                mediaFoundationEncoder.Write(buffer, 0, bytesToWrite);
                bytesToEncode -= bytesToWrite;
            }
        }
    }
}
