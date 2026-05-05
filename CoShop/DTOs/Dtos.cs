using System.ComponentModel.DataAnnotations;

namespace CoShop.DTOs;

// ── Auth ──────────────────────────────────────────────────────────────────────

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(50),
     RegularExpression(@"^[a-zA-Z0-9_\-]+$", ErrorMessage = "Username darf nur Buchstaben, Zahlen, _ und - enthalten.")]
    string Username,

    [Required, EmailAddress, MaxLength(200)]
    string Email,

    [Required, MinLength(8), MaxLength(100),
     RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Passwort braucht mind. 1 Grossbuchstabe, 1 Kleinbuchstabe und 1 Zahl.")]
    string Password
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required]               string Password
);

public record AuthResponse(string Token, int UserId, string Username, string Role);

// ── User ──────────────────────────────────────────────────────────────────────

public record UserDto(int Id, string Username, string Email, string Role);

// ── ShoppingList ──────────────────────────────────────────────────────────────

public record CreateListRequest(
    [Required, MinLength(1), MaxLength(100)] string Title
);

public record UpdateListRequest(
    [Required, MinLength(1), MaxLength(100)] string Title
);

public record ShoppingListSummaryDto(
    int Id, string Title, DateTime CreatedAt,
    int OwnerId, int ItemCount, int MemberCount
);

public record ShoppingListDetailDto(
    int Id, string Title, DateTime CreatedAt,
    int OwnerId, string OwnerUsername,
    IEnumerable<ItemDto> Items,
    IEnumerable<MemberDto> Members
);

// ── Item ──────────────────────────────────────────────────────────────────────

public record CreateItemRequest(
    [Required, MinLength(1), MaxLength(100)] string Name,
    [Range(0.001, 99999, ErrorMessage = "Menge muss zwischen 0.001 und 99999 liegen.")]
    decimal Amount,
    [MaxLength(30)] string Unit
);

public record UpdateItemRequest(
    [Required, MinLength(1), MaxLength(100)] string Name,
    [Range(0.001, 99999, ErrorMessage = "Menge muss zwischen 0.001 und 99999 liegen.")]
    decimal Amount,
    [MaxLength(30)] string Unit
);

public record ToggleBoughtRequest(
    [Required] bool IsBought
);

public record ItemDto(
    int Id, string Name, decimal Amount, string Unit,
    bool IsBought, int ListId,
    int LastModifiedByUserId, string LastModifiedByUsername
);

// ── Members ───────────────────────────────────────────────────────────────────

public record InviteMemberRequest(
    [Required, MinLength(3), MaxLength(50)] string Username
);

public record MemberDto(int UserId, string Username, DateTime JoinedAt);