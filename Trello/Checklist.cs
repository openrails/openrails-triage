using System;
using System.Collections.Generic;
using System.Linq;

namespace Open_Rails_Triage.Trello
{
	#pragma warning disable CS0649

	class JsonChecklist
	{
		public string id;
		public string name;
		public List<JsonChecklistItem> checkItems;
	}

	#pragma warning restore CS0649


	public class Checklist
	{
		public string Id => Json.id;
		public string Name => Json.name;
		public List<ChecklistItem> ChecklistItems => Items;

		internal readonly Cache Cache;
		internal readonly JsonChecklist Json;
		internal readonly List<ChecklistItem> Items;

		internal Checklist(Cache cache, JsonChecklist json) => (Cache, Json, Items) = (cache, json, json.checkItems.Select(checkItem => new ChecklistItem(cache, checkItem)).ToList());
	}
}
