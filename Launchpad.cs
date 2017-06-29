using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Open_Rails_Roadmap_bot.Launchpad
{
	class JsonProject
	{
		public string name;
		public string title;
		public string active_milestones_collection_link;
	}

	class JsonMilestoneCollection
	{
		public JsonMilestone[] entries;
	}

	class JsonMilestone
	{
		public string name;
		public string title;
	}

	public class Project
	{
		public static async Task<Project> Get(string url)
		{
			var response = await new HttpClient().GetAsync(url);
			var text = await response.Content.ReadAsStringAsync();
			return new Project(JsonConvert.DeserializeObject<JsonProject>(text));
		}

		public string Id => Json.name;
		public string Name => Json.title;

		public async Task<List<Milestone>> GetActiveMilestones()
		{
			var response = await new HttpClient().GetAsync(Json.active_milestones_collection_link);
			var text = await response.Content.ReadAsStringAsync();
			var json = JsonConvert.DeserializeObject<JsonMilestoneCollection>(text);
			return json.entries.Select(milestone => new Milestone(milestone)).ToList();
		}

		readonly JsonProject Json;

		internal Project(JsonProject json) => Json = json;
	}

	public class Milestone
	{
		public string Id => Json.name;
		public string Name => Json.title;

		readonly JsonMilestone Json;

		internal Milestone(JsonMilestone json) => Json = json;
	}
}
