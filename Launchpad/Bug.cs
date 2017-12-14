using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Triage.Launchpad
{
	#pragma warning disable CS0649

	class JsonBugCollection
	{
		public JsonBug[] entries;
		public string next_collection_link;
		public JsonBugCollection(string url) => next_collection_link = url;
	}

	class JsonBug
	{
		public string self_link;
		public string title;
		public string description;
		public DateTimeOffset date_created;
		public string[] tags;
		public string messages_collection_link;
		public string attachments_collection_link;
		public string web_link;
	}

	#pragma warning restore CS0649

	public class Bug
	{
		public string Name => Json.title;
		public string Description => Json.description;
		public DateTimeOffset Created => Json.date_created;
		public IReadOnlyList<string> Tags => Json.tags;
		public async Task<List<Message>> GetMessages() => await Cache.GetMessageCollection(Json.messages_collection_link);
		public async Task<List<Attachment>> GetAttachments() => await Cache.GetAttachmentCollection(Json.attachments_collection_link);

		internal readonly Cache Cache;
		internal readonly JsonBug Json;

		internal Bug(Cache cache, JsonBug json) => (Cache, Json) = (cache, json);
	}
}
