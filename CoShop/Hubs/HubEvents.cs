namespace CoShop.Hubs;

// Every message sent over SignalR uses one of these records.
// The "Actor" fields let the frontend skip re-rendering its own optimistic updates.

public record ItemCreatedEvent(int ListId, int ItemId, string Name, decimal Amount, string Unit, int ActorUserId, string ActorUsername);

public record ItemUpdatedEvent(int ListId, int ItemId, string Name, decimal Amount, string Unit, int ActorUserId, string ActorUsername);

public record ItemDeletedEvent(int ListId, int ItemId, int ActorUserId);

public record ItemBoughtToggledEvent(int ListId, int ItemId, bool IsBought, int ActorUserId, string ActorUsername);

public record ListRenamedEvent(int ListId, string NewTitle, int ActorUserId);

public record ListDeletedEvent(int ListId, int ActorUserId);

public record MemberAddedEvent(int ListId, int UserId, string Username);

public record MemberRemovedEvent(int ListId, int UserId);