namespace JuniperFox.Api.Hubs;

public static class ListHubGroups
{
    public static string ForList(Guid listId) => $"list:{listId:D}";
}
