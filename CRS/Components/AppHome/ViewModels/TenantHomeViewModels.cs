namespace Horizon.Components.AppHome.ViewModels {
    public sealed class TenantKpisVm {
        public int Properties { get; set; }
        public int StudiesActive { get; set; }
        public int ReportsRecent { get; set; }
        public int CustomerAccounts { get; set; }
    }

    public sealed class PipelineStageVm {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class WorkItemVm {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Property { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }
}