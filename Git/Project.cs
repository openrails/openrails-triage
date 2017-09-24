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

		public Project(string gitPath)
		{
			GitPath = gitPath;
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
			return Commit.Parse(GetCommandOutput($"rev-list --header --since={since.ToUnixTimeSeconds()} {branch}"));
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
			Console.WriteLine($"[{GitPath}]");
			Console.WriteLine($"> git {args}");
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
		}
	}
}
