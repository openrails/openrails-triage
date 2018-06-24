using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Triage.Launchpad
{
	#pragma warning disable CS0649

	class JsonProject
	{
		public string self_link;
		public string name;
		public string title;
		public string all_milestones_collection_link;
		public string all_specifications_collection_link;
		public string active_milestones_collection_link;
		public string valid_specifications_collection_link;
	}

	#pragma warning restore CS0649

	public class Project
	{
		public string Id => Json.name;
		public string Name => Json.title;

		public Task<List<Milestone>> GetMilestones() => Cache.GetMilestoneCollection(Json.all_milestones_collection_link);
		public Task<List<Milestone>> GetActiveMilestones() => Cache.GetMilestoneCollection(Json.active_milestones_collection_link);
		public Task<List<Specification>> GetSpecifications() => Cache.GetSpecificationCollection(Json.all_specifications_collection_link);
		public Task<List<Specification>> GetValidSpecifications() => Cache.GetSpecificationCollection(Json.valid_specifications_collection_link);
		public Task<List<BugTask>> GetRecentBugTasks() => Cache.GetBugTaskCollection(Json.self_link + "?ws.op=searchTasks&status=New&status=Incomplete&status=Opinion&status=Invalid&status=Won't+Fix&status=Expired&status=Confirmed&status=Triaged&status=In+Progress&status=Fix+Committed&status=Fix+Released&modified_since=" + DateTime.UtcNow.AddDays(-7).ToString("s"));
		public Task<List<BugTask>> GetUnreleasedBugTasks() => Cache.GetBugTaskCollection(Json.self_link + "?ws.op=searchTasks&status=New&status=Incomplete&status=Opinion&status=Invalid&status=Won't+Fix&status=Expired&status=Confirmed&status=Triaged&status=In+Progress&status=Fix+Committed");

		internal readonly Cache Cache;
		internal readonly JsonProject Json;

		internal Project(Cache cache, JsonProject json) => (Cache, Json) = (cache, json);
	}
}
