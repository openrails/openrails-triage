using System;
using System.Diagnostics;
using System.IO;

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

		void RunCommand(string command)
		{
			Console.WriteLine($"[{GitPath}]");
			Console.WriteLine($"> git {command}");
			var git = Process.Start(new ProcessStartInfo()
			{
				WorkingDirectory = GitPath,
				FileName = "git",
				Arguments = command,
			});
			git.WaitForExit();
			Debug.Assert(git.ExitCode == 0, $"git {command} failed: {git.ExitCode}");
		}
	}
}
