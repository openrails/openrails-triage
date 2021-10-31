using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Triage.Trello
{
	#pragma warning disable CS0649

	class JsonList
	{
		public string id;
		public string name;
		public bool closed;
		public float pos;
	}

	#pragma warning restore CS0649


	public class List
	{
		public string Id => Json.id;
		public string Name => Json.name;
		public bool Closed => Json.closed;
		public bool Open => !Json.closed;
		public float Position => Json.pos;
		public async Task<List<Card>> GetCards() => await Cache.GetCardCollection(Json.id);

		internal readonly Cache Cache;
		internal readonly JsonList Json;

		internal List(Cache cache, JsonList json) => (Cache, Json) = (cache, json);
	}
}
