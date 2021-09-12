using System;

namespace Open_Rails_Triage.GitHub
{
	public class GraphPullRequest
	{
		public Uri Url;
		public int Number;
		public string Title;
		public string Body;
		public GraphPullRequestLabels Labels;
		public int Additions;
		public int Deletions;
	}

	public class GraphPullRequestLabels
	{
		public GraphPullRequestLabelNode[] Nodes;
	}

	public class GraphPullRequestLabelNode
	{
		public string Name;
	}
}
