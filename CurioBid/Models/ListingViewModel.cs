using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CurioBid.Models
{
    // The form used to create or edit a listing
    public class ListingFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150)]
        [Display(Name = "Item Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a category.")]
        public string Category { get; set; } = "Ancient Coins";

        [Required(ErrorMessage = "Starting price is required.")]
        [Range(typeof(decimal), "10", "1000000", ErrorMessage = "Price must be between $10 and $1,000,000.")]
        [Display(Name = "Starting / Reserve Price (USD)")]
        public decimal? StartingPrice { get; set; }

        [Required(ErrorMessage = "Choose the evidence type.")]
        [Display(Name = "Evidence Video Type")]
        public string EvidenceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "A video proof link is required.")]
        [Url(ErrorMessage = "Enter a full link starting with http:// or https://")]
        [Display(Name = "Video Proof Link")]
        public string VideoUrl { get; set; } = string.Empty;

        [Url(ErrorMessage = "Enter a full link starting with http:// or https://")]
        [Display(Name = "Photo Link (optional)")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "Description must be 20 to 2000 characters.")]
        [Display(Name = "Description & Authenticity Details")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a reviewer.")]
        [Display(Name = "Send to Reviewer")]
        public string ReviewerId { get; set; } = string.Empty;

        // Only used to draw the dropdown, never posted back
        [ValidateNever]
        public List<SelectListItem> Reviewers { get; set; } = new();
    }

    // What the reviewer posts when deciding
    public class ReviewDecisionViewModel
    {
        public int ListingId { get; set; }

        [Required]
        public string Decision { get; set; } = string.Empty;   // approve | reject | needsproof

        [StringLength(500)]
        public string? Note { get; set; }
    }

    // The reviewer's dashboard
    public class ReviewerQueueViewModel
    {
        public List<Listing> Pending { get; set; } = new();
        public List<Listing> Reviewed { get; set; } = new();
    }
}