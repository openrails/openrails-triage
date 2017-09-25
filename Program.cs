using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Open_Rails_Triage.Git;
using Open_Rails_Triage.Launchpad;

namespace Open_Rails_Triage
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

				AsyncMain(new ConfigurationBuilder()
					.AddJsonFile(config.Value.FullName, true)
					.Build()).Wait();
			}
			catch (CommandLineParser.Exceptions.CommandLineException e)
			{
				Console.WriteLine(e.Message);
			}
		}

		static async Task AsyncMain(IConfigurationRoot config)
		{
			var gitConfig = config.GetSection("git");
			var git = new Git.Project(GetGitPath());
			git.Init(gitConfig["projectUrl"]);
			git.Fetch();
			var commits = git.GetLog(gitConfig["branch"], DateTimeOffset.Now.AddDays(-7));
			Console.WriteLine();
			CommitTriage(commits, gitConfig);

			var launchpad = new Launchpad.Cache();
			var launchpadConfig = config.GetSection("launchpad");
			var project = await launchpad.GetProject(launchpadConfig["projectUrl"]);
			await SpecificationTriage(project, launchpadConfig);
		}

		static string GetGitPath()
		{
			var appFilePath = System.Reflection.Assembly.GetEntryAssembly().Location;
			return Path.Combine(Path.GetDirectoryName(appFilePath), "git");
		}

		static void CommitTriage(List<Commit> commits, IConfigurationSection gitConfig)
		{
			var commitMessagesConfig = gitConfig.GetSection("commitMessages");
			var forms = commitMessagesConfig.GetSection("expectedForms").GetChildren();
			foreach (var commit in commits)
			{
				if (!forms.Any(form => Regex.IsMatch(commit.Message, form.Value, RegexOptions.IgnoreCase)))
				{
					Console.WriteLine(
						$"Commit '{commit.Summary}'\n" +
						$"  On {commit.AuthorDate} by {commit.AuthorName}\n" +
						$"  Issue: {commitMessagesConfig["error"]}"
					);
					Console.WriteLine();
				}
			}
		}

		static async Task SpecificationTriage(Launchpad.Project project, IConfigurationSection config)
		{
			foreach (var specification in await project.GetSpecifications())
			{
				var milestone = await specification.GetMilestone();

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
				foreach (var link in config.GetSection("links").GetChildren())
				{
					var hasStartDate = DateTimeOffset.TryParse(link["startDate"] ?? "", out var startDate);
					var startMilestone = link["startMilestone"];
					var forms = link.GetSection("expectedForms").GetChildren();
					if ((!hasStartDate || specification.Created > startDate)
						&& (milestone == null || startMilestone == null || string.Compare(milestone.Id, startMilestone) > 0)
						&& specification.Definition == Definition.Approved
						&& specification.Implementation != Implementation.Informational)
					{
						if (!specification.Summary.Contains(link["baseUrl"]))
						{
							issues.Add($"Definition approved without {link.Key} link");
						}
						else if (!forms.Any(form => specification.Summary.Contains(form.Value)))
						{
							issues.Add($"Definition approved without normal {link.Key} link");
						}
					}
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
						$"Blueprint '{specification.Name}' ({milestone?.Name})\n" +
						$"  Status: {specification.Lifecycle} / {specification.Priority} / {specification.Direction} / {specification.Definition} / {specification.Implementation}\n" +
						String.Join("\n", issues.Select(issue => $"  Issue: {issue}"))
					);
					Console.WriteLine();
				}
			}
		}
	}
}
