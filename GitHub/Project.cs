using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Open_Rails_Triage.GitHub
{
	public class Project
	{
		const string COMMIT_PULL_REQUEST_MERGE_PREFIX = "Merge pull request #";

		IConfigurationSection Config;
		Query Query;

		public bool IsEnabled { get => Config["token"] != null; }

		public Project(IConfigurationSection config)
		{
			Config = config;
			Query = new Query(Config["token"]);
		}

		public bool IsPullRequestMerge(Git.Commit commit)
		{
			if (!IsEnabled) return false;
			return commit.Message.StartsWith(COMMIT_PULL_REQUEST_MERGE_PREFIX);
		}

		public async Task<GraphPullRequest> GetPullRequest(Git.Commit commit)
		{
			if (!IsPullRequestMerge(commit)) return null;

			var text = commit.Message.Substring(COMMIT_PULL_REQUEST_MERGE_PREFIX.Length).Split(' ');

			if (!int.TryParse(text[0], out var number)) return null;

			return await Query.GetPullRequest(Config["organization"], Config["repository"], number);
		}
	}
}
