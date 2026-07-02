using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Dto.Response;
using Fantasy.Server.Domain.Tutorial.Entity;
using Fantasy.Server.Domain.Tutorial.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Tutorial.Service;

public class GetCompletedTutorialsService : IGetCompletedTutorialsService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerTutorialRepository _playerTutorialRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetCompletedTutorialsService(
        IPlayerRepository playerRepository,
        IPlayerTutorialRepository playerTutorialRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerTutorialRepository = playerTutorialRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<CompletedTutorialsResponse> ExecuteAsync()
    {
        long accountId = _currentUserProvider.GetAccountId();

        PlayerEntity player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어를 찾을 수 없습니다.");

        List<PlayerTutorial> tutorials = await _playerTutorialRepository.FindAllByPlayerIdAsync(player.Id);
        return new CompletedTutorialsResponse(tutorials.Select(t => t.TutorialId).ToList());
    }
}
