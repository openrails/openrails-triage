using System;
using System.Collections.Generic;

namespace Open_Rails_Triage.Trello
{
	#pragma warning disable CS0649

	class JsonCard
	{
		public string id;
		public string name;
		public string desc;
		public List<string> idMembersVoted;
		public Uri url;
		public float pos;
	}

	#pragma warning restore CS0649


	public class Card
	{
		public string Id => Json.id;
		public string Name => Json.name;
		public string Description => Json.desc;
		public int Votes => Json.idMembersVoted.Count;
		public Uri Uri => Json.url;
		public float Position => Json.pos;

		internal readonly Cache Cache;
		internal readonly JsonCard Json;

		internal Card(Cache cache, JsonCard json) => (Cache, Json) = (cache, json);
	}
}
