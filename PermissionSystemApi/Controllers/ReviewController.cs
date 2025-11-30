using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PermissionSystemApi.Data;
using PermissionSystemApi.Dtos;
using PermissionSystemApi.Models;

[Route("api/review")]
[ApiController]
public class ReviewController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReviewController(ApplicationDbContext context)
    {
        _context = context;
    }

    // -------------------------------
    // 1) Start Review Cycle
    // -------------------------------
    [HttpPost("start")]
    public async Task<IActionResult> StartCycle([FromBody] StartCycleDto dto)
    {
        try
        {
            // Check Active Cycle
            var active = await _context.PeriodicReviews
                .Where(r => r.Status == "InProgress")
                .FirstOrDefaultAsync();

            if (active != null)
                return BadRequest("A review cycle is already active.");

            // Create cycle
            var cycle = new PeriodicReview
            {
                ReviewDetails = "Periodic Access Review",
                Status = "InProgress",
                CurrentStage = "InProgress",
                RequestNumber = GenerateRequestNumber("REVIEW"),
                CreatedAt = DateTime.Now
            };

            _context.PeriodicReviews.Add(cycle);
            await _context.SaveChangesAsync();

            return Ok(new { cycleId = cycle.Id });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Invalid request body.");
        }
    }

    // -------------------------------
    // 2) Review Overview
    // -------------------------------
    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var active = await _context.PeriodicReviews
            .Where(r => r.Status == "InProgress")
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        var total = await _context.PermissionRecords.CountAsync();

        int inScope = 0;
        int reviewed = 0;

        if (active != null)
        {
            inScope = await _context.PermissionRecords
                .Where(p => p.CreatedAt <= active.CreatedAt)
                .CountAsync();

            reviewed = await _context.PermissionRecords
                .Where(p => p.UpdatedAt != null &&
                            p.UpdatedAt >= active.CreatedAt)
                .CountAsync();
        }

        var dto = new ReviewOverviewDto
        {
            HasActiveCycle = active != null,
            ActiveCycleId = active?.Id ?? 0,
            ActiveCycleStatus = active?.Status,
            ActiveCycleCreatedAt = active?.CreatedAt,
            TotalPermissions = total,
            InScopeThisCycle = inScope,
            ReviewedCount = reviewed
        };

        return Ok(dto);
    }

    // -------------------------------
    // 3) Review Items In Scope
    // -------------------------------
    [HttpGet("items")]
    public async Task<IActionResult> Items()
    {
        var active = await _context.PeriodicReviews
            .Where(r => r.Status == "InProgress")
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (active == null)
            return Ok(new List<ReviewItemDto>());

        // Pick all permissions created before cycle start
        var items = await _context.PermissionRecords
            .Include(p => p.User)
            .Where(p => p.CreatedAt <= active.CreatedAt)
            .Select(p => new ReviewItemDto
            {
                Id = p.Id,
                UserId = p.UserId,
                EmployeeId = p.User.EmployeeId,
                FullName = p.User.FullName,
                Email = p.User.Email,
                SystemName = "General",
                PermissionDetails = p.PermissionDetails,
                CurrentStatus = p.Status,
                CurrentStage = p.CurrentStage,
                RequestNumber = p.RequestNumber,
                SuggestedAction = p.Status == "Approved" ? "Keep" : "Review"
            })
            .ToListAsync();

        return Ok(items);
    }

    // -------------------------------
    // 4) Save Decisions (Keep/Revoke)
    // -------------------------------
    [HttpPost("save-decisions")]
    public async Task<IActionResult> SaveDecisions([FromBody] List<SaveDecisionDto> decisions)
    {
        foreach (var d in decisions)
        {
            var perm = await _context.PermissionRecords.FindAsync(d.Id);
            if (perm == null) continue;

            // Save review action to PermissionRecord
            perm.Status = d.SuggestedAction == "Revoke" ? "Revoked" : "Approved";
            perm.CurrentStage = "Reviewed";
            perm.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        return Ok(new { saved = decisions.Count });
    }

    // -------------------------------
    // 5) Confirm Review Cycle
    // -------------------------------
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm()
    {
        var active = await _context.PeriodicReviews
            .Where(r => r.Status == "InProgress")
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (active == null)
            return BadRequest("No active cycle.");

        active.Status = "Completed";
        active.CurrentStage = "Completed";
        active.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new { confirmed = true, cycleId = active.Id });
    }

    // -------------------------------
    // 6) Get Pending Approvals for Manager
    // -------------------------------
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(int userId)
    {
        var assignments = await _context.ManagerAssignments
            .Where(m => m.UserId == userId)
            .ToListAsync();

        var result = new List<PendingApprovalVm>();

        foreach (var a in assignments)
        {
            // FIRST APPROVAL - Direct Manager
            if (a.RoleType == "DirectManager")
            {
                if (a.SystemName == "InvoiceQ")
                {
                    result.AddRange(
                        _context.InvoiceQRequests
                            .Where(r => r.Status == "Pending")
                            .Select(r => new PendingApprovalVm
                            {
                                id = r.Id,
                                requestNumber = r.RequestNumber,
                                system = "InvoiceQ",
                                details = r.RequestDetails,
                                status = r.Status,
                                currentStage = r.CurrentStage,
                                createdAt = r.CreatedAt,
                                role = "First Approval"
                            })
                    );
                }

                if (a.SystemName == "Ocity")
                {
                    result.AddRange(
                        _context.OcityRequests
                            .Where(r => r.Status == "Pending")
                            .Select(r => new PendingApprovalVm
                            {
                                id = r.Id,
                                requestNumber = r.RequestNumber,
                                system = "Ocity",
                                details = r.RequestDetails,
                                status = r.Status,
                                currentStage = r.CurrentStage,
                                createdAt = r.CreatedAt,
                                role = "First Approval"
                            })
                    );
                }
            }

            // FINAL APPROVAL - Financial/Project Manager
            if (a.RoleType == "FinancialManager" || a.RoleType == "ProjectManager")
            {
                if (a.SystemName == "InvoiceQ")
                {
                    result.AddRange(
                        _context.InvoiceQRequests
                            .Where(r => r.Status == "FirstApproved")
                            .Select(r => new PendingApprovalVm
                            {
                                id = r.Id,
                                requestNumber = r.RequestNumber,
                                system = "InvoiceQ",
                                details = r.RequestDetails,
                                status = r.Status,
                                currentStage = r.CurrentStage,
                                createdAt = r.CreatedAt,
                                role = "Final Approval"
                            })
                    );
                }

                if (a.SystemName == "Ocity")
                {
                    result.AddRange(
                        _context.OcityRequests
                            .Where(r => r.Status == "FirstApproved")
                            .Select(r => new PendingApprovalVm
                            {
                                id = r.Id,
                                requestNumber = r.RequestNumber,
                                system = "Ocity",
                                details = r.RequestDetails,
                                status = r.Status,
                                currentStage = r.CurrentStage,
                                createdAt = r.CreatedAt,
                                role = "Final Approval"
                            })
                    );
                }
            }
        }

        return Ok(result.OrderByDescending(r => r.createdAt));
    }

    // -------------------------------
    // 7) Get Review History
    // -------------------------------
    [HttpGet("history")]
    public async Task<IActionResult> GetReviewHistory()
    {
        var reviews = await _context.PeriodicReviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.RequestNumber,
                r.Status,
                r.CurrentStage,
                r.CreatedAt,
                r.UpdatedAt,
                r.ReviewDetails
            })
            .ToListAsync();

        return Ok(reviews);
    }

    // -------------------------------
    // HELPER METHOD
    // -------------------------------
    private string GenerateRequestNumber(string prefix)
    {
        return $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
    }
}

// ============================================
// DTOs
// ============================================

public class StartCycleDto
{
    public int ManagerUserId { get; set; }
}




public class SaveDecisionDto
{
    public int Id { get; set; }
    public string SuggestedAction { get; set; } // "Keep" or "Revoke"
}

