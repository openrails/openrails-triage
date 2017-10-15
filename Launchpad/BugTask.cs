using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Triage.Launchpad
{
	#pragma warning disable CS0649

	class JsonBugTaskCollection
	{
		public JsonBugTask[] entries;
		public string next_collection_link;
		public JsonBugTaskCollection(string url) => next_collection_link = url;
	}

	class JsonBugTask
	{
		public string self_link;
		public string title;
		public DateTimeOffset date_created;
		public string status;
		public string importance;
		public string assignee_link;
		public string milestone_link;
		public string bug_link;
		public string web_link;
	}

	#pragma warning restore CS0649

	public enum Status
	{
		Unknown,
		New,
		Incomplete,
		Opinion,
		Invalid,
		WontFix,
		Expired,
		Confirmed,
		Triaged,
		InProgress,
		FixCommitted,
		FixReleased,
	}

	public enum Importance
	{
		Unknown,
		Undecided,
		Critical,
		High,
		Medium,
		Low,
		Wishlist,
	}

	public class BugTask
	{
		static Dictionary<string, Status> StatusMapping = new Dictionary<string, Status>()
		{
			{ "New", Status.New },
			{ "Incomplete", Status.Incomplete },
			{ "Opinion", Status.Opinion },
			{ "Invalid", Status.Invalid },
			{ "Won't Fix", Status.WontFix },
			{ "Expired", Status.Expired },
			{ "Confirmed", Status.Confirmed },
			{ "Triaged", Status.Triaged },
			{ "In Progress", Status.InProgress },
			{ "Fix Committed", Status.FixCommitted },
			{ "Fix Released", Status.FixReleased },
		};

		static Dictionary<string, Importance> ImportanceMapping = new Dictionary<string, Importance>()
		{
			{ "Unknown", Importance.Unknown },
			{ "Undecided", Importance.Undecided },
			{ "Critical", Importance.Critical },
			{ "High", Importance.High },
			{ "Medium", Importance.Medium },
			{ "Low", Importance.Low },
			{ "Wishlist", Importance.Wishlist },
		};

		public string Name => Json.title;
		public DateTimeOffset Created => Json.date_created;
		public Status Status => StatusMapping[Json.status];
		public Importance Importance => ImportanceMapping[Json.importance];
		public bool HasAssignee => Json.assignee_link != null;
		public bool HasMilestone => Json.milestone_link != null;
		public async Task<Bug> GetBug() => await Cache.GetBug(Json.bug_link);
		public async Task<Milestone> GetMilestone() => HasMilestone ? await Cache.GetMilestone(Json.milestone_link) : null;

		internal readonly Cache Cache;
		internal readonly JsonBugTask Json;

		internal BugTask(Cache cache, JsonBugTask json) => (Cache, Json) = (cache, json);
	}
}
