using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Open_Rails_Roadmap_bot.Launchpad;

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

			await SpecificationTriage(project);
		}

		static async Task SpecificationTriage(Project project)
		{
			var discussionStartDate = new DateTimeOffset(2015, 9, 20, 0, 0, 0, TimeSpan.Zero);

			foreach (var specification in await project.GetSpecifications())
			{
				var issues = new List<string>();
				if (specification.Direction == Direction.Approved
					&& specification.Priority <= Priority.Undefined)
				{
					issues.Add("Direction approved without priority");
				}
				if (specification.Definition == Definition.Approved
					&& specification.Direction != Direction.Approved)
				{
					issues.Add("Definition approved without direction approved");
				}
				if (specification.Created > discussionStartDate
					&& specification.Definition == Definition.Approved
					&& specification.Implementation != Implementation.Informational
					&& !specification.Summary.Contains("http://www.elvastower.com/forums/index.php?/topic/"))
				{
					issues.Add("Definition approved without discussion link");
				}
				if (specification.Created > discussionStartDate
					&& specification.Definition == Definition.Approved
					&& specification.Implementation != Implementation.Informational
					&& specification.Summary.Contains("http://www.elvastower.com/forums/index.php?/topic/")
					&& !specification.Summary.Contains("Discussion: http://www.elvastower.com/forums/index.php?/topic/")
					&& !specification.Summary.Contains("Discussion (developers only): http://www.elvastower.com/forums/index.php?/topic/"))
				{
					issues.Add("Definition approved without normal discussion link");
				}
				if (specification.Created > discussionStartDate
					// TODO: Check milestone is > 1.1
					&& specification.Definition == Definition.Approved
					&& specification.Implementation != Implementation.Informational
					&& !specification.Summary.Contains("https://trello.com/c/"))
				{
					issues.Add("Definition approved without roadmap link");
				}
				if (specification.Created > discussionStartDate
					// TODO: Check milestone is > 1.1
					&& specification.Definition == Definition.Approved
					&& specification.Implementation != Implementation.Informational
					&& specification.Summary.Contains("https://trello.com/c/")
					&& !specification.Summary.Contains("Roadmap: https://trello.com/c/"))
				{
					issues.Add("Definition approved without normal roadmap link");
				}
				if (specification.Definition == Definition.Approved
					&& !specification.HasApprover)
				{
					issues.Add("Definition approved without approver");
				}
				if (specification.Definition <= Definition.Drafting
					&& !specification.HasDrafter)
				{
					issues.Add("Definition in drafting (or later) without drafter");
				}
				if (specification.Implementation >= Implementation.Started
					&& specification.Definition != Definition.Approved)
				{
					issues.Add("Implementation started without definition approved");
				}
				if (specification.Implementation >= Implementation.Started
					&& !specification.HasAssignee)
				{
					issues.Add("Implementation started without assignee");
				}
				if (specification.Implementation == Implementation.Implemented
					&& !specification.HasMilestone)
				{
					issues.Add("Implementation completed without milestone");
				}
				// Check for commits mentioning specification:
				//   If so, check specification has no or current milestone
				//   If so, check definition is approved
				//   If not, and it has no or current milestone, check implementation is not implemented
				if (issues.Count > 0)
				{
					Console.WriteLine(
						$"Blueprint '{specification.Name}'\n" +
						$"  Status: {specification.Lifecycle} / {specification.Priority} / {specification.Direction} / {specification.Definition} / {specification.Implementation}\n" +
						String.Join("\n", issues.Select(issue => $"  {issue}"))
					);
				}
			}
		}
	}
}
