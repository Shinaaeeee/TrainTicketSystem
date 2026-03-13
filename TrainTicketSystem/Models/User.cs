using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TrainTicketSystem.Models;

public partial class User
{
    public int UserId { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$",
        ErrorMessage = "Password must be at least 6 characters and contain uppercase, lowercase and number")]
    public string Password { get; set; }

    [Required]
    public string FullName { get; set; }

    [Required]
    [RegularExpression(@"^[0]{1}3[0-9]{8}$",
        ErrorMessage = "Phone must start with 03 and have 10 digits")]
    public string Phone { get; set; }

    [Required]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$",
        ErrorMessage = "Email must be a gmail address")]
    public string Email { get; set; }

    public string Role { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
