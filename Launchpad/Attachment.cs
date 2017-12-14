using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Triage.Launchpad
{
	#pragma warning disable CS0649

	class JsonAttachmentCollection
	{
		public JsonAttachment[] entries;
		public string next_collection_link;
		public JsonAttachmentCollection(string url) => next_collection_link = url;
	}

	class JsonAttachment
	{
		public string self_link;
		public string title;
		public string type;
		public string data_link;
		public string web_link;
	}

	#pragma warning restore CS0649

	public enum Type
	{
		Unspecified,
		Patch,
	}

	public class Attachment
	{
		static Dictionary<string, Type> TypeMapping = new Dictionary<string, Type>()
		{
			{ "Unspecified", Type.Unspecified },
			{ "Patch", Type.Patch },
		};

		public string Name => Json.title;
		public Type Type => TypeMapping[Json.type];
		public async Task<string> GetData() => await Cache.GetAttachmentData(Json.data_link);

		internal readonly Cache Cache;
		internal readonly JsonAttachment Json;

		internal Attachment(Cache cache, JsonAttachment json) => (Cache, Json) = (cache, json);
	}
}
