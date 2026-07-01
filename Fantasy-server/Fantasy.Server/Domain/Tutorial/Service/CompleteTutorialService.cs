using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Constant;
using Fantasy.Server.Domain.Tutorial.Dto.Response;
using Fantasy.Server.Domain.Tutorial.Entity;
using Fantasy.Server.Domain.Tutorial.Repository.Interface;
using Fantasy.Server.Domain.Tutorial.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Tutorial.Service;

public class CompleteTutorialService : ICompleteTutorialService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerTutorialRepository _playerTutorialRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public CompleteTutorialService(
        IPlayerRepository playerRepository,
        IPlayerTutorialRepository playerTutorialRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerTutorialRepository = playerTutorialRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<TutorialCompleteResponse> ExecuteAsync(string tutorialId)
    {
        if (!TutorialIds.All.Contains(tutorialId))
            throw new BadRequestException("존재하지 않는 튜토리얼입니다.");

        long accountId = _currentUserProvider.GetAccountId();

        PlayerEntity player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어를 찾을 수 없습니다.");

        PlayerTutorial? existing = await _playerTutorialRepository.FindByPlayerIdAndTutorialIdAsync(player.Id, tutorialId);
        if (existing != null)
            return new TutorialCompleteResponse(tutorialId, true, existing.CompletedAt);

        PlayerTutorial created = await _playerTutorialRepository.SaveAsync(PlayerTutorial.Create(player.Id, tutorialId));
        return new TutorialCompleteResponse(tutorialId, false, created.CompletedAt);
    }
}
