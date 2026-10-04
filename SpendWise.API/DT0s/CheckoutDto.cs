using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.DTOs
{
    public class CheckoutDto
    {
        [Range(
            1,
            12,
            ErrorMessage = "Month must be between 1 and 12.")]
        public int Month { get; set; }

        [Range(
            2020,
            2100,
            ErrorMessage = "Year must be between 2020 and 2100.")]
        public int Year { get; set; }
    }
}