using BookSharingApp.Models;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Tests.Endpoints
{
    /// <summary>
    /// Tests for community endpoint query logic.
    /// Since CommunityEndpoints don't have a service layer, these tests verify
    /// the database queries used directly in the endpoints.
    /// </summary>
    public class CommunityEndpointTests
    {
        public class GetCommunityByIdTests
        {
            [Fact]
            public async Task GetCommunityById_WithNoMembers_ReturnsDtoWithZeroMemberCount()
            {
                // Arrange
                using var context = DbContextHelper.CreateInMemoryContext();

                var community = TestDataBuilder.CreateCommunity(name: "Empty Book Club");
                context.Communities.Add(community);
                await context.SaveChangesAsync();

                // Act - Query pattern from CommunityEndpoints.cs:23-30
                var communityDto = await context.Communities
                    .Where(c => c.Id == community.Id)
                    .Select(c => new CommunityWithMemberCountDto(
                        c.Id,
                        c.Name,
                        c.Active,
                        c.Members.Count()
                    ))
                    .FirstOrDefaultAsync();

                // Assert
                communityDto.Should().NotBeNull();
                communityDto!.Id.Should().Be(community.Id);
                communityDto.Name.Should().Be("Empty Book Club");
                communityDto.Active.Should().BeTrue();
                communityDto.MemberCount.Should().Be(0);
            }

            [Fact]
            public async Task GetCommunityById_WithMultipleMembers_ReturnsDtoWithCorrectMemberCount()
            {
                // Arrange
                using var context = DbContextHelper.CreateInMemoryContext();

                var user1 = TestDataBuilder.CreateUser(id: "user-1", firstName: "John");
                var user2 = TestDataBuilder.CreateUser(id: "user-2", firstName: "Jane");
                var user3 = TestDataBuilder.CreateUser(id: "user-3", firstName: "Bob");
                var community = TestDataBuilder.CreateCommunity(name: "Book Lovers");

                context.Users.AddRange(user1, user2, user3);
                context.Communities.Add(community);
                await context.SaveChangesAsync();

                var communityUser1 = TestDataBuilder.CreateCommunityUser(community.Id, user1.Id);
                var communityUser2 = TestDataBuilder.CreateCommunityUser(community.Id, user2.Id);
                var communityUser3 = TestDataBuilder.CreateCommunityUser(community.Id, user3.Id);
                context.CommunityUsers.AddRange(communityUser1, communityUser2, communityUser3);
                await context.SaveChangesAsync();

                // Act - Query pattern from CommunityEndpoints.cs:23-30
                var communityDto = await context.Communities
                    .Where(c => c.Id == community.Id)
                    .Select(c => new CommunityWithMemberCountDto(
                        c.Id,
                        c.Name,
                        c.Active,
                        c.Members.Count()
                    ))
                    .FirstOrDefaultAsync();

                // Assert
                communityDto.Should().NotBeNull();
                communityDto!.Id.Should().Be(community.Id);
                communityDto.Name.Should().Be("Book Lovers");
                communityDto.Active.Should().BeTrue();
                communityDto.MemberCount.Should().Be(3);
            }

            [Fact]
            public async Task GetCommunityById_WhenCommunityDoesNotExist_ReturnsNull()
            {
                // Arrange
                using var context = DbContextHelper.CreateInMemoryContext();
                var nonExistentId = 999;

                // Act - Query pattern from CommunityEndpoints.cs:23-30
                var communityDto = await context.Communities
                    .Where(c => c.Id == nonExistentId)
                    .Select(c => new CommunityWithMemberCountDto(
                        c.Id,
                        c.Name,
                        c.Active,
                        c.Members.Count()
                    ))
                    .FirstOrDefaultAsync();

                // Assert
                communityDto.Should().BeNull();
            }

            [Fact]
            public async Task GetCommunityById_WithInactiveCommunity_ReturnsDtoWithCorrectActiveStatus()
            {
                // Arrange
                using var context = DbContextHelper.CreateInMemoryContext();

                var community = new Community
                {
                    Name = "Inactive Club",
                    Active = false,
                    CreatedBy = "user-1"
                };
                context.Communities.Add(community);
                await context.SaveChangesAsync();

                // Act - Query pattern from CommunityEndpoints.cs:23-30
                var communityDto = await context.Communities
                    .Where(c => c.Id == community.Id)
                    .Select(c => new CommunityWithMemberCountDto(
                        c.Id,
                        c.Name,
                        c.Active,
                        c.Members.Count()
                    ))
                    .FirstOrDefaultAsync();

                // Assert
                communityDto.Should().NotBeNull();
                communityDto!.Active.Should().BeFalse();
            }
        }

        public class JoinCommunityTests
        {
            [Fact]
            public async Task JoinCommunity_AfterJoining_ReturnsDtoWithUpdatedMemberCount()
            {
                // Arrange
                using var context = DbContextHelper.CreateInMemoryContext();

                var user1 = TestDataBuilder.CreateUser(id: "user-1", firstName: "John");
                var user2 = TestDataBuilder.CreateUser(id: "user-2", firstName: "Jane");
                var newUser = TestDataBuilder.CreateUser(id: "new-user", firstName: "Bob");
                var community = TestDataBuilder.CreateCommunity(name: "Book Club");

                context.Users.AddRange(user1, user2, newUser);
                context.Communities.Add(community);
                await context.SaveChangesAsync();

                var communityUser1 = TestDataBuilder.CreateCommunityUser(community.Id, user1.Id);
                var communityUser2 = TestDataBuilder.CreateCommunityUser(community.Id, user2.Id);
                context.CommunityUsers.AddRange(communityUser1, communityUser2);
                await context.SaveChangesAsync();

                // Simulate join
                var newCommunityUser = new CommunityUser
                {
                    CommunityId = community.Id,
                    UserId = newUser.Id
                };
                context.CommunityUsers.Add(newCommunityUser);
                await context.SaveChangesAsync();

                // Act - Query pattern from CommunityUserEndpoints.cs:44-52
                var communityDto = await context.Communities
                    .Where(c => c.Id == community.Id)
                    .Select(c => new CommunityWithMemberCountDto(
                        c.Id,
                        c.Name,
                        c.Active,
                        c.Members.Count()
                    ))
                    .FirstAsync();

                // Assert
                communityDto.Should().NotBeNull();
                communityDto.Id.Should().Be(community.Id);
                communityDto.Name.Should().Be("Book Club");
                communityDto.Active.Should().BeTrue();
                communityDto.MemberCount.Should().Be(3, "because the new user just joined");
            }

            [Fact]
            public async Task JoinCommunity_WhenFirstMember_ReturnsDtoWithMemberCountOfOne()
            {
                // Arrange
                using var context = DbContextHelper.CreateInMemoryContext();

                var user = TestDataBuilder.CreateUser(id: "user-1", firstName: "John");
                var community = TestDataBuilder.CreateCommunity(name: "New Club");

                context.Users.Add(user);
                context.Communities.Add(community);
                await context.SaveChangesAsync();

                // Simulate join
                var communityUser = new CommunityUser
                {
                    CommunityId = community.Id,
                    UserId = user.Id
                };
                context.CommunityUsers.Add(communityUser);
                await context.SaveChangesAsync();

                // Act - Query pattern from CommunityUserEndpoints.cs:44-52
                var communityDto = await context.Communities
                    .Where(c => c.Id == community.Id)
                    .Select(c => new CommunityWithMemberCountDto(
                        c.Id,
                        c.Name,
                        c.Active,
                        c.Members.Count()
                    ))
                    .FirstAsync();

                // Assert
                communityDto.Should().NotBeNull();
                communityDto.MemberCount.Should().Be(1);
            }

            [Fact]
            public async Task JoinCommunity_MemberCountReflectsActualDatabaseState()
            {
                // Arrange
                using var context = DbContextHelper.CreateInMemoryContext();

                var community = TestDataBuilder.CreateCommunity(name: "Test Club");
                context.Communities.Add(community);
                await context.SaveChangesAsync();

                // Add 5 members
                for (int i = 1; i <= 5; i++)
                {
                    var user = TestDataBuilder.CreateUser(id: $"user-{i}", firstName: $"User{i}");
                    context.Users.Add(user);
                    await context.SaveChangesAsync();

                    var communityUser = TestDataBuilder.CreateCommunityUser(community.Id, user.Id);
                    context.CommunityUsers.Add(communityUser);
                    await context.SaveChangesAsync();
                }

                // Act - Query pattern from CommunityUserEndpoints.cs:44-52
                var communityDto = await context.Communities
                    .Where(c => c.Id == community.Id)
                    .Select(c => new CommunityWithMemberCountDto(
                        c.Id,
                        c.Name,
                        c.Active,
                        c.Members.Count()
                    ))
                    .FirstAsync();

                // Assert
                communityDto.MemberCount.Should().Be(5);

                // Verify against actual database count
                var actualCount = await context.CommunityUsers
                    .CountAsync(cu => cu.CommunityId == community.Id);
                communityDto.MemberCount.Should().Be(actualCount);
            }
        }

        public class GetUserCommunitiesTests
        {
            [Fact]
            public async Task GetUserCommunities_WithMultipleCommunities_ReturnsAllWithMemberCounts()
            {
                // Arrange
                using var context = DbContextHelper.CreateInMemoryContext();

                var user = TestDataBuilder.CreateUser(id: "user-1", firstName: "John");
                var community1 = TestDataBuilder.CreateCommunity(name: "Club 1");
                var community2 = TestDataBuilder.CreateCommunity(name: "Club 2");

                context.Users.Add(user);
                context.Communities.AddRange(community1, community2);
                await context.SaveChangesAsync();

                // User joins both communities
                var cu1 = TestDataBuilder.CreateCommunityUser(community1.Id, user.Id);
                var cu2 = TestDataBuilder.CreateCommunityUser(community2.Id, user.Id);
                context.CommunityUsers.AddRange(cu1, cu2);

                // Add another member to community1
                var otherUser = TestDataBuilder.CreateUser(id: "user-2", firstName: "Jane");
                context.Users.Add(otherUser);
                await context.SaveChangesAsync();

                var cu3 = TestDataBuilder.CreateCommunityUser(community1.Id, otherUser.Id);
                context.CommunityUsers.Add(cu3);
                await context.SaveChangesAsync();

                // Act - Query pattern from CommunityUserEndpoints.cs:85-93
                var userCommunities = await context.CommunityUsers
                    .Where(cu => cu.UserId == user.Id)
                    .Select(cu => new CommunityWithMemberCountDto(
                        cu.Community.Id,
                        cu.Community.Name,
                        cu.Community.Active,
                        cu.Community.Members.Count()
                    ))
                    .ToListAsync();

                // Assert
                userCommunities.Should().HaveCount(2);

                var club1Dto = userCommunities.First(c => c.Name == "Club 1");
                club1Dto.MemberCount.Should().Be(2, "because two users joined Club 1");

                var club2Dto = userCommunities.First(c => c.Name == "Club 2");
                club2Dto.MemberCount.Should().Be(1, "because only one user joined Club 2");
            }
        }
    }
}
