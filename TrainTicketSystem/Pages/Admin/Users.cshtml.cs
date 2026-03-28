using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Pages.Admin;

public class UsersModel : PageModel
{
    private readonly TrainTicketDbContext _context;

    public UsersModel(TrainTicketDbContext context)
    {
        _context = context;
    }

    public List<User> Users { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchName { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? RoleFilter { get; set; }

    // Edit modal bindings
    [BindProperty]
    public int EditUserId { get; set; }

    [BindProperty]
    public string EditFullName { get; set; } = "";

    [BindProperty]
    public string EditEmail { get; set; } = "";

    [BindProperty]
    public string EditPhone { get; set; } = "";

    [BindProperty]
    public string EditRole { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("Role");
        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return RedirectToPage("/Login");

        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchName))
        {
            query = query.Where(u =>
                (u.FullName != null && u.FullName.Contains(SearchName)) ||
                u.Username.Contains(SearchName) ||
                (u.Email != null && u.Email.Contains(SearchName)));
        }

        if (!string.IsNullOrWhiteSpace(RoleFilter))
        {
            query = query.Where(u => u.Role == RoleFilter);
        }

        Users = await query.OrderBy(u => u.UserId).ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        var role = HttpContext.Session.GetString("Role");
        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var user = await _context.Users.FindAsync(EditUserId);
        if (user == null) return NotFound();

        user.FullName = EditFullName;
        user.Email = EditEmail;
        user.Phone = EditPhone;
        user.Role = EditRole;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"User \"{user.Username}\" updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int userId)
    {
        var role = HttpContext.Session.GetString("Role");
        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var user = await _context.Users
            .Include(u => u.Bookings)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return NotFound();

        // Prevent deleting users with existing bookings
        if (user.Bookings.Any())
        {
            TempData["ErrorMessage"] = $"Cannot delete \"{user.Username}\" because they have existing bookings.";
            return RedirectToPage();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"User \"{user.Username}\" has been deleted.";
        return RedirectToPage();
    }
}
