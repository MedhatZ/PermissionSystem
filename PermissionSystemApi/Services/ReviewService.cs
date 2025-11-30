using PermissionSystemApi.Data;
using PermissionSystemApi.Dtos;
using PermissionSystemApi.Models;
using Microsoft.EntityFrameworkCore;

namespace PermissionSystemApi.Services
{
    public class ReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PermissionRecord>> GetPermissions()
        {
            return await _context.PermissionRecords.ToListAsync();
        }

        //public async Task<bool> ReviewPermission(ReviewItemVm dto)
        //{
        //    var record = await _context.PermissionRecords.FindAsync(dto.PermissionRecordId);

        //    if (record == null)
        //        return false;

        //    record.Status = dto.Status;
        //    record.CurrentStage = "Reviewed";
        //    record.UpdatedAt = DateTime.Now;

        //    var review = new PeriodicReview
        //    {
        //        ReviewDetails = dto.Comment,
        //        Status = dto.Status,
        //        CurrentStage = "Completed",
        //        RequestNumber = GenerateRequestNumber("REVIEW"),
        //        CreatedAt = DateTime.Now
        //    };

        //    _context.PeriodicReviews.Add(review);
        //    await _context.SaveChangesAsync();
        //    return true;
        //}

        private string GenerateRequestNumber(string prefix)
        {
            return $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }
}