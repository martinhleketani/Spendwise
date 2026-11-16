namespace SpendWise.API.Services
{
    public interface IAuditLogService
    {
        // For authenticated customers and administrators.
        Task LogAsync(
            string action,
            string module,
            string description,
            string? entityType = null,
            string? entityId = null);

        // For activities such as registration, before authentication.
        Task LogAsyncForActorAsync(
            string actorType,
            int? actorId,
            string actorName,
            string actorEmail,
            string action,
            string module,
            string description,
            string? entityType = null,
            string? entityId = null);
    }
}