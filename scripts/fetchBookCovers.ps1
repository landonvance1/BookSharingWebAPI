#!/usr/bin/env pwsh

# Script to fetch book cover images from OpenLibrary API
# Maps seed data to book IDs and downloads cover images

$books = @(
    @{ Id = 1; Title = "The Great Gatsby"; Author = "F. Scott Fitzgerald" },
    @{ Id = 2; Title = "To Kill a Mockingbird"; Author = "Harper Lee" },
    @{ Id = 3; Title = "1984"; Author = "George Orwell" },
    @{ Id = 4; Title = "Wolf Hall"; Author = "Hilary Mantel" },
    @{ Id = 5; Title = "Bring Up the Bodies"; Author = "Hilary Mantel" },
    @{ Id = 6; Title = "The Mirror & the Light"; Author = "Hilary Mantel" },
    @{ Id = 7; Title = "A Place of Greater Safety"; Author = "Hilary Mantel" },
    @{ Id = 8; Title = "The Blade Itself"; Author = "Joe Abercrombie" },
    @{ Id = 9; Title = "Before They Are Hanged"; Author = "Joe Abercrombie" },
    @{ Id = 10; Title = "Last Argument of Kings"; Author = "Joe Abercrombie" },
    @{ Id = 11; Title = "Best Served Cold"; Author = "Joe Abercrombie" },
    @{ Id = 12; Title = "The Left Hand of Darkness"; Author = "Ursula K Le Guin" },
    @{ Id = 13; Title = "A Wizard of Earthsea"; Author = "Ursula K Le Guin" },
    @{ Id = 14; Title = "The Dispossessed"; Author = "Ursula K Le Guin" },
    @{ Id = 15; Title = "The Lathe of Heaven"; Author = "Ursula K Le Guin" },
    @{ Id = 16; Title = "Assassin's Apprentice"; Author = "Robin Hobb" },
    @{ Id = 17; Title = "Royal Assassin"; Author = "Robin Hobb" },
    @{ Id = 18; Title = "Assassin's Quest"; Author = "Robin Hobb" },
    @{ Id = 19; Title = "Ship of Magic"; Author = "Robin Hobb" },
    @{ Id = 20; Title = "Jonathan Strange & Mr Norrell"; Author = "Susanna Clarke" },
    @{ Id = 21; Title = "Piranesi"; Author = "Susanna Clarke" },
    @{ Id = 22; Title = "The Ladies of Grace Adieu"; Author = "Susanna Clarke" },
    @{ Id = 23; Title = "The Wood at Midwinter"; Author = "Susanna Clarke" }
)

$imagesDir = "./wwwroot/images"
Write-Host "Fetching book cover images from OpenLibrary..."

foreach ($book in $books) {
    $title = [System.Web.HttpUtility]::UrlEncode($book.Title)
    $author = [System.Web.HttpUtility]::UrlEncode($book.Author)
    $searchUrl = "https://openlibrary.org/search.json?title=$title&author=$author&limit=1"
    
    Write-Host "Searching for: $($book.Title) by $($book.Author) (ID: $($book.Id))"
    
    try {
        # Search for the book
        $response = Invoke-RestMethod -Uri $searchUrl -Method Get
        
        if ($response.docs -and $response.docs.Count -gt 0 -and $response.docs[0].cover_i) {
            $coverId = $response.docs[0].cover_i
            $coverUrl = "https://covers.openlibrary.org/b/id/$coverId-L.jpg"
            $outputPath = "$imagesDir/$($book.Id).jpg"
            
            Write-Host "  Found cover ID: $coverId"
            Write-Host "  Downloading: $coverUrl -> $outputPath"
            
            # Download the cover image
            Invoke-WebRequest -Uri $coverUrl -OutFile $outputPath
            Write-Host "  ✓ Downloaded successfully"
        }
        else {
            Write-Host "  ✗ No cover found"
        }
        
        # Be respectful to the API
        Start-Sleep -Seconds 1
    }
    catch {
        Write-Host "  ✗ Error: $($_.Exception.Message)"
    }
}

Write-Host "Done fetching book covers!"