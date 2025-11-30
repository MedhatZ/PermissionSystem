namespace PermissionSystemWeb.Models.ViewModels
{
    public class PeriodicReviewPageVm
    {
        public ReviewOverviewVm Overview { get; set; } = new ReviewOverviewVm();
        public List<ReviewItemVm> Items { get; set; } = new();
    }
}
