using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Open_Rails_Roadmap_bot.Launchpad
{
	class JsonSpecificationCollection
	{
		public JsonSpecification[] entries;
		public string next_collection_link;
		public JsonSpecificationCollection(string url) => next_collection_link = url;
	}

	class JsonSpecification
	{
		public string self_link;
		public string name;
		public string title;
		public string milestone_link;
	}

	public class Specification
	{
		public string Id => Json.name;
		public string Name => Json.title;

		internal readonly Cache Cache;
		internal readonly JsonSpecification Json;

		internal Specification(Cache cache, JsonSpecification json) => (Cache, Json) = (cache, json);
	}
}
