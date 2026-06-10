using Fantasy.Server.Domain.Dungeon.Dto.Response;

namespace Fantasy.Server.Domain.Dungeon.Service.Interface;

public interface IDungeonTicketService
{
    Task<DungeonTicketResponse> ExecuteAsync();
}
