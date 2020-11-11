using System;
using System.Collections.Generic;

namespace Open_Rails_Triage.Trello
{
	#pragma warning disable CS0649

	class JsonChecklistItem
	{
		public string id;
		public string name;
		public string state;
		public float pos;
	}

	#pragma warning restore CS0649


	public class ChecklistItem
	{
		public string Id => Json.id;
		public string Name => Json.name;
		public bool Complete => Json.state == "complete";
		public float Position => Json.pos;

		internal readonly Cache Cache;
		internal readonly JsonChecklistItem Json;

		internal ChecklistItem(Cache cache, JsonChecklistItem json) => (Cache, Json) = (cache, json);
	}
}
