using System;
using CSCore;
using CSCore.Codecs;
using CSCore.MediaFoundation;
using CommandLine;
using System.Collections.Generic;
using System.Linq;

namespace SplitAudio
{
    class Program
    {

        class Options
        {
            [Option('r', "read", Required = true, HelpText = "Input file to be processed.")]
            public String InputFile { get; set; }

            [Option('t', "time", Required = true, HelpText = "Comma delimted input string of times to split out. Format follows <starttime>|<time span>. Ex: 01:24|03:12,01:22:14|4:56", Separator = ',')]
            public IEnumerable<String> InputTimes { get; set; }

            // Omitting long name, defaults to name of property, ie "--verbose"
            [Option(
              Default = false,
              HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

        }

        class ChopJob
        {
            public TimeSpan StartTime { get; set; }
            public TimeSpan Span { get; set; }
            public String OutFileName { get; set; }
        }

        //ewwww, but perfect is the enemy of good
        private static readonly List<ChopJob> Jobs = new System.Collections.Generic.List<ChopJob>();
        private static String InputFilePath;

        static void Main(string[] args)
        {


            CommandLine.Parser.Default.ParseArguments<Options>(args)
    .WithParsed(RunOptions)
    .WithNotParsed(HandleParseError);

            Console.WriteLine("Beginning to load: {0}", InputFilePath);
            using (IWaveSource source = CodecFactory.Instance.GetCodec(InputFilePath))
            {
                Console.WriteLine("Loaded: {0}", InputFilePath);
                foreach (var jobbie in Jobs)
                {
                    Console.WriteLine("Working on job with startTime: {0} timeSpan: {1}", jobbie.StartTime.ToString(), jobbie.Span.ToString());
                    using (MediaFoundationEncoder mediaFoundationEncoder =
                                    MediaFoundationEncoder.CreateWMAEncoder(source.WaveFormat, jobbie.OutFileName))
                    {
                        AddTimeSpan(source, mediaFoundationEncoder, jobbie.StartTime, jobbie.Span);
                    }

                }
            }
        }

        private static TimeSpan GetTimespan(String input)
        {
            TimeSpan retVal;
            if (!(input.Count(c => c == ':') > 1))
            {
                input = "00:" + input;
            }
            if (!System.TimeSpan.TryParse(input, out retVal))
            {
                Console.WriteLine("ERROR, not able to parse the following value into a timespan: " + input);
                throw new ArgumentOutOfRangeException("Invalid timespan: " + input);
            }
            return retVal;
        }

        private static Tuple<TimeSpan, TimeSpan> GetJobTimes(String input)
        {
            TimeSpan startTime;
            TimeSpan span;
            if (!(input.IndexOf('|') > 0))
            {
                throw new ArgumentException("Incorrect time entered: " + input);
            }

            String[] split = input.Split('|');
            startTime = GetTimespan(split[0]);
            span = (startTime + GetTimespan(split[1]));

            return new Tuple<TimeSpan, TimeSpan>(startTime, span);
        }


        static void RunOptions(Options opts)
        {
            //handle options
            InputFilePath = opts.InputFile;
            Int32 i = 0;
            String inputDirectory = System.IO.Path.GetDirectoryName(opts.InputFile);
            String inputFileName = System.IO.Path.GetFileNameWithoutExtension(opts.InputFile);
            String inputFileExtension = System.IO.Path.GetExtension(opts.InputFile);
            foreach (var timez in opts.InputTimes)
            {
                ChopJob cj = new ChopJob();
                Tuple<TimeSpan, TimeSpan> jobTimes = GetJobTimes(timez);
                cj.StartTime = jobTimes.Item1;
                cj.Span = jobTimes.Item2;
                String finalFilePath = System.IO.Path.Combine(inputDirectory, inputFileName + i.ToString() + inputFileExtension);
                cj.OutFileName = finalFilePath;
                Jobs.Add(cj);
                i++;
                Console.WriteLine("New job added with startTime of: {0} and target output path of {1}", cj.StartTime, cj.OutFileName);
            }

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
