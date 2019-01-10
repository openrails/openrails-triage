using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Triage.Launchpad
{
	#pragma warning disable CS0649

	class JsonPerson
	{
		public string self_link;
		public string name;
		public string display_name;
		public DateTimeOffset date_created;
		public string web_link;
	}

	#pragma warning restore CS0649

	public class Person
	{
		public string Username => Json.name;
		public string Name => Json.display_name;
		public DateTimeOffset Created => Json.date_created;

		internal readonly Cache Cache;
		internal readonly JsonPerson Json;

		internal Person(Cache cache, JsonPerson json) => (Cache, Json) = (cache, json);
	}
}
