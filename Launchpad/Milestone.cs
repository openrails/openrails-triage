using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Open_Rails_Roadmap_bot.Launchpad
{
	class JsonMilestoneCollection
	{
		public JsonMilestone[] entries;
		public string next_collection_link;
		public JsonMilestoneCollection(string url) => next_collection_link = url;
	}

	class JsonMilestone
	{
		public string self_link;
		public string name;
		public string title;
		public string target_link;
	}

	public class Milestone
	{
		public string Id => Json.name;
		public string Name => Json.title;

		public async Task<List<Specification>> GetSpecifications() => (
			await (
				await Cache.GetProject(Json.target_link)
			).GetSpecifications()
		).Where(
			specification => specification.Json.milestone_link == Json.self_link
		).ToList();

		internal readonly Cache Cache;
		internal readonly JsonMilestone Json;

		internal Milestone(Cache cache, JsonMilestone json) => (Cache, Json) = (cache, json);
	}
}
