using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Open_Rails_Roadmap_bot.Launchpad
{
	public class Cache
	{
		Dictionary<string, Project> Projects = new Dictionary<string, Project>();
		Dictionary<string, List<Milestone>> MilestoneCollections = new Dictionary<string, List<Milestone>>();
		Dictionary<string, Milestone> Milestones = new Dictionary<string, Milestone>();
		Dictionary<string, List<Specification>> SpecificationCollections = new Dictionary<string, List<Specification>>();
		Dictionary<string, Specification> Specifications = new Dictionary<string, Specification>();

		internal async Task<T> Get<T>(string url)
		{
			System.Console.WriteLine("{0}<{1}>", typeof(T).FullName, url);
			var response = await new HttpClient().GetAsync(url);
			var text = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<T>(text);
		}

		public async Task<Project> GetProject(string url)
		{
			if (!Projects.ContainsKey(url))
				Projects[url] = new Project(this, await Get<JsonProject>(url));
			return Projects[url];
		}

		public async Task<List<Milestone>> GetMilestoneCollection(string url)
		{
			if (!MilestoneCollections.ContainsKey(url))
			{
				var collection = new List<Milestone>();
				var json = new JsonMilestoneCollection(url);
				do
				{
					json = await Get<JsonMilestoneCollection>(json.next_collection_link);
					collection.AddRange(json.entries.Select(milestone => FromJson(milestone)));
				} while (json.next_collection_link != null);
				MilestoneCollections[url] = collection;
			}
			return MilestoneCollections[url];
		}

		public async Task<Milestone> GetMilestone(string url)
		{
			if (!Milestones.ContainsKey(url))
				Milestones[url] = new Milestone(this, await Get<JsonMilestone>(url));
			return Milestones[url];
		}

		internal Milestone FromJson(JsonMilestone json)
		{
			return Milestones[json.self_link] = new Milestone(this, json);
		}

		public async Task<List<Specification>> GetSpecificationCollection(string url)
		{
			if (!SpecificationCollections.ContainsKey(url))
			{
				var collection = new List<Specification>();
				var json = new JsonSpecificationCollection(url);
				do
				{
					json = await Get<JsonSpecificationCollection>(json.next_collection_link);
					collection.AddRange(json.entries.Select(Specification => FromJson(Specification)));
				} while (json.next_collection_link != null);
				SpecificationCollections[url] = collection;
			}
			return SpecificationCollections[url];
		}

		public async Task<Specification> GetSpecification(string url)
		{
			if (!Specifications.ContainsKey(url))
				Specifications[url] = new Specification(this, await Get<JsonSpecification>(url));
			return Specifications[url];
		}

		internal Specification FromJson(JsonSpecification json)
		{
			return Specifications[json.self_link] = new Specification(this, json);
		}
	}
}
