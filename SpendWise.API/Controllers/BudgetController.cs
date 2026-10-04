using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using SpendWise.API.DTOs;
using SpendWise.API.Models;
using System.Security.Claims;

namespace SpendWise.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BudgetController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BudgetController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // CREATE MONTHLY BUDGET
        // POST: /api/Budget
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> CreateBudget(
            CreateBudgetDto request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            if (request.Month < 1 || request.Month > 12)
            {
                return BadRequest(new
                {
                    message = "Month must be between 1 and 12."
                });
            }

            if (request.Year < 2020 || request.Year > 2100)
            {
                return BadRequest(new
                {
                    message = "Invalid year."
                });
            }

            if (request.TotalLimit <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Total monthly limit must be greater than R0."
                });
            }

            bool alreadyExists =
                await _context.MonthlyBudgets.AnyAsync(b =>
                    b.UserId == userId.Value &&
                    b.Month == request.Month &&
                    b.Year == request.Year);

            if (alreadyExists)
            {
                return Conflict(new
                {
                    message =
                        "A budget already exists for this month."
                });
            }

            if (request.Categories.Any(c =>
                    string.IsNullOrWhiteSpace(c.Category) ||
                    c.LimitAmount < 0))
            {
                return BadRequest(new
                {
                    message =
                        "Every category must have a valid name and limit."
                });
            }

            bool duplicateCategories =
                request.Categories
                    .GroupBy(c =>
                        c.Category.Trim().ToLower())
                    .Any(g =>
                        g.Count() > 1);

            if (duplicateCategories)
            {
                return BadRequest(new
                {
                    message =
                        "Duplicate budget categories are not allowed."
                });
            }

            decimal categoryTotal =
                request.Categories
                    .Sum(c => c.LimitAmount);

            if (categoryTotal > request.TotalLimit)
            {
                return BadRequest(new
                {
                    message =
                        "Category limits cannot exceed the total monthly budget."
                });
            }

            var budget = new MonthlyBudget
            {
                UserId = userId.Value,
                Month = request.Month,
                Year = request.Year,
                TotalLimit = request.TotalLimit,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var category in request.Categories)
            {
                budget.CategoryBudgets.Add(
                    new CategoryBudget
                    {
                        Category =
                            category.Category.Trim(),

                        LimitAmount =
                            category.LimitAmount,

                        SpentAmount =
                            0.00m
                    });
            }

            _context.MonthlyBudgets.Add(budget);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Monthly budget created successfully.",

                budgetId =
                    budget.Id,

                month =
                    budget.Month,

                year =
                    budget.Year,

                totalLimit =
                    budget.TotalLimit,

                categoryTotal,

                unallocated =
                    budget.TotalLimit -
                    categoryTotal
            });
        }

        // =====================================================
        // GET MONTHLY BUDGET
        // GET: /api/Budget/{year}/{month}
        // =====================================================
        [HttpGet("{year:int}/{month:int}")]
        public async Task<IActionResult> GetBudget(
            int year,
            int month)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var budget =
                await _context.MonthlyBudgets
                    .Include(b =>
                        b.CategoryBudgets)
                    .FirstOrDefaultAsync(b =>
                        b.UserId == userId.Value &&
                        b.Year == year &&
                        b.Month == month);

            if (budget == null)
            {
                return NotFound(new
                {
                    message =
                        "Budget not found."
                });
            }

            decimal totalSpent =
                budget.CategoryBudgets
                    .Sum(c =>
                        c.SpentAmount);

            return Ok(new
            {
                budget.Id,
                budget.Month,
                budget.Year,

                totalLimit =
                    budget.TotalLimit,

                totalSpent,

                remaining =
                    budget.TotalLimit -
                    totalSpent,

                categories =
                    budget.CategoryBudgets
                        .Select(c => new
                        {
                            c.Id,
                            c.Category,

                            limit =
                                c.LimitAmount,

                            spent =
                                c.SpentAmount,

                            remaining =
                                c.LimitAmount -
                                c.SpentAmount
                        })
            });
        }

        // =====================================================
        // GET SPENDING ALERTS
        // GET: /api/Budget/alerts/{year}/{month}
        // =====================================================
        [HttpGet("alerts/{year:int}/{month:int}")]
        public async Task<IActionResult> GetBudgetAlerts(
            int year,
            int month)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            // -----------------------------------------
            // VALIDATE DATE
            // -----------------------------------------
            if (month < 1 || month > 12)
            {
                return BadRequest(new
                {
                    message =
                        "Month must be between 1 and 12."
                });
            }

            if (year < 2020 || year > 2100)
            {
                return BadRequest(new
                {
                    message =
                        "Invalid year."
                });
            }

            // -----------------------------------------
            // GET MONTHLY BUDGET
            // -----------------------------------------
            var budget =
                await _context.MonthlyBudgets
                    .Include(b =>
                        b.CategoryBudgets)
                    .FirstOrDefaultAsync(b =>
                        b.UserId == userId.Value &&
                        b.Year == year &&
                        b.Month == month);

            if (budget == null)
            {
                return NotFound(new
                {
                    message =
                        "Budget not found."
                });
            }

            // -----------------------------------------
            // CREATE CATEGORY ALERTS
            // -----------------------------------------
            var alerts =
                budget.CategoryBudgets
                    .OrderByDescending(c =>
                        c.LimitAmount > 0
                            ? c.SpentAmount /
                              c.LimitAmount
                            : 0)
                    .Select(c =>
                    {
                        decimal percentageUsed =
                            c.LimitAmount > 0
                                ? Math.Round(
                                    (c.SpentAmount /
                                     c.LimitAmount) * 100,
                                    2)
                                : 0;

                        string status;
                        string message;

                        if (percentageUsed > 100)
                        {
                            status =
                                "Over Budget";

                            message =
                                $"You are R{Math.Abs(c.LimitAmount - c.SpentAmount):0.00} over your {c.Category} budget.";
                        }
                        else if (percentageUsed == 100)
                        {
                            status =
                                "Budget Reached";

                            message =
                                $"You have reached your {c.Category} budget limit.";
                        }
                        else if (percentageUsed >= 90)
                        {
                            status =
                                "High Warning";

                            message =
                                $"You have used {percentageUsed}% of your {c.Category} budget.";
                        }
                        else if (percentageUsed >= 75)
                        {
                            status =
                                "Warning";

                            message =
                                $"You have used {percentageUsed}% of your {c.Category} budget.";
                        }
                        else
                        {
                            status =
                                "Safe";

                            message =
                                $"Your {c.Category} spending is within budget.";
                        }

                        return new
                        {
                            category =
                                c.Category,

                            limit =
                                c.LimitAmount,

                            spent =
                                c.SpentAmount,

                            remaining =
                                c.LimitAmount -
                                c.SpentAmount,

                            percentageUsed,

                            status,

                            message
                        };
                    })
                    .ToList();

            // -----------------------------------------
            // MONTHLY BUDGET TOTALS
            // -----------------------------------------
            decimal totalSpent =
                budget.CategoryBudgets
                    .Sum(c =>
                        c.SpentAmount);

            decimal totalRemaining =
                budget.TotalLimit -
                totalSpent;

            decimal totalPercentageUsed =
                budget.TotalLimit > 0
                    ? Math.Round(
                        (totalSpent /
                         budget.TotalLimit) * 100,
                        2)
                    : 0;

            // -----------------------------------------
            // OVERALL MONTHLY STATUS
            // -----------------------------------------
            string overallStatus;

            if (totalPercentageUsed > 100)
            {
                overallStatus =
                    "Over Budget";
            }
            else if (totalPercentageUsed == 100)
            {
                overallStatus =
                    "Budget Reached";
            }
            else if (totalPercentageUsed >= 90)
            {
                overallStatus =
                    "High Warning";
            }
            else if (totalPercentageUsed >= 75)
            {
                overallStatus =
                    "Warning";
            }
            else
            {
                overallStatus =
                    "Safe";
            }

            // -----------------------------------------
            // RETURN ALERT INFORMATION
            // -----------------------------------------
            return Ok(new
            {
                month,
                year,

                summary = new
                {
                    totalBudget =
                        budget.TotalLimit,

                    totalSpent,

                    remaining =
                        totalRemaining,

                    percentageUsed =
                        totalPercentageUsed,

                    status =
                        overallStatus
                },

                alerts
            });
        }

        // =====================================================
        // GET USER ID FROM JWT
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