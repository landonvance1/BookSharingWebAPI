using BookSharingApp.Common;
using BookSharingApp.Validators;
using Microsoft.AspNetCore.Http;

namespace BookSharingWebAPI.Validators
{
    public class CoverImageValidator
    {
        public ValidationResult Validate(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return ValidationResult.Failure("No image file provided");

            if (imageFile.Length > ImageAnalysisConstants.MaxImageFileSizeBytes)
            {
                var maxSizeMb = ImageAnalysisConstants.MaxImageFileSizeBytes / (1024 * 1024);
                return ValidationResult.Failure($"Image too large. Maximum size is {maxSizeMb}MB.");
            }

            var contentType = imageFile.ContentType?.ToLowerInvariant() ?? "";
            if (!ImageAnalysisConstants.SupportedImageTypes.Contains(contentType))
                return ValidationResult.Failure("Invalid image type. Use JPEG, PNG, or WebP.");

            return ValidationResult.Success();
        }
    }
}
