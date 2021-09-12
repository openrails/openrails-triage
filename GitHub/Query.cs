using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Open_Rails_Triage.GitHub
{
	public class Query
	{
		const string Endpoint = "https://api.github.com/graphql";

		readonly string Token;

		HttpClient Client = new HttpClient();

		public Query(string token)
		{
			Token = token;
		}

		internal async Task<JObject> Get(string query)
		{
			var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
			request.Headers.UserAgent.Clear();
			request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Open-Rails-Code-Bot", "1.0"));
			request.Headers.Authorization = new AuthenticationHeaderValue("bearer", Token);
			var graphQuery = new GraphQuery { Query = $"query {{ {query} }}" };
			var graphQueryJson = JsonConvert.SerializeObject(graphQuery);
			request.Content = new StringContent(graphQueryJson, Encoding.UTF8, "application/json");
			var response = await Client.SendAsync(request);
			var text = await response.Content.ReadAsStringAsync();
			return JObject.Parse(text);
		}

		public async Task<GraphPullRequest> GetPullRequest(string organization, string repository, int number)
		{
			var query = @"
				organization(login: """ + organization + @""") {
					repository(name: """ + repository + @""") {
						pullRequest(number: " + number + @") {
							url
							number
							title
							body
							labels(first: 100) {
								nodes {
									name
								}
							}
							additions
							deletions
						}
					}
				}
			";
			var response = await Get(query);
			return response["data"]["organization"]["repository"]["pullRequest"].ToObject<GraphPullRequest>();
		}
	}
}
