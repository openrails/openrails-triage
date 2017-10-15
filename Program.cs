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

			var launchpad = new Launchpad.Cache();
			var launchpadConfig = config.GetSection("launchpad");
			var project = await launchpad.GetProject(launchpadConfig["projectUrl"]);

			var launchpadCommitsConfig = launchpadConfig.GetSection("commits");
			var launchpadCommits = git.GetLog(gitConfig["branch"], DateTimeOffset.Parse(launchpadCommitsConfig["startDate"]));

			CommitLog(commits, gitConfig);
			CommitTriage(commits, gitConfig);
			await SpecificationTriage(project, launchpadConfig, launchpadCommits);
			await SpecificationApprovals(project);
		}

		static string GetGitPath()
		{
			var appFilePath = System.Reflection.Assembly.GetEntryAssembly().Location;
			return Path.Combine(Path.GetDirectoryName(appFilePath), "git");
		}

		static void CommitLog(List<Commit> commits, IConfigurationSection gitConfig)
		{
			Console.WriteLine("Commit log");
			Console.WriteLine("==========");
			Console.WriteLine();

			var webUrlConfig = gitConfig.GetSection("webUrl");
			foreach (var commit in commits)
			{
				Console.WriteLine(
					$"- [{commit.Summary}]({webUrlConfig["commit"].Replace("%KEY%", commit.Key)}) **at** {commit.AuthorDate} **by** {commit.AuthorName}"
				);
				Console.WriteLine();
			}
		}

		static void CommitTriage(List<Commit> commits, IConfigurationSection gitConfig)
		{
			Console.WriteLine("Commit triage");
			Console.WriteLine("=============");
			Console.WriteLine();

			var webUrlConfig = gitConfig.GetSection("webUrl");
			var commitMessagesConfig = gitConfig.GetSection("commitMessages");
			var forms = commitMessagesConfig.GetSection("expectedForms").GetChildren();
			foreach (var commit in commits)
			{
				if (!forms.Any(form => Regex.IsMatch(commit.Message, form.Value, RegexOptions.IgnoreCase)))
				{
					Console.WriteLine(
						$"- [{commit.Summary}]({webUrlConfig["commit"].Replace("%KEY%", commit.Key)}) **at** {commit.AuthorDate} **by** {commit.AuthorName}\n" +
						$"  - **Issue:** {commitMessagesConfig["error"]}"
					);
					Console.WriteLine();
				}
			}
		}

		static async Task SpecificationTriage(Launchpad.Project project, IConfigurationSection config, List<Commit> commits)
		{
			Console.WriteLine("Specification triage");
			Console.WriteLine("====================");
			Console.WriteLine();

			var commitsConfig = config.GetSection("commits");
			var commitReferencesConfig = commitsConfig.GetSection("references");
			var commitReferencesSource = commitReferencesConfig.GetSection("source").GetChildren();

			foreach (var specification in await project.GetSpecifications())
			{
				var milestone = await specification.GetMilestone();

				var issues = new List<string>();
				if (specification.Direction == Direction.Approved
					&& specification.Priority <= Priority.Undefined)
				{
					issues.Add("Direction is approved but priority is missing");
				}
				if (specification.Definition == Definition.Approved
					&& specification.Direction != Direction.Approved)
				{
					issues.Add("Definition is approved but direction is not approved");
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
							issues.Add($"Definition is approved but no {link.Key} link is found");
						}
						else if (!forms.Any(form => specification.Summary.Contains(form.Value)))
						{
							issues.Add($"Definition is approved not no normal {link.Key} link is found");
						}
					}
				}
				if (specification.Definition == Definition.Approved
					&& !specification.HasApprover)
				{
					issues.Add("Definition is approved but approver is missing");
				}
				if (specification.Definition <= Definition.Drafting
					&& !specification.HasDrafter)
				{
					issues.Add("Definition is drafting (or later) but drafter is missing");
				}
				if (specification.Implementation >= Implementation.Started
					&& specification.Definition != Definition.Approved)
				{
					issues.Add("Implementation is started (or later) but definition is not approved");
				}
				if (specification.Implementation >= Implementation.Started
					&& !specification.HasAssignee)
				{
					issues.Add("Implementation is started (or later) but assignee is missing");
				}
				if (specification.Implementation == Implementation.Implemented
					&& !specification.HasMilestone)
				{
					issues.Add("Implementation is completed but milestone is missing");
				}
				var commitMentions = commits.Where(commit => commit.Message.Contains(specification.Json.web_link));
				if (specification.Whiteboard != null)
				{
					foreach (var referenceSource in commitReferencesSource)
					{
						var match = Regex.Match(specification.Whiteboard, referenceSource.Value, RegexOptions.IgnoreCase);
						while (match.Success)
						{
							var target = commitReferencesConfig["target"].Replace("%1", match.Groups[1].Value);
							commitMentions = commitMentions.Union(commits.Where(commit => commit.Message.Contains(target)));
							match = match.NextMatch();
						}
					}
				}
				if (commitMentions.Any())
				{
					if (milestone != null
						&& milestone.Id != commitsConfig["currentMilestone"])
					{
						issues.Add("Code was committed but milestone is incorrect");
					}
					if (specification.Definition != Definition.Approved)
					{
						issues.Add("Code was committed but definition is not approved");
					}
					var latestCommit = commitMentions.OrderBy(commit => commit.AuthorDate).Last();
					if ((DateTimeOffset.Now - latestCommit.AuthorDate).TotalDays > 28
						&& specification.Implementation != Implementation.Implemented)
					{
						issues.Add("Code was committed more than 28 days ago but implementation is not complete");
					}
				}
				else
				{
					if (specification.Implementation == Implementation.Implemented
						&& milestone != null
						&& milestone.Id == commitsConfig["currentMilestone"])
					{
						issues.Add("No code was committed but implementation is complete and for current milestone");
					}
				}
				if (issues.Count > 0)
				{
					Console.WriteLine(
						$"- [{specification.Name} ({milestone?.Name})]({specification.Json.web_link})\n" +
						$"  - **Status:** {specification.Lifecycle}, {specification.Priority}, {specification.Direction}, {specification.Definition}, {specification.Implementation}\n" +
						String.Join("\n", issues.Select(issue => $"  - **Issue:** {issue}"))
					);
					Console.WriteLine();
				}
			}
		}

		static async Task SpecificationApprovals(Launchpad.Project project)
		{
			Console.WriteLine("Specification approvals");
			Console.WriteLine("=======================");
			Console.WriteLine();

			foreach (var specification in await project.GetValidSpecifications())
			{
				var milestone = await specification.GetMilestone();

				if (specification.Direction != Direction.Approved)
				{
					Console.WriteLine(
						$"- [{specification.Name} ({milestone?.Name})]({specification.Json.web_link})\n" +
						$"  - **Status:** {specification.Lifecycle}, {specification.Priority}, {specification.Direction}, {specification.Definition}, {specification.Implementation}"
					);
					Console.WriteLine();
				}
			}
		}
	}
}
