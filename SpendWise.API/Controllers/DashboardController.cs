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
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public DashboardController(
            AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // GET MONTHLY DASHBOARD
        // Example: GET /api/Dashboard/2026/9
        // =====================================================

        [HttpGet("{year:int}/{month:int}")]
        public async Task<IActionResult> GetDashboard(
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
            // MONTHLY BUDGET
            //
            // A user may not have allocated a budget yet.
            // That is a valid zero state.
            // =================================================

            var monthlyBudget =
                await _context
                    .MonthlyBudgets
                    .Include(
                        b =>
                            b.CategoryBudgets)
                    .FirstOrDefaultAsync(
                        b =>
                            b.UserId ==
                                userId.Value &&
                            b.Year ==
                                year &&
                            b.Month ==
                                month);


            decimal totalBudget =
                monthlyBudget?.TotalLimit ?? 0m;


            decimal totalSpent =
                monthlyBudget == null
                    ? 0m
                    : monthlyBudget
                        .CategoryBudgets
                        .Sum(
                            c =>
                                c.SpentAmount);


            decimal totalRemaining =
                totalBudget -
                totalSpent;


            decimal percentageUsed =
                totalBudget > 0
                    ? Math.Round(
                        (totalSpent /
                         totalBudget) * 100,
                        2)
                    : 0m;


            // =================================================
            // CATEGORY BREAKDOWN
            // =================================================

            var categories =
                monthlyBudget == null
                    ? new List<object>()
                    : monthlyBudget
                        .CategoryBudgets
                        .OrderBy(
                            c =>
                                c.Category)
                        .Select(
                            c =>
                                (object)new
                                {
                                    id =
                                        c.Id,

                                    category =
                                        c.Category,

                                    limit =
                                        c.LimitAmount,

                                    spent =
                                        c.SpentAmount,

                                    remaining =
                                        c.LimitAmount -
                                        c.SpentAmount,

                                    percentageUsed =
                                        c.LimitAmount > 0
                                            ? Math.Round(
                                                (
                                                    c.SpentAmount /
                                                    c.LimitAmount
                                                ) * 100,
                                                2)
                                            : 0m
                                })
                        .ToList();


            // =================================================
            // RECENT SPENDING
            //
            // Transactions belong directly to the user.
            // No wallet lookup is required.
            // =================================================

            var recentTransactions =
                await _context
                    .Transactions
                    .AsNoTracking()
                    .Where(
                        t =>
                            t.UserId ==
                                userId.Value &&
                            (
                                t.Type == "Purchase" ||
                                t.Amount < 0
                            ))
                    .OrderByDescending(
                        t =>
                            t.CreatedAt)
                    .Take(5)
                    .Select(
                        t =>
                            new
                            {
                                id =
                                    t.Id,

                                amount =
                                    t.Amount,

                                type =
                                    t.Type,

                                description =
                                    t.Description,

                                reference =
                                    t.Reference,

                                createdAt =
                                    t.CreatedAt
                            })
                    .ToListAsync();


            // =================================================
            // RECENT GROCERY LISTS
            // =================================================

            var groceryLists =
                await _context
                    .GroceryLists
                    .AsNoTracking()
                    .Where(
                        g =>
                            g.UserId ==
                                userId.Value)
                    .Include(
                        g =>
                            g.Items)
                    .OrderByDescending(
                        g =>
                            g.UpdatedAt)
                    .Take(5)
                    .ToListAsync();


            var recentGroceryLists =
                groceryLists
                    .Select(
                        g =>
                            new
                            {
                                id =
                                    g.Id,

                                listName =
                                    g.ListName,

                                retailer =
                                    g.Retailer,

                                status =
                                    g.Status,

                                itemCount =
                                    g.Items.Count,

                                total =
                                    g.Items.Sum(
                                        i =>
                                            i.UnitPrice *
                                            i.Quantity),

                                updatedAt =
                                    g.UpdatedAt
                            })
                    .ToList();


            // =================================================
            // GROCERY BUDGET SUMMARY
            // =================================================

            object? grocerySummary =
                null;


            if (monthlyBudget != null)
            {
                var groceryBudget =
                    monthlyBudget
                        .CategoryBudgets
                        .FirstOrDefault(
                            c =>
                                c.Category.Equals(
                                    "Groceries",
                                    StringComparison
                                        .OrdinalIgnoreCase));


                if (groceryBudget != null)
                {
                    grocerySummary =
                        new
                        {
                            limit =
                                groceryBudget
                                    .LimitAmount,

                            spent =
                                groceryBudget
                                    .SpentAmount,

                            remaining =
                                groceryBudget
                                    .LimitAmount -
                                groceryBudget
                                    .SpentAmount,

                            percentageUsed =
                                groceryBudget
                                        .LimitAmount > 0
                                    ? Math.Round(
                                        (
                                            groceryBudget
                                                .SpentAmount /
                                            groceryBudget
                                                .LimitAmount
                                        ) * 100,
                                        2)
                                    : 0m
                        };
                }
            }


            // =================================================
            // RETURN DASHBOARD
            // =================================================

            return Ok(
                new
                {
                    month,

                    year,

                    monthlyBudget =
                        new
                        {
                            limit =
                                totalBudget,

                            spent =
                                totalSpent,

                            remaining =
                                totalRemaining,

                            percentageUsed
                        },

                    groceries =
                        grocerySummary,

                    categories,

                    recentTransactions,

                    recentGroceryLists
                });
        }


        // =====================================================
        // GET DASHBOARD CHART DATA
        // Example: GET /api/Dashboard/charts/2026/9
        // =====================================================

        [HttpGet("charts/{year:int}/{month:int}")]
        public async Task<IActionResult> GetChartData(
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
            // GET MONTHLY BUDGET
            // =================================================

            var monthlyBudget =
                await _context
                    .MonthlyBudgets
                    .Include(
                        b =>
                            b.CategoryBudgets)
                    .FirstOrDefaultAsync(
                        b =>
                            b.UserId ==
                                userId.Value &&
                            b.Year ==
                                year &&
                            b.Month ==
                                month);


            // =================================================
            // CLEAN ZERO STATE
            // =================================================

            if (monthlyBudget == null)
            {
                return Ok(
                    new
                    {
                        month,

                        year,

                        summary =
                            new
                            {
                                totalBudget =
                                    0m,

                                totalSpent =
                                    0m,

                                remaining =
                                    0m
                            },

                        barChart =
                            Array.Empty<object>(),

                        pieChart =
                            Array.Empty<object>(),

                        lineChart =
                            Array.Empty<object>()
                    });
            }


            // =================================================
            // BAR CHART
            // =================================================

            var barChart =
                monthlyBudget
                    .CategoryBudgets
                    .OrderBy(
                        c =>
                            c.Category)
                    .Select(
                        c =>
                            new
                            {
                                category =
                                    c.Category,

                                budget =
                                    c.LimitAmount,

                                spent =
                                    c.SpentAmount
                            })
                    .ToList();


            // =================================================
            // PIE CHART
            // =================================================

            var pieChart =
                monthlyBudget
                    .CategoryBudgets
                    .Where(
                        c =>
                            c.SpentAmount > 0)
                    .OrderByDescending(
                        c =>
                            c.SpentAmount)
                    .Select(
                        c =>
                            new
                            {
                                category =
                                    c.Category,

                                amount =
                                    c.SpentAmount
                            })
                    .ToList();


            decimal totalSpent =
                monthlyBudget
                    .CategoryBudgets
                    .Sum(
                        c =>
                            c.SpentAmount);


            var pieChartWithPercentage =
                pieChart
                    .Select(
                        c =>
                            new
                            {
                                category =
                                    c.category,

                                amount =
                                    c.amount,

                                percentage =
                                    totalSpent > 0
                                        ? Math.Round(
                                            (
                                                c.amount /
                                                totalSpent
                                            ) * 100,
                                            2)
                                        : 0m
                            })
                    .ToList();


            // =================================================
            // MONTH DATE RANGE
            // =================================================

            DateTime startDate =
                new DateTime(
                    year,
                    month,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc);


            DateTime endDate =
                startDate.AddMonths(1);


            // =================================================
            // LINE CHART
            //
            // Transactions belong directly to UserId.
            // =================================================

            var dailyPurchases =
                await _context
                    .Transactions
                    .AsNoTracking()
                    .Where(
                        t =>
                            t.UserId ==
                                userId.Value &&
                            (
                                t.Type == "Purchase" ||
                                t.Amount < 0
                            ) &&
                            t.CreatedAt >=
                                startDate &&
                            t.CreatedAt <
                                endDate)
                    .GroupBy(
                        t =>
                            t.CreatedAt.Date)
                    .Select(
                        g =>
                            new DailySpendingResult
                            {
                                Date =
                                    g.Key,

                                Amount =
                                    g.Sum(
                                        t =>
                                            Math.Abs(
                                                t.Amount))
                            })
                    .OrderBy(
                        x =>
                            x.Date)
                    .ToListAsync();


            var lineChart =
                dailyPurchases
                    .Select(
                        d =>
                            new
                            {
                                date =
                                    d.Date.ToString(
                                        "yyyy-MM-dd"),

                                amount =
                                    d.Amount
                            })
                    .ToList();


            // =================================================
            // TOTAL BUDGET INFORMATION
            // =================================================

            decimal totalBudget =
                monthlyBudget.TotalLimit;


            decimal remaining =
                totalBudget -
                totalSpent;


            // =================================================
            // RETURN CHART DATA
            // =================================================

            return Ok(
                new
                {
                    month,

                    year,

                    summary =
                        new
                        {
                            totalBudget,

                            totalSpent,

                            remaining
                        },

                    barChart,

                    pieChart =
                        pieChartWithPercentage,

                    lineChart
                });
        }


        // =====================================================
        // HELPER CLASS FOR LINE CHART QUERY
        // =====================================================

        private class DailySpendingResult
        {
            public DateTime Date { get; set; }

            public decimal Amount { get; set; }
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