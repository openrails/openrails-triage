using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Open_Rails_Triage.Trello
{
	public class Cache
	{
		readonly string Key;
		readonly string Token;
		readonly HttpClient Client = new HttpClient();
		readonly Dictionary<string, Board> Boards = new Dictionary<string, Board>();
		readonly Dictionary<string, List<List>> ListCollections = new Dictionary<string, List<List>>();

		public Cache(string key, string token)
		{
			Key = key;
			Token = token;
		}

		internal async Task<T> Get<T>(string url)
		{
			var response = await Client.GetAsync(url);
			var text = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<T>(text);
		}

		public async Task<Board> GetBoard(string idBoard)
		{
			var url = $"https://api.trello.com/1/boards/{idBoard}?key={Key}&token={Token}";
			if (!Boards.ContainsKey(url))
				Boards[url] = new Board(this, await Get<JsonBoard>(url));
			return Boards[url];
		}

		public async Task<List<List>> GetListCollection(string idBoard)
		{
			var url = $"https://api.trello.com/1/boards/{idBoard}/lists?key={Key}&token={Token}";
			if (!ListCollections.ContainsKey(url))
				ListCollections[url] = (await Get<List<JsonList>>(url))
					.Select(json => new List(this, json))
					.ToList();
			return ListCollections[url];
		}
	}
}
