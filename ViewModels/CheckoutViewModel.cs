using System.ComponentModel.DataAnnotations;

namespace NexusGear.ViewModels
{
    public class CheckoutViewModel
    {
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, Phone] public string Phone { get; set; } = string.Empty;
        [Required] public string Address { get; set; } = string.Empty;
        [Required] public string City { get; set; } = string.Empty;
        [Required] public string Country { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        [Required] public string PaymentMethod { get; set; } = "card";
        public string? Notes { get; set; }
        public string? StripePaymentIntentId { get; set; }

        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
    }
}
