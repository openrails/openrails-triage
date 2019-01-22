using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Open_Rails_Triage.Git
{
	public class Commit
	{
		public string Key { get; private set; }
		public List<string> ParentKeys { get; } = new List<string>();
		public string AuthorName { get; private set; }
		public string AuthorEmail { get; private set; }
		public DateTimeOffset AuthorDate { get; private set; }
		public string CommitterName { get; private set; }
		public string CommitterEmail { get; private set; }
		public DateTimeOffset CommitterDate { get; private set; }
		public string Message { get; private set; }
		public string Summary => Message.Split('\n')[0];
		public List<Commit> Commits { get; } = new List<Commit>();

		internal Commit(string key) => (Key, Message) = (key, "");

		public static List<Commit> Parse(IEnumerable<string> lines)
		{
			var commits = new List<Commit>();
			Commit commit = null;
			foreach (var line in lines.Select(line => line.Replace("\0", "")))
			{
				if (line.Length == 40 && line.IndexOf(' ') == -1)
				{
					commit = new Commit(line);
					commits.Add(commit);
				}
				else if (line.StartsWith("parent "))
				{
					commit.ParentKeys.Add(line.Substring(7));
				}
				else if (line.StartsWith("author "))
				{
					(commit.AuthorName, commit.AuthorEmail, commit.AuthorDate) = ParseUser(line);
				}
				else if (line.StartsWith("committer "))
				{
					(commit.CommitterName, commit.CommitterEmail, commit.CommitterDate) = ParseUser(line);
				}
				else if (line.StartsWith("    "))
				{
					if (commit.Message.Length > 0)
					{
						commit.Message += "\n";
					}
					commit.Message += line.Substring(4);
				}
			}
			return commits;
		}

		static ValueTuple<string, string, DateTimeOffset> ParseUser(string line)
		{
			var typeName = line.IndexOf(" ");
			Debug.Assert(typeName != -1);
			var nameEmail = line.IndexOf(" <", typeName + 1);
			Debug.Assert(nameEmail != -1);
			var emailDate = line.IndexOf("> ", nameEmail + 2);
			Debug.Assert(emailDate != -1);
			return (
				line.Substring(typeName + 1, nameEmail - typeName - 1),
				line.Substring(nameEmail + 2, emailDate - nameEmail - 2),
				ParseDate(line.Substring(emailDate + 2))
			);
		}

		static DateTimeOffset ParseDate(string dateAndOffset)
		{
			var dateOffset = dateAndOffset.IndexOf(" ");
			Debug.Assert(dateOffset != -1);
			Debug.Assert(dateAndOffset.Length - dateOffset == 6);
			var unixTime = long.Parse(dateAndOffset.Substring(0, dateOffset));
			var offsetDir = dateAndOffset[dateOffset + 1] == '+' ? 1 : -1;
			var offsetHour = int.Parse(dateAndOffset.Substring(dateOffset + 2, 2));
			var offsetMinute = int.Parse(dateAndOffset.Substring(dateOffset + 4, 2));
			var offset = new TimeSpan(offsetDir * offsetHour, offsetDir * offsetMinute, 0);
			return DateTimeOffset.FromUnixTimeSeconds(unixTime - (long)offset.TotalSeconds).ToOffset(offset);
		}
	}
}
