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
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public ReportsController(
            AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // MONTHLY SPENDING REPORT
        //
        // GET:
        // /api/Reports/monthly/2026/9
        // =====================================================

        [HttpGet("monthly/{year:int}/{month:int}")]
        public async Task<IActionResult> GetMonthlyReport(
            int year,
            int month)
        {
            int? userId =
                GetCurrentUserId();


            if (userId == null)
            {
                return Unauthorized();
            }


            // =================================================
            // VALIDATE MONTH
            // =================================================

            if (month < 1 ||
                month > 12)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Month must be between 1 and 12."
                    });
            }


            // =================================================
            // VALIDATE YEAR
            // =================================================

            if (year < 2020 ||
                year > 2100)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Invalid year."
                    });
            }


            // =================================================
            // GET USER PURCHASE TRANSACTIONS
            //
            // Transactions belong directly to UserId.
            // No Wallet or WalletId is used.
            // =================================================

            var transactions =
                await _context
                    .Transactions
                    .AsNoTracking()
                    .Where(
                        t =>
                            t.UserId ==
                                userId.Value &&
                            t.Type ==
                                "Purchase" &&
                            t.CreatedAt.Year ==
                                year &&
                            t.CreatedAt.Month ==
                                month)
                    .ToListAsync();


            // =================================================
            // SUMMARY
            // =================================================

            decimal totalSpent =
                transactions.Sum(
                    t =>
                        Math.Abs(
                            t.Amount));


            int transactionCount =
                transactions.Count;


            decimal averagePurchase =
                transactionCount > 0
                    ? totalSpent /
                        transactionCount
                    : 0m;


            decimal biggestPurchase =
                transactionCount > 0
                    ? transactions.Max(
                        t =>
                            Math.Abs(
                                t.Amount))
                    : 0m;


            // =================================================
            // CATEGORY BREAKDOWN
            // =================================================

            var categoryGroups =
                transactions
                    .GroupBy(
                        t =>
                            string.IsNullOrWhiteSpace(
                                t.Category)
                                ? "Uncategorized"
                                : t.Category!)
                    .Select(
                        group =>
                        {
                            decimal amount =
                                group.Sum(
                                    t =>
                                        Math.Abs(
                                            t.Amount));


                            decimal percentage =
                                totalSpent > 0
                                    ? Math.Round(
                                        (
                                            amount /
                                            totalSpent
                                        ) * 100,
                                        2)
                                    : 0m;


                            return new
                            {
                                category =
                                    group.Key,

                                amount,

                                percentage,

                                transactionCount =
                                    group.Count()
                            };
                        })
                    .OrderByDescending(
                        x =>
                            x.amount)
                    .ToList();


            // =================================================
            // BIGGEST TRANSACTION
            // =================================================

            var biggestTransaction =
                transactions
                    .OrderByDescending(
                        t =>
                            Math.Abs(
                                t.Amount))
                    .FirstOrDefault();


            // =================================================
            // RETURN MONTHLY REPORT
            // =================================================

            return Ok(
                new
                {
                    month,

                    year,

                    summary =
                        new
                        {
                            totalSpent,

                            transactionCount,

                            averagePurchase =
                                Math.Round(
                                    averagePurchase,
                                    2),

                            biggestPurchase
                        },

                    categories =
                        categoryGroups,

                    biggestTransaction =
                        biggestTransaction == null
                            ? null
                            : new
                            {
                                biggestTransaction.Id,

                                biggestTransaction.Category,

                                biggestTransaction.Retailer,

                                amount =
                                    Math.Abs(
                                        biggestTransaction.Amount),

                                biggestTransaction.Description,

                                biggestTransaction.CreatedAt
                            }
                });
        }


        // =====================================================
        // RETAILER SPENDING REPORT
        //
        // GET:
        // /api/Reports/retailers/2026/9
        // =====================================================

        [HttpGet("retailers/{year:int}/{month:int}")]
        public async Task<IActionResult> GetRetailerReport(
            int year,
            int month)
        {
            int? userId =
                GetCurrentUserId();


            if (userId == null)
            {
                return Unauthorized();
            }


            // =================================================
            // VALIDATE MONTH
            // =================================================

            if (month < 1 ||
                month > 12)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Month must be between 1 and 12."
                    });
            }


            // =================================================
            // VALIDATE YEAR
            // =================================================

            if (year < 2020 ||
                year > 2100)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Invalid year."
                    });
            }


            // =================================================
            // GET USER RETAILER PURCHASES
            // =================================================

            var transactions =
                await _context
                    .Transactions
                    .AsNoTracking()
                    .Where(
                        t =>
                            t.UserId ==
                                userId.Value &&
                            t.Type ==
                                "Purchase" &&
                            t.Retailer !=
                                null &&
                            t.Retailer !=
                                "" &&
                            t.CreatedAt.Year ==
                                year &&
                            t.CreatedAt.Month ==
                                month)
                    .ToListAsync();


            // =================================================
            // SUMMARY
            // =================================================

            decimal totalSpentAtRetailers =
                transactions.Sum(
                    t =>
                        Math.Abs(
                            t.Amount));


            int transactionCount =
                transactions.Count;


            decimal averageRetailerPurchase =
                transactionCount > 0
                    ? totalSpentAtRetailers /
                        transactionCount
                    : 0m;


            // =================================================
            // RETAILER BREAKDOWN
            // =================================================

            var retailerGroups =
                transactions
                    .GroupBy(
                        t =>
                            t.Retailer!)
                    .Select(
                        group =>
                        {
                            decimal amount =
                                group.Sum(
                                    t =>
                                        Math.Abs(
                                            t.Amount));


                            decimal percentage =
                                totalSpentAtRetailers > 0
                                    ? Math.Round(
                                        (
                                            amount /
                                            totalSpentAtRetailers
                                        ) * 100,
                                        2)
                                    : 0m;


                            return new
                            {
                                retailer =
                                    group.Key,

                                amount,

                                percentage,

                                transactionCount =
                                    group.Count()
                            };
                        })
                    .OrderByDescending(
                        x =>
                            x.amount)
                    .ToList();


            var highestSpendingRetailer =
                retailerGroups
                    .FirstOrDefault();


            // =================================================
            // RETURN RETAILER REPORT
            // =================================================

            return Ok(
                new
                {
                    month,

                    year,

                    summary =
                        new
                        {
                            totalSpentAtRetailers,

                            transactionCount,

                            averageRetailerPurchase =
                                Math.Round(
                                    averageRetailerPurchase,
                                    2),

                            retailerCount =
                                retailerGroups.Count
                        },

                    retailers =
                        retailerGroups,

                    highestSpendingRetailer
                });
        }


        // =====================================================
        // DATE-RANGE SPENDING REPORT
        //
        // Example:
        // /api/Reports/range
        // ?startDate=2026-09-01&endDate=2026-09-30
        // =====================================================

        [HttpGet("range")]
        public async Task<IActionResult> GetDateRangeReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            int? userId =
                GetCurrentUserId();


            if (userId == null)
            {
                return Unauthorized();
            }


            // =================================================
            // VALIDATE DATES
            // =================================================

            if (startDate == default)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Start date is required."
                    });
            }


            if (endDate == default)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "End date is required."
                    });
            }


            if (startDate.Date >
                endDate.Date)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Start date cannot be after end date."
                    });
            }


            // =================================================
            // DATE RANGE
            // =================================================

            DateTime start =
                startDate.Date;


            // Makes the selected end date inclusive.
            DateTime endExclusive =
                endDate
                    .Date
                    .AddDays(1);


            // =================================================
            // GET USER PURCHASE TRANSACTIONS
            // =================================================

            var transactions =
                await _context
                    .Transactions
                    .AsNoTracking()
                    .Where(
                        t =>
                            t.UserId ==
                                userId.Value &&
                            t.Type ==
                                "Purchase" &&
                            t.CreatedAt >=
                                start &&
                            t.CreatedAt <
                                endExclusive)
                    .OrderByDescending(
                        t =>
                            t.CreatedAt)
                    .ToListAsync();


            // =================================================
            // SUMMARY
            // =================================================

            decimal totalSpent =
                transactions.Sum(
                    t =>
                        Math.Abs(
                            t.Amount));


            int transactionCount =
                transactions.Count;


            decimal averagePurchase =
                transactionCount > 0
                    ? totalSpent /
                        transactionCount
                    : 0m;


            decimal biggestPurchase =
                transactionCount > 0
                    ? transactions.Max(
                        t =>
                            Math.Abs(
                                t.Amount))
                    : 0m;


            // =================================================
            // CATEGORY BREAKDOWN
            // =================================================

            var categoryGroups =
                transactions
                    .GroupBy(
                        t =>
                            string.IsNullOrWhiteSpace(
                                t.Category)
                                ? "Uncategorized"
                                : t.Category!)
                    .Select(
                        group =>
                        {
                            decimal amount =
                                group.Sum(
                                    t =>
                                        Math.Abs(
                                            t.Amount));


                            decimal percentage =
                                totalSpent > 0
                                    ? Math.Round(
                                        (
                                            amount /
                                            totalSpent
                                        ) * 100,
                                        2)
                                    : 0m;


                            return new
                            {
                                category =
                                    group.Key,

                                amount,

                                percentage,

                                transactionCount =
                                    group.Count()
                            };
                        })
                    .OrderByDescending(
                        x =>
                            x.amount)
                    .ToList();


            // =================================================
            // RETAILER BREAKDOWN
            // =================================================

            var retailerTransactions =
                transactions
                    .Where(
                        t =>
                            !string
                                .IsNullOrWhiteSpace(
                                    t.Retailer))
                    .ToList();


            decimal retailerTotal =
                retailerTransactions.Sum(
                    t =>
                        Math.Abs(
                            t.Amount));


            var retailerGroups =
                retailerTransactions
                    .GroupBy(
                        t =>
                            t.Retailer!)
                    .Select(
                        group =>
                        {
                            decimal amount =
                                group.Sum(
                                    t =>
                                        Math.Abs(
                                            t.Amount));


                            decimal percentage =
                                retailerTotal > 0
                                    ? Math.Round(
                                        (
                                            amount /
                                            retailerTotal
                                        ) * 100,
                                        2)
                                    : 0m;


                            return new
                            {
                                retailer =
                                    group.Key,

                                amount,

                                percentage,

                                transactionCount =
                                    group.Count()
                            };
                        })
                    .OrderByDescending(
                        x =>
                            x.amount)
                    .ToList();


            // =================================================
            // TRANSACTION LIST
            // =================================================

            var transactionList =
                transactions
                    .Select(
                        t =>
                            new
                            {
                                t.Id,

                                t.Category,

                                t.Retailer,

                                amount =
                                    Math.Abs(
                                        t.Amount),

                                t.Description,

                                t.Reference,

                                t.CreatedAt
                            })
                    .ToList();


            // =================================================
            // RETURN DATE-RANGE REPORT
            // =================================================

            return Ok(
                new
                {
                    startDate =
                        startDate.Date,

                    endDate =
                        endDate.Date,

                    summary =
                        new
                        {
                            totalSpent,

                            transactionCount,

                            averagePurchase =
                                Math.Round(
                                    averagePurchase,
                                    2),

                            biggestPurchase
                        },

                    categories =
                        categoryGroups,

                    retailers =
                        retailerGroups,

                    transactions =
                        transactionList
                });
        }


        // =====================================================
        // GET CURRENT LOGGED-IN USER
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