using System;
using System.IO;
using System.Threading.Tasks;
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
					.Build()).Wait();
			}
			catch (CommandLineParser.Exceptions.CommandLineException e)
			{
				Console.WriteLine(e.Message);
			}
		}

		static async Task Main(IConfigurationRoot config)
		{
			var launchpad = new Launchpad.Cache();

			var project = await launchpad.GetProject(config.GetSection("launchpad")["project"]);
			Console.WriteLine("Project: {0}", project.Name);

			var milestones = await project.GetActiveMilestones();
			foreach (var milestone in milestones)
			{
				Console.WriteLine("Milestone: {0}", milestone.Name);

				var milestoneSpecifications = await milestone.GetSpecifications();
				foreach (var specification in milestoneSpecifications)
					Console.WriteLine("    Specification: {0} ({1}, {2}, {3}, {4}, {5})",
						specification.Name,
						specification.LifecycleStatus,
						specification.Priority,
						specification.DirectionApproved,
						specification.DefinitionStatus,
						specification.ImplementationStatus);
			}
		}
	}
}
