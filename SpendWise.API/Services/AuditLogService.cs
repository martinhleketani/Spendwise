using SpendWise.API.Data;
using SpendWise.API.Models;
using System.Security.Claims;

namespace SpendWise.API.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string action,
            string module,
            string description,
            string? entityType = null,
            string? entityId = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                throw new InvalidOperationException(
                    "HTTP context is unavailable.");
            }

            var user = httpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                throw new InvalidOperationException(
                    "An authenticated user is required.");
            }

            string? actorIdValue =
                user.FindFirstValue(ClaimTypes.NameIdentifier);

            string actorName =
                user.FindFirstValue(ClaimTypes.Name) ?? "";

            string actorEmail =
                user.FindFirstValue(ClaimTypes.Email) ?? "";

            if (!int.TryParse(actorIdValue, out int actorId) ||
                string.IsNullOrWhiteSpace(actorName) ||
                string.IsNullOrWhiteSpace(actorEmail))
            {
                throw new InvalidOperationException(
                    "Valid user identity claims are required.");
            }

            string actorType =
                user.IsInRole("Administrator")
                    ? "Administrator"
                    : "Customer";

            await LogAsyncForActorAsync(
                actorType,
                actorId,
                actorName,
                actorEmail,
                action,
                module,
                description,
                entityType,
                entityId);
        }

        public async Task LogAsyncForActorAsync(
            string actorType,
            int? actorId,
            string actorName,
            string actorEmail,
            string action,
            string module,
            string description,
            string? entityType = null,
            string? entityId = null)
        {
            ValidateFields(
                actorType,
                actorName,
                actorEmail,
                action,
                module,
                description,
                entityType,
                entityId);

            if (actorType == "Customer" &&
                (!actorId.HasValue ||
                 actorId.Value <= 0 ||
                 string.IsNullOrWhiteSpace(actorName) ||
                 string.IsNullOrWhiteSpace(actorEmail)))
            {
                throw new ArgumentException(
                    "Valid customer information is required.");
            }

            if (actorType == "Administrator" &&
                (!actorId.HasValue || actorId.Value <= 0))
            {
                throw new ArgumentException(
                    "A valid administrator ID is required.");
            }

            var httpContext = _httpContextAccessor.HttpContext;

            string? ipAddress =
                httpContext?.Connection.RemoteIpAddress?.ToString();

            var auditLog = new AuditLog
            {
                ActorType = actorType,
                ActorId = actorId,
                ActorName = actorName.Trim(),
                ActorEmail = actorEmail.Trim(),

                // Populate legacy administrator fields only
                // when an administrator performs the activity.
                AdminUserId = actorType == "Administrator"
                    ? actorId
                    : null,

                AdminName = actorType == "Administrator"
                    ? actorName.Trim()
                    : string.Empty,

                AdminEmail = actorType == "Administrator"
                    ? actorEmail.Trim()
                    : string.Empty,

                Action = action.Trim(),
                Module = module.Trim(),
                Description = description.Trim(),
                EntityType = entityType?.Trim(),
                EntityId = entityId?.Trim(),

                IpAddress = ipAddress?.Length > 50
                    ? ipAddress[..50]
                    : ipAddress,

                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
        }

        private static void ValidateFields(
            string actorType,
            string actorName,
            string actorEmail,
            string action,
            string module,
            string description,
            string? entityType,
            string? entityId)
        {
            if (string.IsNullOrWhiteSpace(actorType) ||
                string.IsNullOrWhiteSpace(action) ||
                string.IsNullOrWhiteSpace(module) ||
                string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException(
                    "Actor type, action, module and description are required.");
            }

            if (actorType != "Customer" &&
                actorType != "Administrator" &&
                actorType != "System")
            {
                throw new ArgumentException(
                    "Invalid audit actor type.");
            }

            if (actorName.Length > 150 ||
                actorEmail.Length > 150 ||
                actorType.Length > 20 ||
                action.Length > 50 ||
                module.Length > 100 ||
                description.Length > 1000 ||
                entityType?.Length > 100 ||
                entityId?.Length > 100)
            {
                throw new ArgumentException(
                    "One or more audit log fields exceed their maximum length.");
            }
        }
    }
}