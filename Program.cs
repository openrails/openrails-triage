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
				ForcedDefaultValue = new FileInfo("config.json")
			};

			var verbose = new CommandLineParser.Arguments.SwitchArgument('v', "verbose", false);

			var commandLineParser = new CommandLineParser.CommandLineParser()
			{
				Arguments = {
					config,
					verbose,
				}
			};

			try
			{
				commandLineParser.ParseCommandLine(args);

				AsyncMain(new ConfigurationBuilder()
					.AddJsonFile(config.Value.FullName, true)
					.AddJsonFile(config.Value.FullName.Replace(".json", "-secret.json"), true)
					.Build(), verbose.Value).Wait();
			}
			catch (CommandLineParser.Exceptions.CommandLineException e)
			{
				Console.WriteLine(e.Message);
			}
		}

		static async Task AsyncMain(IConfigurationRoot config, bool verbose)
		{
			var gitConfig = config.GetSection("git");
			var launchpadConfig = config.GetSection("launchpad");
			var launchpadCommitsConfig = launchpadConfig.GetSection("commits");
			var trelloConfig = config.GetSection("trello");

			var git = new Git.Project(GetGitPath(), verbose);
			git.Init(gitConfig["projectUrl"]);
			git.Fetch();
			var launchpad = new Launchpad.Cache();
			var launchpadProject = await launchpad.GetProject(launchpadConfig["projectUrl"]);
			var launchpadCommits = git.GetLog(gitConfig["branch"], DateTimeOffset.Parse(launchpadCommitsConfig["startDate"]));

			CommitTriage(launchpadCommits, gitConfig);
			await BugTriage(launchpadProject, launchpadConfig, launchpadCommits);
			await SpecificationTriage(launchpadProject, launchpadConfig, launchpadCommits);
			await SpecificationApprovals(launchpadProject);

			var trello = new Trello.Cache(trelloConfig["key"], trelloConfig["token"]);
			var board = await trello.GetBoard(trelloConfig["board"]);
			await RoadmapTriage(board, trelloConfig);
		}

		static string GetGitPath()
		{
			var appFilePath = System.Reflection.Assembly.GetEntryAssembly().Location;
			return Path.Combine(Path.GetDirectoryName(appFilePath), "git");
		}

		static void CommitTriage(List<Commit> commits, IConfigurationSection gitConfig)
		{
			Console.WriteLine("Commit triage");
			Console.WriteLine("=============");
			Console.WriteLine();

			var webUrlConfig = gitConfig.GetSection("webUrl");
			var commitMessagesConfig = gitConfig.GetSection("commitMessages");
			var forms = GetConfigPatternMatchers(commitMessagesConfig.GetSection("expectedForms"));
			foreach (var commit in commits)
			{
				if (!forms.Any(pattern => pattern(commit.Message)))
				{
					Console.WriteLine(
						$"- [{commit.Summary}]({webUrlConfig["commit"].Replace("%KEY%", commit.Key)}) **at** {commit.AuthorDate} **by** {commit.AuthorName}\n" +
						$"  - **Issue:** {commitMessagesConfig["error"]}"
					);
					Console.WriteLine();
				}
			}
		}

		static async Task BugTriage(Launchpad.Project project, IConfigurationSection config, List<Commit> commits)
		{
			Console.WriteLine("Bug triage");
			Console.WriteLine("==========");
			Console.WriteLine();

			var bugsConfig = config.GetSection("bugs");
			var scanAttachments = GetConfigPatternMatchers(bugsConfig.GetSection("scanAttachments"));
			var commitReferencesConfig = config.GetSection("commits").GetSection("bugReferences");
			var commitReferencesSource = GetConfigPatternValueMatchers(commitReferencesConfig.GetSection("source"));

			foreach (var bugTask in await project.GetRecentBugTasks(DateTimeOffset.Parse(bugsConfig["startDate"])))
			{
				var bug = await bugTask.GetBug();
				var milestone = await bugTask.GetMilestone();
				var attachments = await bug.GetAttachments();
				var attachmentLogs = await Task.WhenAll(attachments
					.Where(attachment => attachment.Type != Launchpad.Type.Patch
						&& scanAttachments.Any(pattern => pattern(attachment.Name)))
					.Select(async attachment => await attachment.GetData()));

				var issues = new List<string>();

				var idealTitles = new List<string>();
				if (bugsConfig["scanDescriptions"] == "True")
				{
					foreach (var message in await bug.GetMessages())
					{
						var idealTitle = GetBugIdealTitle(bugsConfig.GetSection("idealTitle"), message.Description);
						if (idealTitle.Length > 0)
						{
							idealTitles.Add(idealTitle);
						}
					}
				}
				foreach (var attachmentLog in attachmentLogs)
				{
					var idealTitle = GetBugIdealTitle(bugsConfig.GetSection("idealTitle"), attachmentLog);
					if (idealTitle.Length > 0)
					{
						idealTitles.Add(idealTitle);
					}
				}
				var idealTagsTitle = bug.Name;
				if (idealTitles.Count >= 1)
				{
					if (!bug.Name.Contains(idealTitles[0]))
					{
						issues.Add($"Ideal title: {idealTitles[0]}");
						idealTagsTitle = idealTitles[0];
					}
				}

				var allIdealTags = new SortedSet<string>();
				foreach (var idealTagConfig in bugsConfig.GetSection("idealTags").GetChildren())
				{
					if (Regex.IsMatch(idealTagsTitle, idealTagConfig.Key))
					{
						var knownTags = new SortedSet<string>();
						foreach (var knownTag in idealTagConfig["knownTags"].Split(' '))
						{
							knownTags.Add(knownTag);
						}

						var idealTags = GetBugIdealTags(idealTagConfig, idealTagsTitle);
						allIdealTags.UnionWith(idealTags);

						foreach (var tag in idealTags.Where(tag => !bug.Tags.Contains(tag)))
						{
							issues.Add($"Missing known tag {tag}");
						}
						foreach (var tag in knownTags.Where(tag => bug.Tags.Contains(tag) && !idealTags.Contains(tag)))
						{
							issues.Add($"Extra known tag {tag}");
						}
					}
				}

				foreach (var idealStatusConfig in bugsConfig.GetSection("idealStatus").GetChildren())
				{
					var statusMatch = IsValuePresentMissing(idealStatusConfig.GetSection("status"), bugTask.Status.ToString());
					var tagsMatch = IsValuePresentMissing(idealStatusConfig.GetSection("tags"), allIdealTags.ToArray());
					if (statusMatch && tagsMatch && bugTask.Status.ToString() != idealStatusConfig.Key)
					{
						issues.Add($"Status should be {idealStatusConfig.Key}");
					}
				}

				var commitMentions = commits.Where(commit => commit.Message.Contains(bugTask.Json.web_link));
				foreach (var message in await bug.GetMessages())
				{
					foreach (var referenceSource in commitReferencesSource)
					{
						var match = referenceSource(message.Description);
						if (match != "")
						{
							var target = commitReferencesConfig["target"].Replace("%1", match);
							commitMentions = commitMentions.Union(commits.Where(commit => commit.Message.Contains(target)));
						}
					}
				}
				if (commitMentions.Any())
				{
					if (bugTask.Status < Status.InProgress)
					{
						issues.Add("Code was committed but bug is not in progress or fixed");
					}
					var latestCommit = commitMentions.OrderBy(commit => commit.AuthorDate).Last();
					if ((DateTimeOffset.Now - latestCommit.AuthorDate).TotalDays > 28
						&& bugTask.Status < Status.FixCommitted)
					{
						issues.Add("Code was committed exclusively more than 28 days ago but bug is not fixed");
					}
				}
				else
				{
					if (bugTask.Status >= Status.FixCommitted)
					{
						issues.Add("No code was committed but bug is fixed");
					}
				}

				WriteBugIssues(bugTask, bug, milestone, issues);
			}

			foreach (var bugTask in await project.GetUnreleasedBugTasks())
			{
				var bug = await bugTask.GetBug();
				var milestone = await bugTask.GetMilestone();

				var issues = new List<string>();

				if (bugTask.Status == Status.InProgress && !bugTask.HasAssignee)
				{
					issues.Add("No assignee set but bug is in progress");
				}
				else if (bugTask.Status >= Status.FixCommitted && !bugTask.HasAssignee)
				{
					issues.Add("No assignee set but bug is fixed");
				}

				if (bugTask.Status >= Status.FixCommitted && milestone == null)
				{
					issues.Add("No milestone set but bug is fixed");
				}

				WriteBugIssues(bugTask, bug, milestone, issues);
			}

			foreach (var bugTask in await project.GetIncompleteBugTasks())
			{
				var bug = await bugTask.GetBug();
				var milestone = await bugTask.GetMilestone();
				var messages = await bug.GetMessages();

				var issues = new List<string>();

				var incompleteMessages = messages.Where(m => m.Created >= bugTask.Incomplete).ToList();
				if (incompleteMessages.Count > 0)
				{
					var lastMessage = incompleteMessages.Last();
					var diff = lastMessage.Created - bugTask.Incomplete;
					if (diff.TotalMinutes >= 1)
					{
						var lastMessageUser = await lastMessage.GetOwner();
						var lastMessageAge = DateTimeOffset.Now - lastMessage.Created;
						issues.Add($"{incompleteMessages.Count} messages added since incomplete status was set; last message was by {lastMessageUser.Name}, {lastMessageAge.TotalDays:N1} days ago");
					}
				}

				WriteBugIssues(bugTask, bug, milestone, issues);
			}
		}

		static void WriteBugIssues(BugTask bugTask, Bug bug, Milestone milestone, List<string> issues)
		{
			if (issues.Count > 0)
			{
				Console.WriteLine(
					$"- [{bug.Name} ({String.Join(", ", bug.Tags)})]({bugTask.Json.web_link})\n" +
					$"  - **Status:** {bugTask.Status}, {bugTask.Importance}, {milestone?.Name}\n" +
					String.Join("\n", issues.Select(issue => $"  - **Issue:** {issue}"))
				);
				Console.WriteLine();
			}
		}

		static string GetBugIdealTitle(IConfigurationSection config, string log)
		{
			var versionPattern = new Regex(config["version"]);
			var routePattern = new Regex(config["route"]);
			var activityPattern = new Regex(config["activity"]);
			var errorPattern = new Regex(config["error"]);
			var exceptionPattern = new Regex(config["exception"]);
			var stackPattern = new Regex(config["stack"]);

			var lines = log.Split('\r', '\n');

			// Find the first error match, or first warning match if no errors.
			var errorLineIndex = -1;
			for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
			{
				if (errorPattern.IsMatch(lines[lineIndex]))
				{
					errorLineIndex = lineIndex;
					break;
				}
			}
			if (errorLineIndex == -1)
			{
				return "";
			}

			var exceptionMatch = exceptionPattern.Match(lines[errorLineIndex]);
			var idealTitle = exceptionMatch.Groups[1].Value;

			// Find a good stack trace line to use as the source location
			var maxStackLines = uint.Parse(config["maxStackLines"]);
			for (var lineIndex = errorLineIndex + 1; lineIndex < errorLineIndex + maxStackLines && lineIndex < lines.Length; lineIndex++)
			{
				var match = stackPattern.Match(lines[lineIndex]);
				if (match.Success)
				{
					idealTitle += " at " + match.Groups[1].Value;
					break;
				}
			}

			// Exclude some known non-issues
			if (config.GetSection("excludes")[idealTitle] != null)
			{
				return "";
			}

			// Look for common metadata about the run
			var meta = new List<string>();
			for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
			{
				var versionMatch = versionPattern.Match(lines[lineIndex]);
				if (versionMatch.Success)
				{
					meta.Add(versionMatch.Groups[1].Value);
				}

				var routeMatch = routePattern.Match(lines[lineIndex]);
				if (routeMatch.Success)
				{
					meta.Add(routeMatch.Groups[1].Value);
				}

				var activityMatch = activityPattern.Match(lines[lineIndex]);
				if (activityMatch.Success)
				{
					meta.Add(activityMatch.Groups[1].Value);
				}
			}
			if (meta.Count > 0)
			{
				if (idealTitle.Length > 0)
				{
					idealTitle = $"{idealTitle} ({String.Join(", ", meta)}";
				}
				else
				{
					idealTitle = String.Join(", ", meta);
				}
			}

			return idealTitle;
		}

		static SortedSet<string> GetBugIdealTags(IConfigurationSection config, string title)
		{
			var tags = new SortedSet<string>();

			foreach (var tag in config.GetChildren())
			{
				if (IsValuePatternMatch(title, tag))
				{
					var matchesException = false;
					foreach (var exceptionTag in tag.GetSection("exceptions").GetChildren())
					{
						if (IsValuePatternMatch(title, exceptionTag))
						{
							tags.Add(exceptionTag.Key.Replace("a-", "").Replace("z-", ""));
							matchesException = true;
							break;
						}
					}
					if (!matchesException)
					{
						tags.Add(tag.Key.Replace("a-", "").Replace("z-", ""));
					}
				}
			}

			tags.RemoveWhere(tag => tag.Length == 0);

			return tags;
		}

		static async Task SpecificationTriage(Launchpad.Project project, IConfigurationSection config, List<Commit> commits)
		{
			Console.WriteLine("Specification triage");
			Console.WriteLine("====================");
			Console.WriteLine();

			var commitsConfig = config.GetSection("commits");
			var commitReferencesConfig = commitsConfig.GetSection("specificationReferences");
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
					if ((!hasStartDate || specification.Created >= startDate)
						&& (milestone == null || startMilestone == null || string.Compare(milestone.Id, startMilestone) >= 0)
						&& specification.Definition == Definition.Approved
						&& specification.Implementation != Implementation.Informational)
					{
						if (!specification.Summary.Contains(link["baseUrl"]))
						{
							issues.Add($"Definition is approved but no {link.Key} link is found");
						}
						else if (!forms.Any(form => specification.Summary.Contains(form.Value)))
						{
							issues.Add($"Definition is approved but no normal {link.Key} link is found");
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
					&& specification.Definition != Definition.Approved
					&& specification.Definition <= Definition.New)
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
						issues.Add($"Code was committed but milestone is {milestone.Id} (expected missing/{commitsConfig["currentMilestone"]})");
					}
					if (specification.Definition != Definition.Approved
						&& specification.Definition <= Definition.New)
					{
						issues.Add("Code was committed but definition is not approved");
					}
					var latestCommit = commitMentions.OrderBy(commit => commit.AuthorDate).Last();
					if ((DateTimeOffset.Now - latestCommit.AuthorDate).TotalDays > 28
						&& specification.Implementation != Implementation.Implemented)
					{
						issues.Add("Code was committed exclusively more than 28 days ago but implementation is not complete");
					}
				}
				else
				{
					if (specification.Implementation == Implementation.Implemented
						&& milestone != null
						&& milestone.Id == commitsConfig["currentMilestone"])
					{
						issues.Add("No code was committed but implementation for current milestone is complete");
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

		static async Task RoadmapTriage(Trello.Board board, IConfigurationSection config)
		{
			Console.WriteLine("Roadmap triage");
			Console.WriteLine("==============");
			Console.WriteLine();

			var lists = Filter(await board.GetLists(), list => list.Name, config["includeLists"], config["excludeLists"]);

			foreach (var list in lists)
			{
				Console.WriteLine($"- {list.Name}");

				var cards = Filter(await list.GetCards(), card => card.Name, config["includeCards"], config["excludeCards"]);
				if (config["sorting"] == "votes")
				{
					for (var i = 1; i < cards.Count; i++)
					{
						if (cards[i].Votes > cards[i - 1].Votes)
						{
							Console.WriteLine($"  - [{cards[i].Name}]({cards[i].Uri}): has more votes than card above ({cards[i].Votes} vs {cards[i - 1].Votes})");
						}
					}
				}
			}
		}

		static IEnumerable<Func<string, bool>> GetConfigPatternMatchers(IConfigurationSection config)
		{
			var patterns = config.GetChildren()
				.Select(pattern => new Regex(pattern.Value, RegexOptions.IgnoreCase))
				.ToList();

			return patterns.Select<Regex, Func<string, bool>>(pattern => test => pattern.IsMatch(test));
		}

		static IEnumerable<Func<string, string>> GetConfigPatternValueMatchers(IConfigurationSection config)
		{
			var patterns = config.GetChildren()
				.Select(pattern => new Regex(pattern.Value, RegexOptions.IgnoreCase))
				.ToList();

			return patterns.Select<Regex, Func<string, string>>(pattern => test => pattern.Match(test)?.Groups?[1].Value);
		}

		static bool IsValuePatternMatch(string value, IConfigurationSection config)
		{
			if (config.Value == "True")
			{
				return true;
			}
			foreach (var subConfig in config.GetChildren())
			{
				if (subConfig.Value != null && value.Contains(subConfig.Value))
				{
					return true;
				}
			}
			return false;
		}

		static bool IsValuePresentMissing(IConfigurationSection config, params string[] values)
		{
			if (config["allPresent"] != null)
			{
				var allPresent = config["allPresent"].Split(' ');
				if (allPresent.Any(value => !values.Contains(value)))
				{
					return false;
				}
			}

			if (config["anyPresent"] != null)
			{
				var anyPresent = config["anyPresent"].Split(' ');
				if (!anyPresent.Any(value => values.Contains(value)))
				{
					return false;
				}
			}

			if (config["allMissing"] != null)
			{
				var allMissing = config["allMissing"].Split(' ');
				if (allMissing.Any(value => values.Contains(value)))
				{
					return false;
				}
			}

			if (config["anyMissing"] != null)
			{
				var anyMissing = config["anyMissing"].Split(' ');
				if (!anyMissing.Any(value => !values.Contains(value)))
				{
					return false;
				}
			}

			return true;
		}

		static List<T> Filter<T>(List<T> items, Func<T, string> field, string include, string exclude)
		{
			if (include != null)
			{
				var filter = new Regex(include);
				items = items.Where(item => filter.IsMatch(field(item))).ToList();
			}
			if (exclude != null)
			{
				var filter = new Regex(exclude);
				items = items.Where(item => !filter.IsMatch(field(item))).ToList();
			}
			return items;
		}
	}
}
