using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PermissionSystemApi.Dtos;
using PermissionSystemApi.Services;

namespace PermissionSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalController : ControllerBase
    {
        private readonly ApprovalService _approvalService;

        public ApprovalController(ApprovalService approvalService)
        {
            _approvalService = approvalService;
        }

        // ============================================
        // First Level Approval (Direct Manager)
        // ============================================

        [HttpPut("approve-first")]
        public async Task<IActionResult> ApproveFirstLevel([FromBody] ApproveRequestDto dto)
        {
            var result = await _approvalService.ApproveFirstLevel(dto);

            if (!result)
                return BadRequest("Invalid request or wrong request status");

            return Ok(new
            {
                message = "Request approved by Direct Manager",
            });
        }

        // ============================================
        // Second Level Approval (Financial/Project Manager)
        // ============================================

        [HttpPut("approve-second")]
        public async Task<IActionResult> ApproveSecondLevel([FromBody] ApproveRequestDto dto)
        {
            var result = await _approvalService.ApproveSecondLevel(dto);

            if (!result)
                return BadRequest("Invalid request or wrong request status");

            return Ok(new
            {
                message = "Request approved by Final Approver",
            });
        }

        // ============================================
        // Implementor
        // ============================================

        [HttpPut("Implementor")]
        public async Task<IActionResult> Implementor([FromBody] ApproveRequestDto dto)
        {
            var result = await _approvalService.Implementor(dto);

            if (!result)
                return BadRequest("Invalid request or wrong request status");

            return Ok(new
            {
                message = "Request approved by Final Approver",
            });
        }

        // ============================================
        // Reject Request
        // ============================================

        [HttpPut("reject")]
        public async Task<IActionResult> Reject([FromBody] RejectRequestDto dto)
        {
            var result = await _approvalService.Reject(dto);

            if (!result)
                return BadRequest("Invalid request id");

            return Ok(new
            {
                message = "Request rejected successfully",
            });
        }


        [HttpGet("get-role")]
        public async Task<IActionResult> GetRole(int userId)
        {
            var role = await _approvalService.GetRole(userId);
            return Ok(role ?? "");
        }


        // ===== NEW ENDPOINT: history for manager =====
        [HttpGet("manager-history")]
        public async Task<IActionResult> GetManagerHistory(string username)
        {
            var result = await _approvalService.GetManagerHistory(int.Parse(username));

            return Ok(result);
        }
    }


}