using SpendWise.App.Models;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SpendWise.App.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthStorageService _authStorageService;

    private const string BaseUrl =
        "https://localhost:7284/";

    private readonly JsonSerializerOptions _jsonOptions =
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ApiService()
    {
        _httpClient =
            new HttpClient
            {
                BaseAddress =
                    new Uri(BaseUrl),

                Timeout =
                    TimeSpan.FromSeconds(30)
            };

        _authStorageService =
            new AuthStorageService();
    }


    // =====================================================
    // AUTHENTICATION
    // =====================================================


    // =====================================================
    // REGISTER
    // =====================================================

    public async Task<(
        bool Success,
        string Message)>
        RegisterAsync(
            RegisterRequest request)
    {
        try
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/register",
                    request);

            string responseBody =
                await response.Content
                    .ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return (
                    true,
                    GetResponseMessage(
                        responseBody,
                        "Account created successfully.")
                );
            }

            return (
                false,
                GetErrorMessage(
                    responseBody)
            );
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine(
                $"REGISTER HTTP ERROR: {ex}");

            return (
                false,
                $"Server connection error: {ex.Message}"
            );
        }
        catch (TaskCanceledException)
        {
            return (
                false,
                "The connection to the server timed out."
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"REGISTER ERROR: {ex}");

            return (
                false,
                $"Registration error: {ex.Message}"
            );
        }
    }


    // =====================================================
    // LOGIN
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        LoginResponse? Data)>
        LoginAsync(
            LoginRequest request)
    {
        try
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/login",
                    request);

            string responseBody =
                await response.Content
                    .ReadAsStringAsync();

            Debug.WriteLine(
                $"LOGIN STATUS: {(int)response.StatusCode}");

            Debug.WriteLine(
                $"LOGIN RESPONSE: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                return (
                    false,
                    GetErrorMessage(
                        responseBody),
                    null
                );
            }

            LoginResponse? loginResponse =
                JsonSerializer.Deserialize<LoginResponse>(
                    responseBody,
                    _jsonOptions);

            if (loginResponse == null)
            {
                return (
                    false,
                    "The server returned an empty login response.",
                    null
                );
            }

            if (string.IsNullOrWhiteSpace(
                loginResponse.Token))
            {
                return (
                    false,
                    "The server did not return an authentication token.",
                    null
                );
            }

            return (
                true,
                GetResponseMessage(
                    responseBody,
                    "Login successful."),
                loginResponse
            );
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine(
                $"LOGIN HTTP ERROR: {ex}");

            return (
                false,
                $"Server connection error: {ex.Message}",
                null
            );
        }
        catch (TaskCanceledException)
        {
            return (
                false,
                "The connection to the server timed out.",
                null
            );
        }
        catch (JsonException ex)
        {
            Debug.WriteLine(
                $"LOGIN JSON ERROR: {ex}");

            return (
                false,
                $"Login response error: {ex.Message}",
                null
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"LOGIN ERROR: {ex}");

            return (
                false,
                $"Login error: {ex.Message}",
                null
            );
        }
    }


    // =====================================================
    // FORGOT PASSWORD
    // =====================================================

    public async Task<(
        bool Success,
        string Message)>
        ForgotPasswordAsync(
            ForgotPasswordRequest request)
    {
        try
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(
                    request.Email))
            {
                return (
                    false,
                    "Email address is required."
                );
            }

            request.Email =
                request.Email
                    .Trim()
                    .ToLowerInvariant();

            var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/forgot-password",
                    request);

            string responseBody =
                await response.Content
                    .ReadAsStringAsync();

            Debug.WriteLine(
                $"FORGOT PASSWORD STATUS: {(int)response.StatusCode}");

            Debug.WriteLine(
                $"FORGOT PASSWORD RESPONSE: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                return (
                    false,
                    GetErrorMessage(
                        responseBody)
                );
            }

            return (
                true,
                GetResponseMessage(
                    responseBody,
                    "If an account exists with this email, a password reset code will be sent.")
            );
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine(
                $"FORGOT PASSWORD HTTP ERROR: {ex}");

            return (
                false,
                $"Server connection error: {ex.Message}"
            );
        }
        catch (TaskCanceledException)
        {
            return (
                false,
                "The connection to the server timed out."
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"FORGOT PASSWORD ERROR: {ex}");

            return (
                false,
                $"Unable to request password reset: {ex.Message}"
            );
        }
    }


    // =====================================================
    // VERIFY RESET CODE
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        VerifyResetCodeResponse? Data)>
        VerifyResetCodeAsync(
            VerifyResetCodeRequest request)
    {
        try
        {
            if (request == null)
            {
                return (
                    false,
                    "Verification information is required.",
                    null
                );
            }

            if (string.IsNullOrWhiteSpace(
                request.Email))
            {
                return (
                    false,
                    "Email address is required.",
                    null
                );
            }

            if (string.IsNullOrWhiteSpace(
                request.Code))
            {
                return (
                    false,
                    "Verification code is required.",
                    null
                );
            }

            request.Email =
                request.Email
                    .Trim()
                    .ToLowerInvariant();

            request.Code =
                request.Code.Trim();

            var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/verify-reset-code",
                    request);

            string responseBody =
                await response.Content
                    .ReadAsStringAsync();

            Debug.WriteLine(
                $"VERIFY RESET CODE STATUS: {(int)response.StatusCode}");

            Debug.WriteLine(
                $"VERIFY RESET CODE RESPONSE: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                return (
                    false,
                    GetErrorMessage(
                        responseBody),
                    null
                );
            }

            VerifyResetCodeResponse? data =
                JsonSerializer.Deserialize<
                    VerifyResetCodeResponse>(
                        responseBody,
                        _jsonOptions);

            if (data == null)
            {
                return (
                    false,
                    "The server returned an empty verification response.",
                    null
                );
            }

            if (string.IsNullOrWhiteSpace(
                data.ResetToken))
            {
                return (
                    false,
                    "The server did not return a password reset token.",
                    null
                );
            }

            return (
                true,
                GetResponseMessage(
                    responseBody,
                    "Reset code verified successfully."),
                data
            );
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine(
                $"VERIFY RESET CODE HTTP ERROR: {ex}");

            return (
                false,
                $"Server connection error: {ex.Message}",
                null
            );
        }
        catch (TaskCanceledException)
        {
            return (
                false,
                "The connection to the server timed out.",
                null
            );
        }
        catch (JsonException ex)
        {
            Debug.WriteLine(
                $"VERIFY RESET CODE JSON ERROR: {ex}");

            return (
                false,
                $"Verification response error: {ex.Message}",
                null
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"VERIFY RESET CODE ERROR: {ex}");

            return (
                false,
                $"Unable to verify reset code: {ex.Message}",
                null
            );
        }
    }


    // =====================================================
    // RESET PASSWORD
    // =====================================================

    public async Task<(
        bool Success,
        string Message)>
        ResetPasswordAsync(
            ResetPasswordRequest request)
    {
        try
        {
            if (request == null)
            {
                return (
                    false,
                    "Password reset information is required."
                );
            }

            if (string.IsNullOrWhiteSpace(
                request.ResetToken))
            {
                return (
                    false,
                    "Reset token is required."
                );
            }

            if (string.IsNullOrWhiteSpace(
                request.NewPassword))
            {
                return (
                    false,
                    "New password is required."
                );
            }

            var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/reset-password",
                    request);

            string responseBody =
                await response.Content
                    .ReadAsStringAsync();

            Debug.WriteLine(
                $"RESET PASSWORD STATUS: {(int)response.StatusCode}");

            Debug.WriteLine(
                $"RESET PASSWORD RESPONSE: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                return (
                    false,
                    GetErrorMessage(
                        responseBody)
                );
            }

            return (
                true,
                GetResponseMessage(
                    responseBody,
                    "Password reset successfully. You can now log in with your new password.")
            );
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine(
                $"RESET PASSWORD HTTP ERROR: {ex}");

            return (
                false,
                $"Server connection error: {ex.Message}"
            );
        }
        catch (TaskCanceledException)
        {
            return (
                false,
                "The connection to the server timed out."
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"RESET PASSWORD ERROR: {ex}");

            return (
                false,
                $"Unable to reset password: {ex.Message}"
            );
        }
    }


    // =====================================================
    // DASHBOARD
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        DashboardResponse? Data)>
        GetDashboardAsync(
            int year,
            int month)
    {
        return await GetAuthorizedAsync<
            DashboardResponse>(
                $"api/Dashboard/{year}/{month}",
                "Dashboard loaded successfully.",
                "Unable to load dashboard");
    }


    // =====================================================
    // BUDGET
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        CreateBudgetResponse? Data)>
        CreateBudgetAsync(
            CreateBudgetRequest request)
    {
        return await PostAuthorizedAsync<
            CreateBudgetRequest,
            CreateBudgetResponse>(
                "api/Budget",
                request,
                "Budget created successfully.",
                "Unable to create budget");
    }


    public async Task<(
        bool Success,
        string Message,
        BudgetResponse? Data)>
        GetBudgetAsync(
            int year,
            int month)
    {
        return await GetAuthorizedAsync<
            BudgetResponse>(
                $"api/Budget/{year}/{month}",
                "Budget loaded successfully.",
                "Unable to load budget");
    }


    public async Task<(
        bool Success,
        string Message,
        BudgetAlertsResponse? Data)>
        GetBudgetAlertsAsync(
            int year,
            int month)
    {
        return await GetAuthorizedAsync<
            BudgetAlertsResponse>(
                $"api/Budget/alerts/{year}/{month}",
                "Budget alerts loaded successfully.",
                "Unable to load budget alerts");
    }


    // =====================================================
    // TRANSACTIONS
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        List<TransactionResponse>? Data)>
        GetTransactionsAsync()
    {
        return await GetAuthorizedAsync<
            List<TransactionResponse>>(
                "api/Transactions",
                "Transactions loaded successfully.",
                "Unable to load transactions");
    }


    // =====================================================
    // GROCERY
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        CreateGroceryListResponse? Data)>
        CreateGroceryListAsync(
            CreateGroceryListRequest request)
    {
        return await PostAuthorizedAsync<
            CreateGroceryListRequest,
            CreateGroceryListResponse>(
                "api/Grocery",
                request,
                "Grocery list created successfully.",
                "Unable to create grocery list");
    }


    public async Task<(
        bool Success,
        string Message,
        List<GroceryListSummary>? Data)>
        GetGroceryListsAsync()
    {
        return await GetAuthorizedAsync<
            List<GroceryListSummary>>(
                "api/Grocery",
                "Grocery lists loaded successfully.",
                "Unable to load grocery lists");
    }


    public async Task<(
        bool Success,
        string Message,
        GroceryListResponse? Data)>
        GetGroceryListAsync(
            int groceryListId)
    {
        return await GetAuthorizedAsync<
            GroceryListResponse>(
                $"api/Grocery/{groceryListId}",
                "Grocery list loaded successfully.",
                "Unable to load grocery list");
    }


    public async Task<(
        bool Success,
        string Message,
        AddGroceryItemResponse? Data)>
        AddGroceryItemAsync(
            int groceryListId,
            AddGroceryItemRequest request)
    {
        return await PostAuthorizedAsync<
            AddGroceryItemRequest,
            AddGroceryItemResponse>(
                $"api/Grocery/{groceryListId}/items",
                request,
                "Item added successfully.",
                "Unable to add grocery item");
    }


    public async Task<(
        bool Success,
        string Message,
        GroceryBudgetCheckResponse? Data)>
        CheckGroceryBudgetAsync(
            int groceryListId,
            int month,
            int year)
    {
        GroceryBudgetCheckRequest request =
            new GroceryBudgetCheckRequest
            {
                Month = month,
                Year = year
            };

        return await PostAuthorizedAsync<
            GroceryBudgetCheckRequest,
            GroceryBudgetCheckResponse>(
                $"api/Grocery/{groceryListId}/budget-check",
                request,
                "Grocery budget checked successfully.",
                "Unable to check grocery budget");
    }


    public async Task<(
        bool Success,
        string Message,
        GroceryCheckoutResponse? Data)>
        CheckoutGroceryAsync(
            int groceryListId,
            int month,
            int year)
    {
        GroceryCheckoutRequest request =
            new GroceryCheckoutRequest
            {
                Month = month,
                Year = year
            };

        return await PostAuthorizedAsync<
            GroceryCheckoutRequest,
            GroceryCheckoutResponse>(
                $"api/Grocery/{groceryListId}/checkout",
                request,
                "Purchase recorded successfully.",
                "Unable to record grocery purchase");
    }


    public async Task<(
        bool Success,
        string Message)>
        DeleteGroceryItemAsync(
            int groceryListId,
            int groceryItemId)
    {
        return await DeleteAuthorizedAsync(
            $"api/Grocery/{groceryListId}/items/{groceryItemId}",
            "Grocery item deleted successfully.",
            "Unable to delete grocery item");
    }


    public async Task<(
        bool Success,
        string Message)>
        DeleteGroceryListAsync(
            int groceryListId)
    {
        return await DeleteAuthorizedAsync(
            $"api/Grocery/{groceryListId}",
            "Grocery list deleted successfully.",
            "Unable to delete grocery list");
    }


    // =====================================================
    // RETAILER PRICE COMPARISON
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        RetailerPriceComparisonResponse? Data)>
        GetRetailerPriceComparisonAsync(
            int groceryListId)
    {
        return await GetAuthorizedAsync<
            RetailerPriceComparisonResponse>(
                $"api/Grocery/{groceryListId}/price-comparison",
                "Retailer prices loaded successfully.",
                "Unable to load retailer price comparison");
    }


    // =====================================================
    // PURCHASE
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        CreatePurchaseResponse? Data)>
        CreatePurchaseAsync(
            CreatePurchaseRequest request)
    {
        return await PostAuthorizedAsync<
            CreatePurchaseRequest,
            CreatePurchaseResponse>(
                "api/Purchase",
                request,
                "Purchase recorded successfully.",
                "Unable to record purchase");
    }


    public async Task<(
        bool Success,
        string Message,
        PurchaseHistoryResponse? Data)>
        GetPurchasesAsync(
            string? category = null,
            string? search = null,
            int? month = null,
            int? year = null,
            decimal? minAmount = null,
            decimal? maxAmount = null)
    {
        try
        {
            List<string> parameters =
                new List<string>();

            if (!string.IsNullOrWhiteSpace(
                category))
            {
                parameters.Add(
                    $"category={Uri.EscapeDataString(category)}");
            }

            if (!string.IsNullOrWhiteSpace(
                search))
            {
                parameters.Add(
                    $"search={Uri.EscapeDataString(search)}");
            }

            if (month.HasValue)
            {
                parameters.Add(
                    $"month={month.Value}");
            }

            if (year.HasValue)
            {
                parameters.Add(
                    $"year={year.Value}");
            }

            if (minAmount.HasValue)
            {
                parameters.Add(
                    "minAmount=" +
                    minAmount.Value.ToString(
                        CultureInfo.InvariantCulture));
            }

            if (maxAmount.HasValue)
            {
                parameters.Add(
                    "maxAmount=" +
                    maxAmount.Value.ToString(
                        CultureInfo.InvariantCulture));
            }

            string url =
                "api/Purchase";

            if (parameters.Count > 0)
            {
                url +=
                    "?" +
                    string.Join(
                        "&",
                        parameters);
            }

            return await GetAuthorizedAsync<
                PurchaseHistoryResponse>(
                    url,
                    "Purchase history loaded successfully.",
                    "Unable to load purchase history");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"GET PURCHASES ERROR: {ex}");

            return (
                false,
                $"Unable to load purchase history: {ex.Message}",
                null
            );
        }
    }


    // =====================================================
         // =====================================================
    // REPORTS
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        MonthlyReportResponse? Data)>
        GetMonthlyReportAsync(
            int year,
            int month)
    {
        if (month < 1 ||
            month > 12)
        {
            return (
                false,
                "Month must be between 1 and 12.",
                null
            );
        }

        return await GetAuthorizedAsync<
            MonthlyReportResponse>(
                $"api/Reports/monthly/{year}/{month}",
                "Monthly report loaded successfully.",
                "Unable to load monthly report");
    }


    public async Task<(
        bool Success,
        string Message,
        RetailerReportResponse? Data)>
        GetRetailerReportAsync(
            int year,
            int month)
    {
        if (month < 1 ||
            month > 12)
        {
            return (
                false,
                "Month must be between 1 and 12.",
                null
            );
        }

        return await GetAuthorizedAsync<
            RetailerReportResponse>(
                $"api/Reports/retailers/{year}/{month}",
                "Retailer report loaded successfully.",
                "Unable to load retailer report");
    }


    public async Task<(
        bool Success,
        string Message,
        DateRangeReportResponse? Data)>
        GetDateRangeReportAsync(
            DateTime startDate,
            DateTime endDate)
    {
        if (startDate.Date >
            endDate.Date)
        {
            return (
                false,
                "Start date cannot be after end date.",
                null
            );
        }

        string start =
            startDate.Date.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

        string end =
            endDate.Date.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

        string url =
            "api/Reports/range" +
            $"?startDate={Uri.EscapeDataString(start)}" +
            $"&endDate={Uri.EscapeDataString(end)}";

        return await GetAuthorizedAsync<
            DateRangeReportResponse>(
                url,
                "Date range report loaded successfully.",
                "Unable to load date range report");
    }


    // =====================================================
    // INSIGHTS
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        InsightsResponse? Data)>
        GetInsightsAsync(
            int year,
            int month)
    {
        if (month < 1 ||
            month > 12)
        {
            return (
                false,
                "Month must be between 1 and 12.",
                null
            );
        }

        if (year < 2020 ||
            year > 2100)
        {
            return (
                false,
                "Invalid year.",
                null
            );
        }

        return await GetAuthorizedAsync<
            InsightsResponse>(
                $"api/Insights/{year}/{month}",
                "Insights loaded successfully.",
                "Unable to load insights");
    }


    // =====================================================
    // PROFILE
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        ProfileResponse? Data)>
        GetProfileAsync()
    {
        return await GetAuthorizedAsync<
            ProfileResponse>(
                "api/Profile",
                "Profile loaded successfully.",
                "Unable to load profile");
    }


    public async Task<(
        bool Success,
        string Message,
        UpdateProfileResponse? Data)>
        UpdateProfileAsync(
            UpdateProfileRequest request)
    {
        if (request == null)
        {
            return (
                false,
                "Profile information is required.",
                null
            );
        }

        if (string.IsNullOrWhiteSpace(
            request.FullName))
        {
            return (
                false,
                "Full name is required.",
                null
            );
        }

        if (string.IsNullOrWhiteSpace(
            request.Email))
        {
            return (
                false,
                "Email is required.",
                null
            );
        }

        request.FullName =
            request.FullName.Trim();

        request.Email =
            request.Email
                .Trim()
                .ToLowerInvariant();

        request.PhoneNumber =
            string.IsNullOrWhiteSpace(
                request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim();

        return await PutAuthorizedAsync<
            UpdateProfileRequest,
            UpdateProfileResponse>(
                "api/Profile",
                request,
                "Profile updated successfully.",
                "Unable to update profile");
    }


    // =====================================================
    // SHARED API HELPERS
    // =====================================================


    // =====================================================
    // AUTHORIZED GET
    // =====================================================

    private async Task<(
        bool Success,
        string Message,
        T? Data)>
        GetAuthorizedAsync<T>(
            string url,
            string successMessage,
            string errorPrefix)
    {
        try
        {
            string? token =
                await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(
                token))
            {
                return (
                    false,
                    "You are not logged in.",
                    default
                );
            }

            SetAuthorizationHeader(
                token);

            var response =
                await _httpClient.GetAsync(
                    url);

            string body =
                await response.Content
                    .ReadAsStringAsync();

            Debug.WriteLine(
                $"GET {url} STATUS: {(int)response.StatusCode}");

            Debug.WriteLine(
                $"GET {url} RESPONSE: {body}");

            if (!response.IsSuccessStatusCode)
            {
                return (
                    false,
                    GetErrorMessage(
                        body),
                    default
                );
            }

            T? data =
                JsonSerializer.Deserialize<T>(
                    body,
                    _jsonOptions);

            if (data == null)
            {
                return (
                    false,
                    "The server returned an empty response.",
                    default
                );
            }

            return (
                true,
                successMessage,
                data
            );
        }
        catch (TaskCanceledException)
        {
            return (
                false,
                "The connection to the server timed out.",
                default
            );
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine(
                $"{errorPrefix.ToUpperInvariant()} HTTP ERROR: {ex}");

            return (
                false,
                $"Server connection error: {ex.Message}",
                default
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"{errorPrefix.ToUpperInvariant()} ERROR: {ex}");

            return (
                false,
                $"{errorPrefix}: {ex.Message}",
                default
            );
        }
    }


    // =====================================================
    // AUTHORIZED POST
    // =====================================================

    private async Task<(
        bool Success,
        string Message,
        TResponse? Data)>
        PostAuthorizedAsync<TRequest, TResponse>(
            string url,
            TRequest request,
            string successMessage,
            string errorPrefix)
    {
        try
        {
            string? token =
                await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(
                token))
            {
                return (
                    false,
                    "You are not logged in.",
                    default
                );
            }

            SetAuthorizationHeader(
                token);

            var response =
                await _httpClient.PostAsJsonAsync(
                    url,
                    request);

            string body =
                await response.Content
                    .ReadAsStringAsync();

            Debug.WriteLine(
                $"POST {url} STATUS: {(int)response.StatusCode}");

            Debug.WriteLine(
                $"POST {url} RESPONSE: {body}");

            if (!response.IsSuccessStatusCode)
            {
                return (
                    false,
                    GetErrorMessage(
                        body),
                    default
                );
            }

            TResponse? data =
                JsonSerializer.Deserialize<TResponse>(
                    body,
                    _jsonOptions);

            if (data == null)
            {
                return (
                    false,
                    "The server returned an empty response.",
                    default
                );
            }

            string message =
                GetResponseMessage(
                    body,
                    successMessage);

            return (
                true,
                message,
                data
            );
        }
        catch (TaskCanceledException)
        {
            return (
                false,
                "The connection to the server timed out.",
                default
            );
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine(
                $"{errorPrefix.ToUpperInvariant()} HTTP ERROR: {ex}");

            return (
                false,
                $"Server connection error: {ex.Message}",
                default
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"{errorPrefix.ToUpperInvariant()} ERROR: {ex}");

            return (
                false,
                $"{errorPrefix}: {ex.Message}",
                default
            );
        }
    }


    // =====================================================
    // AUTHORIZED PUT
    // =====================================================

    private async Task<(
        bool Success,
        string Message,
        TResponse? Data)>
        PutAuthorizedAsync<TRequest, TResponse>(
            string url,
            TRequest request,
            string successMessage,
            string errorPrefix)
    {
        try
        {
            string? token =
                await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(
                token))
            {
                return (
                    false,
                    "You are not logged in.",
                    default
                );
            }

            SetAuthorizationHeader(
                token);

            var response =
                await _httpClient.PutAsJsonAsync(
                    url,
                    request);

            string body =
                await response.Content
                    .ReadAsStringAsync();

            Debug.WriteLine(
                $"PUT {url} STATUS: {(int)response.StatusCode}");

            Debug.WriteLine(
                $"PUT {url} RESPONSE: {body}");

            if (!response.IsSuccessStatusCode)
            {
                return (
                    false,
                    GetErrorMessage(
                        body),
                    default
                );
            }

            TResponse? data =
                JsonSerializer.Deserialize<TResponse>(
                    body,
                    _jsonOptions);

            if (data == null)
            {
                return (
                    false,
                    "The server returned an empty response.",
                    default
                );
            }

            string message =
                GetResponseMessage(
                    body,
                    successMessage);

            return (
                true,
                message,
                data
            );
        }
        catch (TaskCanceledException)
        {
            return (
                false,
                "The connection to the server timed out.",
                default
            );
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine(
                $"{errorPrefix.ToUpperInvariant()} HTTP ERROR: {ex}");

            return (
                false,
                $"Server connection error: {ex.Message}",
                default
            );
        }
        catch (JsonException ex)
        {
            Debug.WriteLine(
                $"{errorPrefix.ToUpperInvariant()} JSON ERROR: {ex}");

            return (
                false,
                $"Server response error: {ex.Message}",
                default
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"{errorPrefix.ToUpperInvariant()} ERROR: {ex}");

            return (
                false,
                $"{errorPrefix}: {ex.Message}",
                default
            );
        }
    }


    // =====================================================
    // AUTHORIZED DELETE
    // =====================================================

    private async Task<(
        bool Success,
        string Message)>
        DeleteAuthorizedAsync(
            string url,
            string successMessage,
            string errorPrefix)
    {
        try
        {
            string? token =
                await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(
                token))
            {
                return (
                    false,
                    "You are not logged in."
                );
            }

            SetAuthorizationHeader(
                token);

            var response =
                await _httpClient.DeleteAsync(
                    url);

            string body =
                await response.Content
                    .ReadAsStringAsync();

            Debug.WriteLine(
                $"DELETE {url} STATUS: {(int)response.StatusCode}");

            Debug.WriteLine(
                $"DELETE {url} RESPONSE: {body}");

            if (!response.IsSuccessStatusCode)
            {
                return (
                    false,
                    GetErrorMessage(
                        body)
                );
            }

            return (
                true,
                GetResponseMessage(
                    body,
                    successMessage)
            );
        }
        catch (TaskCanceledException)
        {
            return (
                false,
                "The connection to the server timed out."
            );
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine(
                $"{errorPrefix.ToUpperInvariant()} HTTP ERROR: {ex}");

            return (
                false,
                $"Server connection error: {ex.Message}"
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"{errorPrefix.ToUpperInvariant()} ERROR: {ex}");

            return (
                false,
                $"{errorPrefix}: {ex.Message}"
            );
        }
    }


    // =====================================================
    // BASIC HELPERS
    // =====================================================

    private async Task<string?> GetTokenAsync()
    {
        return await _authStorageService
            .GetTokenAsync();
    }


    private void SetAuthorizationHeader(
        string token)
    {
        _httpClient
            .DefaultRequestHeaders
            .Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }


    // =====================================================
    // GET RESPONSE MESSAGE
    // =====================================================

    private string GetResponseMessage(
        string response,
        string defaultMessage)
    {
        if (string.IsNullOrWhiteSpace(
            response))
        {
            return defaultMessage;
        }

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(
                    response);

            JsonElement root =
                document.RootElement;

            if (root.ValueKind ==
                    JsonValueKind.Object &&
                root.TryGetProperty(
                    "message",
                    out JsonElement message))
            {
                string? value =
                    message.GetString();

                if (!string.IsNullOrWhiteSpace(
                    value))
                {
                    return value;
                }
            }
        }
        catch
        {
            // Ignore parsing failure.
        }

        return defaultMessage;
    }


    // =====================================================
    // GET ERROR MESSAGE
    // =====================================================

    private static string GetErrorMessage(
        string response)
    {
        if (string.IsNullOrWhiteSpace(
            response))
        {
            return
                "The request could not be completed.";
        }

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(
                    response);

            JsonElement root =
                document.RootElement;


            if (root.ValueKind ==
                    JsonValueKind.Object &&
                root.TryGetProperty(
                    "message",
                    out JsonElement message))
            {
                return message.GetString()
                    ?? "The request could not be completed.";
            }


            if (root.ValueKind ==
                    JsonValueKind.Object &&
                root.TryGetProperty(
                    "title",
                    out JsonElement title))
            {
                return title.GetString()
                    ?? "The request could not be completed.";
            }


            if (root.ValueKind ==
                    JsonValueKind.Object &&
                root.TryGetProperty(
                    "errors",
                    out JsonElement errors) &&
                errors.ValueKind ==
                    JsonValueKind.Object)
            {
                foreach (
                    JsonProperty property
                    in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind ==
                        JsonValueKind.Array)
                    {
                        foreach (
                            JsonElement error
                            in property.Value
                                .EnumerateArray())
                        {
                            string? validationMessage =
                                error.GetString();

                            if (!string.IsNullOrWhiteSpace(
                                validationMessage))
                            {
                                return validationMessage;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Response was not JSON.
        }

        return response;
    }
}
    // =====================================================