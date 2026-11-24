using CurioBid.Data;
using CurioBid.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CurioBid.Controllers
{
    [Authorize(Roles = "Reviewer")]
    public class ReviewerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewerController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ---------- MY QUEUE ----------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var me = _userManager.GetUserId(User);

            // Only listings that were sent to THIS reviewer
            var mine = _db.Listings.Include(l => l.Seller).Where(l => l.ReviewerId == me);

            var model = new ReviewerQueueViewModel
            {
                Pending = await mine
                    .Where(l => l.Status == ListingStatus.Pending)
                    .OrderBy(l => l.CreatedAt)
                    .ToListAsync(),

                Reviewed = await mine
                    .Where(l => l.Status != ListingStatus.Pending)
                    .OrderByDescending(l => l.ReviewedAt)
                    .Take(10)
                    .ToListAsync()
            };

            return View(model);
        }

        // ---------- REVIEW ONE LISTING ----------

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var (listing, isForbidden) = await FindListingForCurrentReviewerAsync(id);
            if (isForbidden) return Forbid();
            if (listing == null) return NotFound();

            if (listing.Status != ListingStatus.Pending)
            {
                TempData["Error"] = "This listing has already been decided.";
                return RedirectToAction(nameof(Index));
            }

            return View(listing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(ReviewDecisionViewModel model)
        {
            var (listing, isForbidden) = await FindListingForCurrentReviewerAsync(model.ListingId);
            if (isForbidden) return Forbid();
            if (listing == null) return NotFound();

            if (listing.Status != ListingStatus.Pending)
            {
                TempData["Error"] = "This listing has already been decided.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid decision, or the note is longer than 500 characters.";
                return RedirectToAction(nameof(Review), new { id = listing.Id });
            }

            var note = model.Note?.Trim();

            switch (model.Decision)
            {
                case "approve":
                    listing.Status = ListingStatus.Approved;
                    listing.AuctionEndsAt = DateTime.UtcNow.AddHours(24);
                    break;

                case "reject":
                case "needsproof":
                    if (string.IsNullOrWhiteSpace(note))
                    {
                        TempData["Error"] = "Please write a note so the seller knows why.";
                        return RedirectToAction(nameof(Review), new { id = listing.Id });
                    }
                    listing.Status = model.Decision == "reject"
                        ? ListingStatus.Rejected
                        : ListingStatus.NeedsProof;
                    break;

                default:
                    return BadRequest();
            }

            listing.ReviewerNote = note;
            listing.ReviewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = listing.Status switch
            {
                ListingStatus.Approved => $"\"{listing.Title}\" was approved.",
                ListingStatus.Rejected => $"\"{listing.Title}\" was rejected.",
                _ => $"More proof was requested for \"{listing.Title}\"."
            };

            return RedirectToAction(nameof(Index));
        }

        // ---------- helper ----------

        private async Task<(Listing? Listing, bool IsForbidden)> FindListingForCurrentReviewerAsync(int id)
        {
            var listing = await _db.Listings
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
            {
                return (null, false);
            }

            var me = _userManager.GetUserId(User);
            if (listing.ReviewerId != me)
            {
                return (null, true);
            }

            return (listing, false);
        }
    }
}