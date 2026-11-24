using CurioBid.Data;
using CurioBid.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CurioBid.Controllers
{

    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private static readonly string[] AllowedRoles = { "User", "Reviewer", "Admin" };
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Email!.Contains(search) || u.FullName.Contains(search));
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

            var list = new List<UserListItemViewModel>();
            foreach (var u in users)
            {
                list.Add(await ToListItemAsync(u));
            }

            ViewData["Search"] = search;

            // IF REQUEST IS AJAX (FETCH), RETURN ONLY THE TABLE HTML
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_UserListPartial", list);
            }

            // OTHERWISE, RETURN THE FULL PAGE
            return View(list);
        }

        // ---------- READ: one user ----------

        [HttpGet]
        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(await ToListItemAsync(user));
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateUserViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!AllowedRoles.Contains(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Invalid role.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            TempData["Success"] = $"User {user.Email} was created as {model.Role}.";
            return RedirectToAction(nameof(Index));
        }

        //[HttpGet]
        //public async Task<IActionResult> Edit(string? id)
        //{
        //    if (string.IsNullOrEmpty(id)) return NotFound();

        //    var user = await _userManager.FindByIdAsync(id);
        //    if (user == null) return NotFound();

        //    var roles = await _userManager.GetRolesAsync(user);

        //    return View(new EditUserViewModel
        //    {
        //        Id = user.Id,
        //        FullName = user.FullName,
        //        Email = user.Email ?? string.Empty,
        //        PhoneNumber = user.PhoneNumber ?? string.Empty,
        //        Role = roles.FirstOrDefault() ?? "User",
        //        IsSuspended = await _userManager.IsLockedOutAsync(user)
        //    });
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!AllowedRoles.Contains(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Invalid role.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);

            // 1. Prevent admin self-lockout / self-demotion
            if (user.Id == _userManager.GetUserId(User) &&
                (model.IsSuspended || model.Role != "Admin"))
            {
                ModelState.AddModelError(string.Empty,
                    "You cannot suspend your own account or remove your own Admin role.");
                return View(model);
            }

            // 2. Prevent demoting/suspending a reviewer with pending listings
            if (currentRoles.Contains("Reviewer") && (model.Role != "Reviewer" || model.IsSuspended) &&
                await _db.Listings.AnyAsync(l => l.ReviewerId == user.Id && l.Status == ListingStatus.Pending))
            {
                ModelState.AddModelError(string.Empty,
                    "This reviewer still has listings waiting for review. Wait until their queue is empty first.");
                return View(model);
            }

            // 3. Update profile fields
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // 4. Enforce single role
            if (currentRoles.Count != 1 || currentRoles[0] != model.Role)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            // 5. Suspend / Unsuspend reliably
            if (model.IsSuspended)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            // 6. Force active sessions to refresh role/lockout state
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = $"User {user.Email} was updated.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- DELETE ----------

        [HttpGet]
        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(await ToListItemAsync(user));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            // =========================================================================
            // NEW CODE: Listings are records that reference this user, so keep the account and suspend it instead
            // =========================================================================
            if (await _db.Listings.AnyAsync(l => l.SellerId == id || l.ReviewerId == id))
            {
                TempData["Error"] = "This user has listings on record and cannot be deleted. Suspend the account instead.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
                TempData["Success"] = $"User {user.Email} was deleted.";
            else
                TempData["Error"] = "The user could not be deleted.";

            return RedirectToAction(nameof(Index));
        }

        // ---------- helper ----------

        private async Task<UserListItemViewModel> ToListItemAsync(ApplicationUser u)
        {
            var roles = await _userManager.GetRolesAsync(u);

            return new UserListItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "None",
                IsSuspended = await _userManager.IsLockedOutAsync(u),
                CreatedAt = u.CreatedAt
            };
        }
    }
}
