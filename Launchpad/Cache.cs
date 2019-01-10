using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Open_Rails_Triage.Launchpad
{
	public class Cache
	{
		HttpClient Client = new HttpClient();
		Dictionary<string, Project> Projects = new Dictionary<string, Project>();
		Dictionary<string, List<Milestone>> MilestoneCollections = new Dictionary<string, List<Milestone>>();
		Dictionary<string, Milestone> Milestones = new Dictionary<string, Milestone>();
		Dictionary<string, List<Specification>> SpecificationCollections = new Dictionary<string, List<Specification>>();
		Dictionary<string, Specification> Specifications = new Dictionary<string, Specification>();
		Dictionary<string, List<BugTask>> BugTaskCollections = new Dictionary<string, List<BugTask>>();
		Dictionary<string, BugTask> BugTasks = new Dictionary<string, BugTask>();
		Dictionary<string, Bug> Bugs = new Dictionary<string, Bug>();
		Dictionary<string, List<Message>> MessageCollections = new Dictionary<string, List<Message>>();
		Dictionary<string, Message> Messages = new Dictionary<string, Message>();
		Dictionary<string, List<Attachment>> AttachmentCollections = new Dictionary<string, List<Attachment>>();
		Dictionary<string, Attachment> Attachments = new Dictionary<string, Attachment>();
		Dictionary<string, Person> Persons = new Dictionary<string, Person>();

		internal async Task<T> Get<T>(string url)
		{
			var response = await Client.GetAsync(url);
			var text = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<T>(text);
		}

		public async Task<Project> GetProject(string url)
		{
			if (!Projects.ContainsKey(url))
				Projects[url] = new Project(this, await Get<JsonProject>(url));
			return Projects[url];
		}

		public async Task<List<Milestone>> GetMilestoneCollection(string url)
		{
			if (!MilestoneCollections.ContainsKey(url))
			{
				var collection = new List<Milestone>();
				var json = new JsonMilestoneCollection(url);
				do
				{
					json = await Get<JsonMilestoneCollection>(json.next_collection_link);
					collection.AddRange(json.entries.Select(milestone => FromJson(milestone)));
				} while (json.next_collection_link != null);
				MilestoneCollections[url] = collection;
			}
			return MilestoneCollections[url];
		}

		public async Task<Milestone> GetMilestone(string url)
		{
			if (!Milestones.ContainsKey(url))
				Milestones[url] = new Milestone(this, await Get<JsonMilestone>(url));
			return Milestones[url];
		}

		internal Milestone FromJson(JsonMilestone json)
		{
			return Milestones[json.self_link] = new Milestone(this, json);
		}

		public async Task<List<Specification>> GetSpecificationCollection(string url)
		{
			if (!SpecificationCollections.ContainsKey(url))
			{
				var collection = new List<Specification>();
				var json = new JsonSpecificationCollection(url);
				do
				{
					json = await Get<JsonSpecificationCollection>(json.next_collection_link);
					collection.AddRange(json.entries.Select(Specification => FromJson(Specification)));
				} while (json.next_collection_link != null);
				SpecificationCollections[url] = collection;
			}
			return SpecificationCollections[url];
		}

		public async Task<Specification> GetSpecification(string url)
		{
			if (!Specifications.ContainsKey(url))
				Specifications[url] = new Specification(this, await Get<JsonSpecification>(url));
			return Specifications[url];
		}

		internal Specification FromJson(JsonSpecification json)
		{
			return Specifications[json.self_link] = new Specification(this, json);
		}

		public async Task<List<BugTask>> GetBugTaskCollection(string url)
		{
			if (!BugTaskCollections.ContainsKey(url))
			{
				var collection = new List<BugTask>();
				var json = new JsonBugTaskCollection(url);
				do
				{
					json = await Get<JsonBugTaskCollection>(json.next_collection_link);
					collection.AddRange(json.entries.Select(BugTask => FromJson(BugTask)));
				} while (json.next_collection_link != null);
				BugTaskCollections[url] = collection;
			}
			return BugTaskCollections[url];
		}

		public async Task<BugTask> GetBugTask(string url)
		{
			if (!BugTasks.ContainsKey(url))
				BugTasks[url] = new BugTask(this, await Get<JsonBugTask>(url));
			return BugTasks[url];
		}

		internal BugTask FromJson(JsonBugTask json)
		{
			return BugTasks[json.self_link] = new BugTask(this, json);
		}

		public async Task<Bug> GetBug(string url)
		{
			if (!Bugs.ContainsKey(url))
				Bugs[url] = new Bug(this, await Get<JsonBug>(url));
			return Bugs[url];
		}

		public async Task<List<Message>> GetMessageCollection(string url)
		{
			if (!MessageCollections.ContainsKey(url))
			{
				var collection = new List<Message>();
				var json = new JsonMessageCollection(url);
				do
				{
					json = await Get<JsonMessageCollection>(json.next_collection_link);
					collection.AddRange(json.entries.Select(Message => FromJson(Message)));
				} while (json.next_collection_link != null);
				MessageCollections[url] = collection;
			}
			return MessageCollections[url];
		}

		public async Task<Message> GetMessage(string url)
		{
			if (!Messages.ContainsKey(url))
				Messages[url] = new Message(this, await Get<JsonMessage>(url));
			return Messages[url];
		}

		internal Message FromJson(JsonMessage json)
		{
			return Messages[json.self_link] = new Message(this, json);
		}

		public async Task<List<Attachment>> GetAttachmentCollection(string url)
		{
			if (!AttachmentCollections.ContainsKey(url))
			{
				var collection = new List<Attachment>();
				var json = new JsonAttachmentCollection(url);
				do
				{
					json = await Get<JsonAttachmentCollection>(json.next_collection_link);
					collection.AddRange(json.entries.Select(Attachment => FromJson(Attachment)));
				} while (json.next_collection_link != null);
				AttachmentCollections[url] = collection;
			}
			return AttachmentCollections[url];
		}

		public async Task<Attachment> GetAttachment(string url)
		{
			if (!Attachments.ContainsKey(url))
				Attachments[url] = new Attachment(this, await Get<JsonAttachment>(url));
			return Attachments[url];
		}

		internal Attachment FromJson(JsonAttachment json)
		{
			return Attachments[json.self_link] = new Attachment(this, json);
		}

		public async Task<string> GetAttachmentData(string url)
		{
			var response = await Client.GetAsync(url);
			return await response.Content.ReadAsStringAsync();
		}

		public async Task<Person> GetPerson(string url)
		{
			if (!Persons.ContainsKey(url))
				Persons[url] = new Person(this, await Get<JsonPerson>(url));
			return Persons[url];
		}
	}
}
