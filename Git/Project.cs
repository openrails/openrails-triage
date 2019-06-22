using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Open_Rails_Triage.Git
{
	public class Project
	{
		string GitPath;
		bool Verbose;

		public Project(string gitPath, bool verbose)
		{
			GitPath = gitPath;
			Verbose = verbose;
		}

		public void Init(string repository)
		{
			if (!Directory.Exists(GitPath))
			{
				Directory.CreateDirectory(GitPath);
				RunCommand($"clone --mirror {repository} .");
			}
		}

		public void Fetch()
		{
			RunCommand("fetch");
		}

		public List<Commit> GetLog(string branch, DateTimeOffset since)
		{
			var commits = Commit.Parse(GetCommandOutput($"rev-list --first-parent --header --since={since.ToUnixTimeSeconds()} {branch}"));
			commits.ForEach(commit => {
				commit.Commits.Clear();
				if (commit.ParentKeys.Count == 2)
				{
					commit.Commits.AddRange(Commit.Parse(GetCommandOutput($"rev-list --header {commit.ParentKeys[0]}..{commit.ParentKeys[1]}")));
				}
			});
			return commits;
		}

		void RunCommand(string command)
		{
			foreach (var line in GetCommandOutput(command))
			{
			}
		}

		IEnumerable<string> GetCommandOutput(string command)
		{
			var args = $"--no-pager {command}";
			if (Verbose) {
				Console.WriteLine("```shell");
				Console.WriteLine($"{GitPath}> git {args}");
			}
			var git = Process.Start(new ProcessStartInfo()
			{
				WorkingDirectory = GitPath,
				FileName = "git",
				Arguments = args,
				StandardOutputEncoding = Encoding.UTF8,
				RedirectStandardOutput = true,
			});
			while (!git.StandardOutput.EndOfStream)
			{
				yield return git.StandardOutput.ReadLine();
			}
			git.WaitForExit();
			Debug.Assert(git.ExitCode == 0, $"git {command} failed: {git.ExitCode}");
			if (Verbose) {
				Console.WriteLine("```");
			}
		}
	}
}
