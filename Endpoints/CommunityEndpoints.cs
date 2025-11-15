using BookSharingApp.Common;
using BookSharingApp.Data;
using BookSharingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookSharingApp.Endpoints
{
    public static class CommunityEndpoints
    {
        public static void MapCommunityEndpoints(this WebApplication app)
        {
            var communities = app.MapGroup("/communities").WithTags("Communities").RequireAuthorization();

            communities.MapGet("/", async (ApplicationDbContext context) => 
                await context.Communities.ToListAsync())
               .WithName("GetAllCommunities")
               .WithOpenApi();

            communities.MapGet("/{id:int}", async (int id, ApplicationDbContext context) => 
            {
                var community = await context.Communities.FindAsync(id);
                return community is not null ? Results.Ok(community) : Results.NotFound();
            })
            .WithName("GetCommunityById")
            .WithOpenApi();

            communities.MapPost("/", async (string name, HttpContext httpContext, ApplicationDbContext context) =>
            {
                // Get the current user ID from the JWT token
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                // Check if user has already created 2 communities
                var userCommunitiesCount = await context.Communities
                    .CountAsync(c => c.CreatedBy == currentUserId);

                if (userCommunitiesCount >= Constants.CommunityCreationLimit)
                {
                    return Results.BadRequest(new { error = "You have reached the maximum limit of 5 communities." });
                }

                // Use a transaction to ensure both operations succeed or fail together
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    // Create the new community
                    var community = new Community
                    {
                        Name = name,
                        CreatedBy = currentUserId,
                        Active = true
                    };

                    context.Communities.Add(community);
                    await context.SaveChangesAsync();

                    // Add the creator as a community user with moderator privileges
                    var communityUser = new CommunityUser
                    {
                        CommunityId = community.Id,
                        UserId = currentUserId,
                        IsModerator = true
                    };

                    context.CommunityUsers.Add(communityUser);
                    await context.SaveChangesAsync();

                    // Commit the transaction if both operations succeed
                    await transaction.CommitAsync();

                    // Clear navigation properties to avoid circular reference issues
                    community.Members = null!;
                    community.Creator = null;

                    return Results.Created($"/communities/{community.Id}", community);
                }
                catch
                {
                    // Transaction will automatically rollback if we don't commit
                    await transaction.RollbackAsync();
                    throw;
                }
            })
            .WithName("AddCommunity")
            .WithOpenApi();

            communities.MapPut("/{id:int}", async (int id, Community community, ApplicationDbContext context) => 
            {
                var existingCommunity = await context.Communities.FindAsync(id);
                if (existingCommunity is null)
                {
                    return Results.NotFound();
                }

                existingCommunity.Name = community.Name;
                existingCommunity.Active = community.Active;

                await context.SaveChangesAsync();
                return Results.Ok(existingCommunity);
            })
            .WithName("UpdateCommunity")
            .WithOpenApi();

            communities.MapDelete("/{id:int}", async (int id, ApplicationDbContext context) => 
            {
                var community = await context.Communities.FindAsync(id);
                if (community is null)
                {
                    return Results.NotFound();
                }

                context.Communities.Remove(community);
                await context.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("DeleteCommunity")
            .WithOpenApi();
        }
    }
}