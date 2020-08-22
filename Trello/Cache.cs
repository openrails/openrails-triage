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
		readonly Dictionary<string, List<Card>> CardCollections = new Dictionary<string, List<Card>>();

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

		public async Task<List<Card>> GetCardCollection(string idList)
		{
			var url = $"https://api.trello.com/1/lists/{idList}/cards?key={Key}&token={Token}";
			if (!CardCollections.ContainsKey(url))
				CardCollections[url] = (await Get<List<JsonCard>>(url))
					.Select(json => new Card(this, json))
					.ToList();
			return CardCollections[url];
		}
	}
}
