using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NotificationService.DTOs;
using NotificationService.Services;
using NotificationService.ErrorHandling;
using System;

namespace NotificationService.Controllers
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class EmailNotificationController : ControllerBase
    {
        private readonly IEmailSenderService _emailSenderService;
        private readonly JwtTokenService _jwtTokenService;



        public EmailNotificationController(IEmailSenderService emailSenderService, JwtTokenService jwtTokenService)
        {
            _emailSenderService = emailSenderService ?? throw new ArgumentNullException(nameof(emailSenderService));
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailNotificationRequest request)
        {
            // Check if request is null
            if (request == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = ErrorCodes.ValidationError,
                    Message = ErrorMessages.ValidationErrorMessage,
                    Details = "The request body is null.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Check if Recipient is null or empty
            if (string.IsNullOrEmpty(request.Recipient))
            {
                return BadRequest(new ErrorResponse
                {
                    Code = ErrorCodes.ValidationError,
                    Message = "Recipient email cannot be null or empty.",
                    Details = "Please provide a valid recipient email address.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Check if recipient email format is valid
            if (!IsValidEmail(request.Recipient))
            {
                return BadRequest(new ErrorResponse
                {
                    Code = ErrorCodes.ValidationError,
                    Message = "Invalid recipient email format.",
                    Details = $"Recipient email: {request.Recipient}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Check if CC email is not empty and format is valid
            if (!string.IsNullOrEmpty(request.CC) && !IsValidEmail(request.CC))
            {
                return BadRequest(new ErrorResponse
                {
                    Code = ErrorCodes.ValidationError,
                    Message = "Invalid CC email format.",
                    Details = $"CC email: {request.CC}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Check if scheduled time is provided and is in the past
            if (request.ScheduledTime.HasValue && request.ScheduledTime.Value < DateTime.UtcNow)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = ErrorCodes.ValidationError,
                    Message = "Invalid ScheduledTime.",
                    Details = "The ScheduledTime cannot be in the past.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            try
            {
                var response = await _emailSenderService.ScheduleEmail(request);

                return Ok(new EmailNotificationResponse(
                    success: response.Success,
                    message: response.Message,
                    status: response.Status
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Code = ErrorCodes.InternalServerError,
                    Message = "An error occurred while processing your request.",
                    Details = ex.Message,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        [HttpGet("status/{id}")]
        public async Task<IActionResult> GetStatus(int id)
        {
            var status = await _emailSenderService.GetEmailStatus(id);
            if (status == null)
            {
                return NotFound(new ErrorResponse
                {
                    Code = ErrorCodes.NotFoundError,
                    Message = ErrorMessages.NotFoundErrorMessage,
                    Details = $"No email notification found with ID: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
            return Ok(status);
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotificationsByStatus([FromQuery] string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return BadRequest(new ErrorResponse
                {
                    Code = ErrorCodes.ValidationError,
                    Message = "Status query parameter is required.",
                    Details = "Please provide a valid status.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var notifications = await _emailSenderService.GetNotificationsByStatusAsync(status);
            return Ok(notifications);
        }

        [AllowAnonymous]
        [HttpPost("auth/token")]
        public IActionResult GenerateToken([FromBody] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new ErrorResponse
                {
                    Code = ErrorCodes.ValidationError,
                    Message = "Username is required to generate a token.",
                    Details = "The username cannot be null or empty.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            try
            {
                if (username != "testuser")
                {
                    return Unauthorized(new ErrorResponse
                    {
                        Code = ErrorCodes.ValidationError,
                        Message = "Invalid username.",
                        Details = "Only valid user is allowed to generate a token.",
                        TraceId = HttpContext.TraceIdentifier
                    });
                }

                var token = _jwtTokenService.GenerateJwtToken(username);
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Code = ErrorCodes.InternalServerError,
                    Message = "An error occurred while generating the token.",
                    Details = ex.Message,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var mailAddress = new System.Net.Mail.MailAddress(email);
                return mailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
