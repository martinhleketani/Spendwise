using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using System.Security.Claims;

namespace SpendWise.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public TransactionsController(
            AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // GET TRANSACTIONS
        //
        // GET: /api/Transactions
        //
        // Returns the logged-in user's recorded
        // spending transactions.
        //
        // Transactions belong directly to the user.
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            GetTransactions()
        {
            int? userId =
                GetCurrentUserId();


            if (userId == null)
            {
                return Unauthorized(
                    new
                    {
                        message =
                            "Invalid user authentication."
                    });
            }


            var transactions =
                await _context
                    .Transactions
                    .AsNoTracking()
                    .Where(
                        transaction =>
                            transaction.UserId ==
                            userId.Value)
                    .Where(
                        transaction =>
                            transaction.Type !=
                            "Deposit")
                    .OrderByDescending(
                        transaction =>
                            transaction.CreatedAt)
                    .Select(
                        transaction =>
                            new
                            {
                                id =
                                    transaction.Id,

                                amount =
                                    transaction.Amount,

                                type =
                                    transaction.Type,

                                category =
                                    transaction.Category,

                                retailer =
                                    transaction.Retailer,

                                description =
                                    transaction.Description,

                                reference =
                                    transaction.Reference,

                                requestId =
                                    transaction.RequestId,

                                createdAt =
                                    transaction.CreatedAt
                            })
                    .ToListAsync();


            return Ok(
                transactions);
        }


        // =====================================================
        // GET CURRENT USER ID FROM JWT
        // =====================================================

        private int? GetCurrentUserId()
        {
            string? value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);


            if (!int.TryParse(
                    value,
                    out int userId))
            {
                return null;
            }


            return userId;
        }
    }
}