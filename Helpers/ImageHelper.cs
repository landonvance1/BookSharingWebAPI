namespace BookSharingApp.Helpers
{
    public static class ImageHelper
    {
        public static async Task DownloadThumbnailAsync(string thumbnailUrl, string isbn, IWebHostEnvironment environment)
        {
            try
            {
                var imagesPath = Path.Combine(environment.WebRootPath ?? "wwwroot", "images");
                Directory.CreateDirectory(imagesPath);
                
                var fileName = $"{isbn}.jpg";
                var filePath = Path.Combine(imagesPath, fileName);
                
                // Skip download if file already exists
                if (File.Exists(filePath))
                    return;
                
                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(thumbnailUrl);
                
                // Validate it's actually an image file by checking magic bytes
                if (!IsValidImageFile(imageBytes))
                {
                    Console.WriteLine($"Downloaded file for ISBN {isbn} is not a valid image file");
                    return;
                }
                
                await File.WriteAllBytesAsync(filePath, imageBytes);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the entire operation
                Console.WriteLine($"Failed to download thumbnail for ISBN {isbn}: {ex.Message}");
            }
        }

        public static bool IsValidImageFile(byte[] bytes)
        {
            if (bytes.Length < 4)
                return false;

            // Check for common image file magic bytes
            // JPEG: FF D8 FF
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return true;
            
            // PNG: 89 50 4E 47
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return true;
            
            // GIF: 47 49 46 38
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38)
                return true;
            
            // WebP: 52 49 46 46 (RIFF) + WebP signature at bytes 8-11
            if (bytes.Length >= 12 && 
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
                return true;
            
            return false;
        }
    }
}