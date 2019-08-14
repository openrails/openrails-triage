using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Triage.Trello
{
	#pragma warning disable CS0649

	class JsonBoard
	{
		public string id;
		public string name;
		public Uri url;
		public Dictionary<string, string> labelNames;
	}

	#pragma warning restore CS0649


	public class Board
	{
		public string Id => Json.id;
		public string Name => Json.name;
		public Uri Uri => Json.url;
		public Dictionary<string, string> Labels => Json.labelNames;
		public async Task<List<List>> GetLists() => await Cache.GetListCollection(Json.id);

		internal readonly Cache Cache;
		internal readonly JsonBoard Json;

		internal Board(Cache cache, JsonBoard json) => (Cache, Json) = (cache, json);
	}
}
