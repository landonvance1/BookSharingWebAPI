namespace BookSharingApp.Models
{
    public record CommunityWithMemberCountDto(int Id, string Name, bool Active, int MemberCount);
}
