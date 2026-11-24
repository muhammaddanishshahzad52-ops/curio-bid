using CurioBid.Data;
using CurioBid.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CurioBid.Controllers
{
    [Authorize(Roles = "User")]
    public class SellerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public SellerController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ---------- LIST MY LISTINGS ----------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var listings = await _db.Listings
                .Include(l => l.Reviewer)
                .Where(l => l.SellerId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(listings);
        }

        // ---------- CREATE ----------

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ListingFormViewModel
            {
                Reviewers = await GetReviewerItemsAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListingFormViewModel model)
        {
            await ValidateFormAsync(model);

            if (!ModelState.IsValid)
            {
                model.Reviewers = await GetReviewerItemsAsync();
                return View(model);
            }

            var listing = new Listing
            {
                Title = model.Title.Trim(),
                Category = model.Category,
                Description = model.Description.Trim(),
                StartingPrice = model.StartingPrice!.Value,
                EvidenceType = model.EvidenceType,
                VideoUrl = model.VideoUrl,
                ImageUrl = model.ImageUrl,
                SellerId = _userManager.GetUserId(User)!,   // from the login cookie, never from the form
                ReviewerId = model.ReviewerId
            };

            _db.Listings.Add(listing);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"\"{listing.Title}\" was sent to the reviewer.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- DETAILS ----------

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var listing = await FindMyListingAsync(id);
            if (listing == null) return NotFound();

            return View(listing);
        }

        // ---------- EDIT (only when the reviewer asked for more proof) ----------

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var listing = await FindMyListingAsync(id);
            if (listing == null) return NotFound();

            if (listing.Status != ListingStatus.NeedsProof)
            {
                TempData["Error"] = "Only listings where the reviewer asked for more proof can be edited.";
                return RedirectToAction(nameof(Index));
            }

            return View(new ListingFormViewModel
            {
                Id = listing.Id,
                Title = listing.Title,
                Category = listing.Category,
                Description = listing.Description,
                StartingPrice = listing.StartingPrice,
                EvidenceType = listing.EvidenceType,
                VideoUrl = listing.VideoUrl,
                ImageUrl = listing.ImageUrl,
                ReviewerId = listing.ReviewerId,
                Reviewers = await GetReviewerItemsAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ListingFormViewModel model)
        {
            var listing = await FindMyListingAsync(model.Id);
            if (listing == null) return NotFound();

            if (listing.Status != ListingStatus.NeedsProof)
            {
                TempData["Error"] = "Only listings where the reviewer asked for more proof can be edited.";
                return RedirectToAction(nameof(Index));
            }

            await ValidateFormAsync(model);

            if (!ModelState.IsValid)
            {
                model.Reviewers = await GetReviewerItemsAsync();
                return View(model);
            }

            listing.Title = model.Title.Trim();
            listing.Category = model.Category;
            listing.Description = model.Description.Trim();
            listing.StartingPrice = model.StartingPrice!.Value;
            listing.EvidenceType = model.EvidenceType;
            listing.VideoUrl = model.VideoUrl;
            listing.ImageUrl = model.ImageUrl;
            listing.ReviewerId = model.ReviewerId;

            // Back into the reviewer's queue
            listing.Status = ListingStatus.Pending;
            listing.ReviewedAt = null;

            await _db.SaveChangesAsync();

            TempData["Success"] = $"\"{listing.Title}\" was resubmitted for review.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- helpers ----------

        // Only returns the listing if it belongs to the logged-in user.
        // Someone else's id gives NotFound, so the page doesn't reveal that it exists.
        private async Task<Listing?> FindMyListingAsync(int id)
        {
            var userId = _userManager.GetUserId(User);

            return await _db.Listings
                .Include(l => l.Reviewer)
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == userId);
        }

        private async Task<List<SelectListItem>> GetReviewerItemsAsync()
        {
            var reviewers = await _userManager.GetUsersInRoleAsync("Reviewer");
            var items = new List<SelectListItem>();

            foreach (var r in reviewers.OrderBy(r => r.FullName))
            {
                if (await _userManager.IsLockedOutAsync(r)) continue;   // skip suspended reviewers
                items.Add(new SelectListItem(r.FullName, r.Id));
            }

            return items;
        }

        // Checks the browser can't be trusted to get right
        private async Task ValidateFormAsync(ListingFormViewModel model)
        {
            if (!ListingOptions.Categories.Contains(model.Category))
            {
                ModelState.AddModelError(nameof(model.Category), "Choose a valid category.");
            }

            if (!ListingOptions.EvidenceTypes.Contains(model.EvidenceType))
            {
                ModelState.AddModelError(nameof(model.EvidenceType), "Choose a valid evidence type.");
            }

            if (!string.IsNullOrEmpty(model.ReviewerId))
            {
                var reviewer = await _userManager.FindByIdAsync(model.ReviewerId);

                if (reviewer == null ||
                    !await _userManager.IsInRoleAsync(reviewer, "Reviewer") ||
                    await _userManager.IsLockedOutAsync(reviewer))
                {
                    ModelState.AddModelError(nameof(model.ReviewerId), "Choose a valid reviewer.");
                }
            }
        }
    }
}