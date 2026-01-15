using BookSharingApp.Common;
using BookSharingApp.Models;

namespace BookSharingApp.Validators
{
    public class ShareStatusValidator
    {
        public ValidationResult ValidateStatusTransition(Share share, ShareStatus newStatus, string currentUserId)
        {
            // Check if user is allowed to perform this action (owner or borrower)
            if (share.Borrower != currentUserId && share.UserBook.UserId != currentUserId)
            {
                return ValidationResult.Failure("You are not authorized to update this share");
            }

            // Cannot update status if share is disputed
            if (share.IsDisputed)
            {
                return ValidationResult.Failure("Cannot update status of a disputed share");
            }

            // Check role-specific permissions
            var isLender = share.UserBook.UserId == currentUserId;
            var isBorrower = share.Borrower == currentUserId;

            // Only lender can move to Declined, Ready and HomeSafe
            if ((newStatus == ShareStatus.Ready || newStatus == ShareStatus.HomeSafe || newStatus == ShareStatus.Declined) && !isLender)
            {
                return ValidationResult.Failure("Only the lender can update status to Declined, Ready, or Home Safe");
            }

            // Only borrower can move to PickedUp and Returned
            if ((newStatus == ShareStatus.PickedUp || newStatus == ShareStatus.Returned) && !isBorrower)
            {
                return ValidationResult.Failure("Only the borrower can update status to Picked Up or Returned");
            }

            // Check status progression rules
            if (!IsValidStatusTransition(share.Status, newStatus))
            {
                return ValidationResult.Failure($"Invalid status transition from {share.Status} to {newStatus}");
            }

            return ValidationResult.Success();
        }

        private bool IsValidStatusTransition(ShareStatus currentStatus, ShareStatus newStatus)
        {
            // Cannot go backwards or stay the same
            if (newStatus <= currentStatus)
                return false;

            // Check valid progression: Requested -> Ready -> PickedUp -> Returned -> HomeSafe
            return currentStatus switch
            {
                ShareStatus.Requested => newStatus == ShareStatus.Ready || newStatus == ShareStatus.Declined,
                ShareStatus.Ready => newStatus == ShareStatus.PickedUp,
                ShareStatus.PickedUp => newStatus == ShareStatus.Returned,
                ShareStatus.Returned => newStatus == ShareStatus.HomeSafe,
                ShareStatus.HomeSafe => false, // Terminal state
                ShareStatus.Declined => false, // Terminal state
                _ => false
            };
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;

        private ValidationResult(bool isValid, string errorMessage = "")
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success() => new(true);
        public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
    }
}