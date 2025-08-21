using BookSharingApp.Data;
using BookSharingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

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

            communities.MapPost("/", async (Community community, ApplicationDbContext context) => 
            {
                community.Id = 0; // Ensure EF generates new ID
                context.Communities.Add(community);
                await context.SaveChangesAsync();
                return Results.Created($"/communities/{community.Id}", community);
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