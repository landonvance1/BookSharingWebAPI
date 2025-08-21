using BookSharingApp.Data;
using BookSharingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Endpoints
{
    public record JoinCommunityRequest(int CommunityId, string UserId);
    public record LeaveCommunityRequest(int CommunityId, string UserId);
    public record CommunityWithMemberCountDto(int Id, string Name, bool Active, int MemberCount);

    public static class CommunityUserEndpoints
    {
        public static void MapCommunityUserEndpoints(this WebApplication app)
        {
            var communityUsers = app.MapGroup("/community-users").WithTags("Community Users").RequireAuthorization();

            communityUsers.MapPost("/join", async ([FromBody] JoinCommunityRequest request, ApplicationDbContext context) => 
            {
                var existingRelation = await context.CommunityUsers
                    .FirstOrDefaultAsync(cu => cu.CommunityId == request.CommunityId && cu.UserId == request.UserId);
                
                if (existingRelation is not null)
                {
                    return Results.Conflict("User is already a member of this community");
                }

                var community = await context.Communities.FindAsync(request.CommunityId);
                if (community is null)
                {
                    return Results.NotFound("Community not found");
                }

                var user = await context.Users.FindAsync(request.UserId);
                if (user is null)
                {
                    return Results.NotFound("User not found");
                }

                var communityUser = new CommunityUser
                {
                    CommunityId = request.CommunityId,
                    UserId = request.UserId
                };

                context.CommunityUsers.Add(communityUser);
                await context.SaveChangesAsync();
                return Results.Created($"/community-users/community/{request.CommunityId}", communityUser);
            })
            .WithName("JoinCommunity")
            .WithOpenApi();

            communityUsers.MapDelete("/leave", async ([FromBody] LeaveCommunityRequest request, ApplicationDbContext context) => 
            {
                var communityUser = await context.CommunityUsers
                    .FirstOrDefaultAsync(cu => cu.CommunityId == request.CommunityId && cu.UserId == request.UserId);
                
                if (communityUser is null)
                {
                    return Results.NotFound("User is not a member of this community");
                }

                context.CommunityUsers.Remove(communityUser);
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