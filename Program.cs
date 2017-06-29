using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Open_Rails_Roadmap_bot
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new CommandLineParser.Arguments.FileArgument('c', "config")
            {
                DefaultValue = new FileInfo("config.json")
            };

            var commandLineParser = new CommandLineParser.CommandLineParser()
            {
                Arguments = {
                    config,
                }
            };

            try
            {
                commandLineParser.ParseCommandLine(args);

                Main(new ConfigurationBuilder()
                    .AddJsonFile(config.Value.FullName, true)
                    .Build());
            }
            catch (CommandLineParser.Exceptions.CommandLineException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void Main(IConfigurationRoot config)
        {
        }
    }
}
