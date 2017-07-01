using System.Collections.Generic;

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

	public enum LifecycleStatus
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

	public enum DirectionApproved
	{
		Approved,
		NeedsApproval,
	}

	public enum DefinitionStatus
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

	public enum ImplementationStatus
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
		static Dictionary<string, LifecycleStatus> LifecycleStatusMapping = new Dictionary<string, LifecycleStatus>()
		{
			{ "Not started", LifecycleStatus.NotStarted },
			{ "Started", LifecycleStatus.Started },
			{ "Complete", LifecycleStatus.Complete },
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

		static Dictionary<string, DefinitionStatus> DefinitionStatusMapping = new Dictionary<string, DefinitionStatus>()
		{
			{ "Approved", DefinitionStatus.Approved },
			{ "Pending Approval", DefinitionStatus.PendingApproval },
			{ "Review", DefinitionStatus.Review },
			{ "Drafting", DefinitionStatus.Drafting },
			{ "Discussion", DefinitionStatus.Discussion },
			{ "New", DefinitionStatus.New },
			{ "Superseded", DefinitionStatus.Superseded },
			{ "Obsolete", DefinitionStatus.Obsolete },
		};

		static Dictionary<string, ImplementationStatus> ImplementationStatusMapping = new Dictionary<string, ImplementationStatus>()
		{
			{ "Unknown", ImplementationStatus.Unknown },
			{ "Not started", ImplementationStatus.NotStarted },
			{ "Deferred", ImplementationStatus.Deferred },
			{ "Needs Infrastructure", ImplementationStatus.NeedsInfrastructure },
			{ "Blocked", ImplementationStatus.Blocked },
			{ "Started", ImplementationStatus.Started },
			{ "Slow progress", ImplementationStatus.SlowProgress },
			{ "Good progress", ImplementationStatus.GoodProgress },
			{ "Beta Available", ImplementationStatus.BetaAvailable },
			{ "Needs Code Review", ImplementationStatus.NeedsCodeReview },
			{ "Deployment", ImplementationStatus.Deployment },
			{ "Implemented", ImplementationStatus.Implemented },
			{ "Informational", ImplementationStatus.Informational },
		};

		public string Id => Json.name;
		public string Name => Json.title;
		public string Summary => Json.summary;
		public LifecycleStatus LifecycleStatus => LifecycleStatusMapping[Json.lifecycle_status];
		public Priority Priority => PriorityMapping[Json.priority];
		public DirectionApproved DirectionApproved => Json.direction_approved ? DirectionApproved.Approved : DirectionApproved.NeedsApproval;
		public DefinitionStatus DefinitionStatus => DefinitionStatusMapping[Json.definition_status];
		public ImplementationStatus ImplementationStatus => ImplementationStatusMapping[Json.implementation_status];
		public bool HasApprover => Json.approver_link != null;
		public bool HasDrafter => Json.drafter_link != null;
		public bool HasAssignee => Json.assignee_link != null;

		internal readonly Cache Cache;
		internal readonly JsonSpecification Json;

		internal Specification(Cache cache, JsonSpecification json) => (Cache, Json) = (cache, json);
	}
}
