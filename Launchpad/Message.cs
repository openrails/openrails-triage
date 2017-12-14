using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Triage.Launchpad
{
	#pragma warning disable CS0649

	class JsonMessageCollection
	{
		public JsonMessage[] entries;
		public string next_collection_link;
		public JsonMessageCollection(string url) => next_collection_link = url;
	}

	class JsonMessage
	{
		public string self_link;
		public string subject;
		public string content;
		public DateTimeOffset date_created;
		public string owner_link;
		public string web_link;
	}

	#pragma warning restore CS0649

	public class Message
	{
		public string Name => Json.subject;
		public string Description => Json.content;

		internal readonly Cache Cache;
		internal readonly JsonMessage Json;

		internal Message(Cache cache, JsonMessage json) => (Cache, Json) = (cache, json);
	}
}
