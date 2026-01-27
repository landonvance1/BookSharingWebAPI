using BookSharingApp.Data;
using BookSharingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookSharingApp.Endpoints
{
    public static class CommunityUserEndpoints
    {
        public static void MapCommunityUserEndpoints(this WebApplication app)
        {
            var communityUsers = app.MapGroup("/community-users").WithTags("Community Users").RequireAuthorization();

            communityUsers.MapPost("/join/{communityId:int}", async (int communityId, HttpContext httpContext, ApplicationDbContext context) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                var existingRelation = await context.CommunityUsers
                    .FirstOrDefaultAsync(cu => cu.CommunityId == communityId && cu.UserId == currentUserId);

                if (existingRelation is not null)
                {
                    return Results.Conflict("User is already a member of this community");
                }

                var community = await context.Communities.FindAsync(communityId);
                if (community is null)
                {
                    return Results.NotFound("Community not found");
                }

                var communityUser = new CommunityUser
                {
                    CommunityId = communityId,
                    UserId = currentUserId
                };

                context.CommunityUsers.Add(communityUser);
                await context.SaveChangesAsync();

                // Return community details with updated member count
                var communityDto = await context.Communities
                    .Where(c => c.Id == communityId)
                    .Select(c => new CommunityWithMemberCountDto(
                        c.Id,
                        c.Name,
                        c.Active,
                        c.Members.Count()
                    ))
                    .FirstAsync();

                return Results.Created($"/community-users/community/{communityId}", communityDto);
            })
            .WithName("JoinCommunity")
            .WithOpenApi();

            communityUsers.MapDelete("/leave/{communityId:int}", async (int communityId, HttpContext httpContext, ApplicationDbContext context) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                var communityUser = await context.CommunityUsers
                    .FirstOrDefaultAsync(cu => cu.CommunityId == communityId && cu.UserId == currentUserId);

                if (communityUser is null)
                {
                    return Results.NotFound("User is not a member of this community");
                }

                // Check if this is the last user in the community
                var memberCount = await context.CommunityUsers
                    .CountAsync(cu => cu.CommunityId == communityId);

                context.CommunityUsers.Remove(communityUser);

                // If this is the last user, delete the community as well
                if (memberCount == 1)
                {
                    var community = await context.Communities.FindAsync(communityId);
                    if (community is not null)
                    {
                        context.Communities.Remove(community);
                    }
                }

                await context.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("LeaveCommunity")
            .WithOpenApi();

            communityUsers.MapGet("/user/{userId}", async (string userId, ApplicationDbContext context) =>
            {
                var userCommunities = await context.CommunityUsers
                    .Where(cu => cu.UserId == userId)
                    .Select(cu => new CommunityWithMemberCountDto(
                        cu.Community.Id,
                        cu.Community.Name,
                        cu.Community.Active,
                        cu.Community.Members.Count()
                    ))
                    .ToListAsync();

                return Results.Ok(userCommunities);
            })
            .WithName("GetUserCommunities")
            .WithOpenApi();

            communityUsers.MapGet("/community/{communityId:int}", async (int communityId, ApplicationDbContext context) =>
            {
                var communityUsers = await context.CommunityUsers
                    .Where(cu => cu.CommunityId == communityId)
                    .Include(cu => cu.User)
                    .Select(cu => cu.User)
                    .ToListAsync();

                return Results.Ok(communityUsers);
            })
            .WithName("GetCommunityUsers")
            .WithOpenApi();
        }
    }
}