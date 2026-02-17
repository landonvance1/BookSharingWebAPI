namespace BookSharingWebAPI.Models;

public class OcrLine
{
    public string Text { get; set; } = "";
    public List<double>? BoundingBox { get; set; }

    public double GetHeight()
    {
        if (BoundingBox == null || BoundingBox.Count < 8)
            return 0;

        // BoundingBox is [x1,y1, x2,y2, x3,y3, x4,y4]
        // Height is roughly the difference between top and bottom y coordinates
        var topY = Math.Min(BoundingBox[1], BoundingBox[3]);
        var bottomY = Math.Max(BoundingBox[5], BoundingBox[7]);
        return bottomY - topY;
    }

    public double GetWidth()
    {
        if (BoundingBox == null || BoundingBox.Count < 8)
            return 0;

        // Width is the difference between left and right x coordinates
        var leftX = Math.Min(BoundingBox[0], BoundingBox[6]);
        var rightX = Math.Max(BoundingBox[2], BoundingBox[4]);
        return rightX - leftX;
    }

    /// <summary>
    /// Determines if text is oriented vertically based on bounding box geometry.
    /// BoundingBox points: [x1,y1, x2,y2, x3,y3, x4,y4] (typically top-left, top-right, bottom-right, bottom-left)
    /// </summary>
    public bool IsVertical()
    {
        if (BoundingBox == null || BoundingBox.Count < 8)
            return false;

        // Calculate the vector from first point to second point (first edge)
        var dx = Math.Abs(BoundingBox[2] - BoundingBox[0]);
        var dy = Math.Abs(BoundingBox[3] - BoundingBox[1]);

        // If the first edge is more vertical than horizontal, text is vertical
        return dy > dx;
    }

    /// <summary>
    /// Gets the text size (font size), correctly handling both horizontal and vertical orientations.
    /// - For horizontal text: uses height (vertical extent of characters)
    /// - For vertical text: uses width (horizontal extent of characters)
    /// </summary>
    public double GetTextSize()
    {
        var height = GetHeight();
        var width = GetWidth();

        if (height == 0 || width == 0)
            return 0;

        // For vertical text, width represents the font size
        // For horizontal text, height represents the font size
        return IsVertical() ? width : height;
    }
}
