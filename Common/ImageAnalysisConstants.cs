namespace BookSharingApp.Common
{
    /// <summary>
    /// Configuration constants for book cover image analysis feature.
    /// </summary>
    public static class ImageAnalysisConstants
    {
        /// <summary>Maximum file size for image uploads (4MB).</summary>
        /// <remarks>Limited by Azure Computer Vision API constraints.</remarks>
        public const int MaxImageFileSizeBytes = 4 * 1024 * 1024;

        /// <summary>Supported image MIME types for cover analysis.</summary>
        public static readonly string[] SupportedImageTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        /// <summary>Maximum polling attempts for OCR result retrieval.</summary>
        /// <remarks>Azure OCR typically completes in 2-5 attempts with 1 second delay.</remarks>
        public const int MaxOcrPollingAttempts = 10;

        /// <summary>Delay between OCR polling attempts (milliseconds).</summary>
        public const int OcrPollingDelayMs = 1000;

        /// <summary>Filters all text smaller than percentage relative to largest text detected.</summary>
        public const double TextSizeFilterThresholdPercentage = 0.2;

        /// <summary>Minimum title length for OCR extraction (characters).</summary>
        public const int MinTitleLength = 3;

        /// <summary>Maximum title length for OCR extraction (characters).</summary>
        /// <remarks>Real book titles rarely exceed 200 characters.</remarks>
        public const int MaxTitleLength = 200;

        /// <summary>Maximum number of results to return from endpoint.</summary>
        public const int MaxResultsPerResponse = 5;

        /// <summary>Minimum number of words to include in search query.</summary>
        public const int MinSearchWords = 2;

        /// <summary>Maximum number of words to include in search query.</summary>
        public const int MaxSearchWords = 15;

        /// <summary>Minimum word match percentage for filtering results (0.0 to 1.0).</summary>
        public const double MinWordMatchThreshold = 0.5;

        /// <summary>Number of retries calling to external lookup services (eg OpenLibrary).</summary>
        public const int MaxLookupRetries = 3;

        // --- Sharpening constants ---

        /// <summary>Minimum words required before sharpening is attempted.</summary>
        /// <remarks>With fewer than 2 words there is nothing meaningful to trim.</remarks>
        public const int SharpenMinWordsRequired = 2;

        /// <summary>Minimum number of height tiers required to attempt gap-based sharpening.</summary>
        /// <remarks>With only 1-2 tiers there is no meaningful gap to exploit.</remarks>
        public const int SharpenMinTiersRequired = 3;

        /// <summary>Heights within this percentage of each other are grouped into the same tier.</summary>
        /// <remarks>0.10 = words whose heights differ by ≤10% are considered the same visual size.</remarks>
        public const double SharpenTierGroupingTolerance = 0.10;

        /// <summary>Minimum relative gap between tiers to qualify as a significant visual boundary.</summary>
        /// <remarks>0.25 = a 25% drop from one tier to the next is considered significant.</remarks>
        public const double SharpenMinGapThreshold = 0.25;

        /// <summary>Minimum words that must survive sharpening; if fewer remain, sharpening is skipped.</summary>
        public const int SharpenMinWordsAfterCut = 2;
    }
}
