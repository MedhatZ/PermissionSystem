namespace PermissionSystemApi.Dtos
{
    public class ReviewOverviewDto
    {
        public bool HasActiveCycle { get; set; }
        public int ActiveCycleId { get; set; }
        public string ActiveCycleStatus { get; set; }
        public DateTime? ActiveCycleCreatedAt { get; set; }
        public int TotalPermissions { get; set; }
        public int InScopeThisCycle { get; set; }
        public int ReviewedCount { get; set; }
    }
}
