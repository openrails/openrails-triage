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
		public string valid_specifications_collection_link;
	}

	class JsonMilestoneCollection
	{
		public JsonMilestone[] entries;
		public string next_collection_link;
	}

	class JsonMilestone
	{
		public string name;
		public string title;
	}

	class JsonSpecificationCollection
	{
		public JsonSpecification[] entries;
		public string next_collection_link;
	}

	class JsonSpecification
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
			var milestones = new List<Milestone>();
			var json = new JsonMilestoneCollection()
			{
				next_collection_link = Json.active_milestones_collection_link
			};
			do
			{
				var response = await new HttpClient().GetAsync(json.next_collection_link);
				var text = await response.Content.ReadAsStringAsync();
				json = JsonConvert.DeserializeObject<JsonMilestoneCollection>(text);
				milestones.AddRange(json.entries.Select(milestone => new Milestone(milestone)));
			} while (json.next_collection_link != null);
			return milestones;
		}

		public async Task<List<Specification>> GetValidSpecifications()
		{
			var specifications = new List<Specification>();
			var json = new JsonSpecificationCollection()
			{
				next_collection_link = Json.valid_specifications_collection_link
			};
			do
			{
				var response = await new HttpClient().GetAsync(json.next_collection_link);
				var text = await response.Content.ReadAsStringAsync();
				json = JsonConvert.DeserializeObject<JsonSpecificationCollection>(text);
				specifications.AddRange(json.entries.Select(specification => new Specification(specification)));
			} while (json.next_collection_link != null);
			return specifications;
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

	public class Specification
	{
		public string Id => Json.name;
		public string Name => Json.title;

		readonly JsonSpecification Json;

		internal Specification(JsonSpecification json) => Json = json;
	}
}
