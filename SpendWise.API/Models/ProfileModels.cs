namespace SpendWise.App.Models;


// =====================================================
// PROFILE RESPONSE
// =====================================================

public class ProfileResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } =
        string.Empty;

    public string Email { get; set; } =
        string.Empty;

    public string? PhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }
}


// =====================================================
// UPDATE PROFILE REQUEST
// =====================================================

public class UpdateProfileRequest
{
    public string FullName { get; set; } =
        string.Empty;

    public string Email { get; set; } =
        string.Empty;

    public string? PhoneNumber { get; set; }
}


// =====================================================
// UPDATE PROFILE RESPONSE
// =====================================================

public class UpdateProfileResponse
{
    public string Message { get; set; } =
        string.Empty;

    public ProfileResponse User { get; set; } =
        new();
}