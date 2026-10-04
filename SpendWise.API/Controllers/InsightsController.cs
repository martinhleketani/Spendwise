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
    public class InsightsController : ControllerBase
    {
        private readonly AppDbContext _context;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public InsightsController(
            AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // GET MONTHLY SPENDING INSIGHTS
        // GET: /api/Insights/{year}/{month}
        // =====================================================

        [HttpGet("{year:int}/{month:int}")]
        public async Task<IActionResult> GetInsights(
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

            var budget =
                await _context.MonthlyBudgets
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


            if (budget == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Budget not found."
                    });
            }


            // =================================================
            // MONTHLY TOTALS
            // =================================================
            //
            // These values represent budgeting and recorded
            // spending only.
            //
            // No SpendWise wallet balance is used.
            // =================================================

            decimal totalBudget =
                budget.TotalLimit;


            decimal totalSpent =
                budget.CategoryBudgets
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
                    : 0;


            // =================================================
            // FIND HIGHEST SPENDING CATEGORY
            // =================================================

            var highestSpendingCategory =
                budget.CategoryBudgets
                    .OrderByDescending(
                        c =>
                            c.SpentAmount)
                    .FirstOrDefault();


            // =================================================
            // FIND CATEGORY CLOSEST TO LIMIT
            // =================================================

            var categoryClosestToLimit =
                budget.CategoryBudgets
                    .Where(
                        c =>
                            c.LimitAmount > 0)
                    .OrderByDescending(
                        c =>
                            c.SpentAmount /
                            c.LimitAmount)
                    .FirstOrDefault();


            // =================================================
            // BUILD GENERAL INSIGHTS
            // =================================================

            var insights =
                new List<object>();


            // =================================================
            // MONTHLY BUDGET INSIGHT
            // =================================================

            insights.Add(
                new
                {
                    type =
                        "Monthly Budget",

                    level =
                        GetStatus(
                            percentageUsed),

                    message =
                        $"You have spent R{totalSpent:0.00} " +
                        $"of your R{totalBudget:0.00} monthly budget. " +
                        $"You have R{totalRemaining:0.00} remaining."
                });


            // =================================================
            // HIGHEST SPENDING CATEGORY
            // =================================================

            if (highestSpendingCategory != null &&
                highestSpendingCategory.SpentAmount > 0)
            {
                decimal categoryPercentage =
                    highestSpendingCategory
                        .LimitAmount > 0
                        ? Math.Round(
                            (
                                highestSpendingCategory
                                    .SpentAmount /
                                highestSpendingCategory
                                    .LimitAmount
                            ) * 100,
                            2)
                        : 0;


                insights.Add(
                    new
                    {
                        type =
                            "Highest Spending Category",

                        level =
                            GetStatus(
                                categoryPercentage),

                        category =
                            highestSpendingCategory
                                .Category,

                        message =
                            $"{highestSpendingCategory.Category} is your " +
                            $"highest spending category at " +
                            $"R{highestSpendingCategory.SpentAmount:0.00}. " +
                            $"You have used {categoryPercentage}% " +
                            $"of its budget."
                    });
            }


            // =================================================
            // CATEGORY CLOSEST TO BUDGET LIMIT
            // =================================================

            if (categoryClosestToLimit != null &&
                categoryClosestToLimit.SpentAmount > 0)
            {
                decimal closestPercentage =
                    Math.Round(
                        (
                            categoryClosestToLimit
                                .SpentAmount /
                            categoryClosestToLimit
                                .LimitAmount
                        ) * 100,
                        2);


                decimal remaining =
                    categoryClosestToLimit
                        .LimitAmount -
                    categoryClosestToLimit
                        .SpentAmount;


                insights.Add(
                    new
                    {
                        type =
                            "Budget Risk",

                        level =
                            GetStatus(
                                closestPercentage),

                        category =
                            categoryClosestToLimit
                                .Category,

                        message =
                            CreateRiskMessage(
                                categoryClosestToLimit
                                    .Category,
                                closestPercentage,
                                remaining)
                    });
            }


            // =================================================
            // CATEGORY INSIGHTS
            // =================================================

            var categoryInsights =
                budget.CategoryBudgets
                    .Select(
                        c =>
                        {
                            decimal categoryPercentage =
                                c.LimitAmount > 0
                                    ? Math.Round(
                                        (
                                            c.SpentAmount /
                                            c.LimitAmount
                                        ) * 100,
                                        2)
                                    : 0;


                            decimal remaining =
                                c.LimitAmount -
                                c.SpentAmount;


                            return new
                            {
                                category =
                                    c.Category,

                                budget =
                                    c.LimitAmount,

                                spent =
                                    c.SpentAmount,

                                remaining,

                                percentageUsed =
                                    categoryPercentage,

                                status =
                                    GetStatus(
                                        categoryPercentage),

                                message =
                                    CreateCategoryMessage(
                                        c.Category,
                                        c.SpentAmount,
                                        c.LimitAmount,
                                        categoryPercentage,
                                        remaining)
                            };
                        })
                    .OrderByDescending(
                        c =>
                            c.percentageUsed)
                    .ToList();


            // =================================================
            // RETURN RESULT
            // =================================================
            //
            // WalletBalance has been removed.
            //
            // The client calculates/display budgeting values
            // from TotalBudget and TotalSpent.
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

                            totalRemaining,

                            percentageUsed
                        },

                    insights,

                    categories =
                        categoryInsights
                });
        }


        // =====================================================
        // GET STATUS
        // =====================================================

        private string GetStatus(
            decimal percentageUsed)
        {
            if (percentageUsed > 100)
            {
                return "Over Budget";
            }


            if (percentageUsed == 100)
            {
                return "Budget Reached";
            }


            if (percentageUsed >= 90)
            {
                return "High Warning";
            }


            if (percentageUsed >= 75)
            {
                return "Warning";
            }


            return "Safe";
        }


        // =====================================================
        // CREATE CATEGORY MESSAGE
        // =====================================================

        private string CreateCategoryMessage(
            string category,
            decimal spent,
            decimal limit,
            decimal percentageUsed,
            decimal remaining)
        {
            if (percentageUsed > 100)
            {
                return
                    $"You are R{Math.Abs(remaining):0.00} over " +
                    $"your {category} budget.";
            }


            if (percentageUsed == 100)
            {
                return
                    $"You have reached your {category} " +
                    $"budget limit of R{limit:0.00}.";
            }


            if (percentageUsed >= 90)
            {
                return
                    $"High warning: you have used " +
                    $"{percentageUsed}% of your {category} budget. " +
                    $"Only R{remaining:0.00} remains.";
            }


            if (percentageUsed >= 75)
            {
                return
                    $"Warning: you have used " +
                    $"{percentageUsed}% of your {category} budget. " +
                    $"R{remaining:0.00} remains.";
            }


            if (spent > 0)
            {
                return
                    $"Your {category} spending is within budget. " +
                    $"You have R{remaining:0.00} remaining.";
            }


            return
                $"You have not spent anything in " +
                $"{category} yet.";
        }


        // =====================================================
        // CREATE RISK MESSAGE
        // =====================================================

        private string CreateRiskMessage(
            string category,
            decimal percentageUsed,
            decimal remaining)
        {
            if (percentageUsed > 100)
            {
                return
                    $"{category} is over budget by " +
                    $"R{Math.Abs(remaining):0.00}.";
            }


            if (percentageUsed == 100)
            {
                return
                    $"{category} has reached its budget limit.";
            }


            if (percentageUsed >= 90)
            {
                return
                    $"{category} is very close to its budget limit. " +
                    $"Only R{remaining:0.00} remains.";
            }


            if (percentageUsed >= 75)
            {
                return
                    $"{category} is approaching its budget limit. " +
                    $"R{remaining:0.00} remains.";
            }


            return
                $"{category} currently has the highest budget usage " +
                $"at {percentageUsed}%.";
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