using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Open_Rails_Triage
{
	class References : Dictionary<string, Reference>
	{
		public static readonly Regex ReferencesPattern = new("(https://bugs\\.launchpad\\.net/[^/]+/\\+bug/[0-9]+|https://blueprints\\.launchpad\\.net/[^/]+/\\+spec/[0-9a-z-]+|https://trello\\.com/c/[0-9a-zA-Z]+)");

		public static string GetReferenceType(string reference) => reference switch
		{
			string a when a.Contains("//bugs.launchpad.net/") => "launchpad-bug",
			string a when a.Contains("//blueprints.launchpad.net/") => "launchpad-blueprint",
			string a when a.Contains("//trello.com/c/") => "trello-card",
			_ => "unknown"
		};

		public void Add(Git.Commit commit, out HashSet<string> types)
		{
			types = new();
			foreach (var match in commit.Commits.Select(commit => ReferencesPattern.Matches(commit.Message)).Append(ReferencesPattern.Matches(commit.Message)).SelectMany(match => match).Select(match => match.Value))
			{
				types.Add(GetReferenceType(match));
				GetReference(match).GitCommits.Add(commit);
			}
		}

		public void Add(Launchpad.Bug bug, out HashSet<string> types)
		{
			types = new();
			foreach (var match in ReferencesPattern.Matches(bug.Description).Select(match => match.Value))
			{
				types.Add(GetReferenceType(match));
				GetReference(match).LaunchpadBugs.Add(bug);
			}
		}

		Reference GetReference(string key)
		{
			if (!ContainsKey(key)) this[key] = new();
			return this[key];
		}
	}

	class Reference
	{
		public List<Git.Commit> GitCommits { get; } = new();
		public List<Launchpad.Bug> LaunchpadBugs { get; } = new();
	}
}
