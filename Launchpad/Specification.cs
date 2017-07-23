using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Roadmap_bot.Launchpad
{
	class JsonSpecificationCollection
	{
		public JsonSpecification[] entries;
		public string next_collection_link;
		public JsonSpecificationCollection(string url) => next_collection_link = url;
	}

	class JsonSpecification
	{
		public string self_link;
		public string name;
		public string title;
		public string summary;
		public DateTimeOffset date_created;
		public string lifecycle_status;
		public string priority;
		public bool direction_approved;
		public string definition_status;
		public string implementation_status;
		public string approver_link;
		public string drafter_link;
		public string assignee_link;
		public string milestone_link;
	}

	public enum Lifecycle
	{
		NotStarted,
		Started,
		Complete,
	}

	public enum Priority
	{
		Not,
		Undefined,
		Low,
		Medium,
		High,
		Essential,
	}

	public enum Direction
	{
		Approved,
		NeedsApproval,
	}

	public enum Definition
	{
		Approved,
		PendingApproval,
		Review,
		Drafting,
		Discussion,
		New, 
		Superseded,
		Obsolete,
	}

	public enum Implementation
	{
		Unknown,
		NotStarted,
		Deferred,
		NeedsInfrastructure,
		Blocked,
		Started,
		SlowProgress,
		GoodProgress,
		BetaAvailable,
		NeedsCodeReview,
		Deployment,
		Implemented,
		Informational,
	}

	public class Specification
	{
		static Dictionary<string, Lifecycle> LifecycleMapping = new Dictionary<string, Lifecycle>()
		{
			{ "Not started", Lifecycle.NotStarted },
			{ "Started", Lifecycle.Started },
			{ "Complete", Lifecycle.Complete },
		};

		static Dictionary<string, Priority> PriorityMapping = new Dictionary<string, Priority>()
		{
			{ "Not", Priority.Not },
			{ "Undefined", Priority.Undefined },
			{ "Low", Priority.Low },
			{ "Medium", Priority.Medium },
			{ "High", Priority.High },
			{ "Essential", Priority.Essential },
		};

		static Dictionary<string, Definition> DefinitionMapping = new Dictionary<string, Definition>()
		{
			{ "Approved", Definition.Approved },
			{ "Pending Approval", Definition.PendingApproval },
			{ "Review", Definition.Review },
			{ "Drafting", Definition.Drafting },
			{ "Discussion", Definition.Discussion },
			{ "New", Definition.New },
			{ "Superseded", Definition.Superseded },
			{ "Obsolete", Definition.Obsolete },
		};

		static Dictionary<string, Implementation> ImplementationMapping = new Dictionary<string, Implementation>()
		{
			{ "Unknown", Implementation.Unknown },
			{ "Not started", Implementation.NotStarted },
			{ "Deferred", Implementation.Deferred },
			{ "Needs Infrastructure", Implementation.NeedsInfrastructure },
			{ "Blocked", Implementation.Blocked },
			{ "Started", Implementation.Started },
			{ "Slow progress", Implementation.SlowProgress },
			{ "Good progress", Implementation.GoodProgress },
			{ "Beta Available", Implementation.BetaAvailable },
			{ "Needs Code Review", Implementation.NeedsCodeReview },
			{ "Deployment", Implementation.Deployment },
			{ "Implemented", Implementation.Implemented },
			{ "Informational", Implementation.Informational },
		};

		public string Id => Json.name;
		public string Name => Json.title;
		public string Summary => Json.summary;
		public DateTimeOffset Created => Json.date_created;
		public Lifecycle Lifecycle => LifecycleMapping[Json.lifecycle_status];
		public Priority Priority => PriorityMapping[Json.priority];
		public Direction Direction => Json.direction_approved ? Direction.Approved : Direction.NeedsApproval;
		public Definition Definition => DefinitionMapping[Json.definition_status];
		public Implementation Implementation => ImplementationMapping[Json.implementation_status];
		public bool HasApprover => Json.approver_link != null;
		public bool HasDrafter => Json.drafter_link != null;
		public bool HasAssignee => Json.assignee_link != null;
		public bool HasMilestone => Json.milestone_link != null;
		public async Task<Milestone> GetMilestone() => HasMilestone ? await Cache.GetMilestone(Json.milestone_link) : null;

		internal readonly Cache Cache;
		internal readonly JsonSpecification Json;

		internal Specification(Cache cache, JsonSpecification json) => (Cache, Json) = (cache, json);
	}
}
