using Newtonsoft.Json;

namespace Open_Rails_Triage.GitHub
{
	public class GraphQuery
	{
		[JsonProperty("query")]
		public string Query;
	}
}
