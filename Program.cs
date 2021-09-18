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
			var gitHubConfig = config.GetSection("github");
			var launchpadConfig = config.GetSection("launchpad");

			var git = new Git.Project(GetGitPath(), verbose);
			git.Init(gitConfig["projectUrl"]);
			git.Fetch();
			var commits = git.GetLog(gitConfig["branch"], DateTimeOffset.Parse(gitConfig["startDate"]));

			var gitHub = new GitHub.Project(gitHubConfig);

			var launchpad = new Launchpad.Cache();
			var launchpadProject = await launchpad.GetProject(launchpadConfig["projectUrl"]);

			await CommitTriage(commits, gitConfig, gitHub);
			await BugTriage(launchpadProject, launchpadConfig, commits);
			await SpecificationTriage(launchpadProject, launchpadConfig, commits);
			await SpecificationApprovals(launchpadProject);
		}

		static string GetGitPath()
		{
			var appFilePath = System.Reflection.Assembly.GetEntryAssembly().Location;
			return Path.Combine(Path.GetDirectoryName(appFilePath), "git");
		}

		static async Task CommitTriage(List<Commit> commits, IConfigurationSection gitConfig, GitHub.Project gitHub)
		{
			Console.WriteLine("Commit triage");
			Console.WriteLine("=============");
			Console.WriteLine();

			var webUrlConfig = gitConfig.GetSection("webUrl");
			var exceptionalLabels = gitConfig.GetSection("references:exceptionalLabels").GetChildren().Select(item => item.Value);
			int.TryParse(gitConfig["references:minimumLines"], out var minimumLines);
			var requiredLabels = gitConfig.GetSection("references:requiredLabels").GetChildren().Select(node => node.Value);
			var referencePattern = new Regex(gitConfig["references:references"]);
			foreach (var commit in commits)
			{
				var pr = await gitHub.GetPullRequest(commit);
				if (pr != null)
				{
					if (pr.Labels.Nodes.Any(label => exceptionalLabels.Contains(label.Name))) continue;
					if (pr.Additions <= minimumLines && pr.Deletions <= minimumLines) continue;
				}
				var message = pr != null ? pr.Title + "\n" + pr.Body : commit.Message;
				var labels = pr?.Labels.Nodes.Select(n => n.Name);
				var references = referencePattern.Matches(message).Select(match => match.Value);

				commit.References.AddRange(references);

				var issues = new List<string>();

				if (labels != null && !requiredLabels.Any(label => labels.Contains(label)))
				{
					issues.Add("Missing required labels");
				}
				if (references.Count() == 0)
				{
					issues.Add("Missing required references");
				}

				if (issues.Count > 0)
				{
					Console.WriteLine($"- [{commit.Summary}]({webUrlConfig["commit"].Replace("%KEY%", commit.Key)}) {string.Join(", ", labels)} **at** {commit.AuthorDate} **by** {commit.AuthorName}");
					foreach (var issue in issues)
					{
						Console.WriteLine($"  - **Issue:** {issue}");
					}
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
			var duplicateMinWords = int.Parse(bugsConfig["duplicateMinWords"] ?? "1");

			var bugDuplicates = new Dictionary<string, (string Title, string Link)>();

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

				var duplicates = new List<(double Match, string Link)>();
				var duplicateTitleWords = idealTagsTitle.Split(" ");
				for (var i = duplicateTitleWords.Length; i >= duplicateMinWords; i--)
				{
					var duplicateTitle = String.Join(" ", duplicateTitleWords.Take(i));
					if (bugDuplicates.ContainsKey(duplicateTitle))
					{
						if (!duplicates.Any(d => d.Link == bugDuplicates[duplicateTitle].Link))
						{
							duplicates.Add((
								50d * duplicateTitle.Length / idealTagsTitle.Length
								+ 50d * duplicateTitle.Length / bugDuplicates[duplicateTitle].Title.Length,
								bugDuplicates[duplicateTitle].Link
							));
						}
					}
					else
					{
						bugDuplicates[duplicateTitle] = (bug.Name, GetBugLink(bugTask, bug));
					}
				}
				foreach (var duplicate in duplicates.OrderBy(d => -d.Match))
				{
					issues.Add($"Possible duplicate {duplicate.Match:F0}% - {duplicate.Link}");
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

				var commitMentions = commits.Where(commit => commit.References.Contains(bugTask.Json.web_link));
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
					$"- {GetBugLink(bugTask, bug)}\n" +
					$"  - **Status:** {bugTask.Status}, {bugTask.Importance}, {milestone?.Name}\n" +
					String.Join("\n", issues.Select(issue => $"  - **Issue:** {issue}"))
				);
				Console.WriteLine();
			}
		}

		static string GetBugLink(BugTask bugTask, Bug bug)
		{
			return $"[{bug.Name} ({String.Join(", ", bug.Tags)})]({bugTask.Json.web_link})";
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
				var commitMentions = commits.Where(commit => commit.References.Contains(specification.Json.web_link));
				if (commitMentions.Any())
				{
					if (milestone != null
						&& milestone.Id != config["currentMilestone"])
					{
						issues.Add($"Code was committed but milestone is {milestone.Id} (expected missing/{config["currentMilestone"]})");
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
						&& milestone.Id == config["currentMilestone"])
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
	}
}
