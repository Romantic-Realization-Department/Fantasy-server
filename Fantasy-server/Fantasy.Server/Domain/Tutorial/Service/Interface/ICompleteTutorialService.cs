using Fantasy.Server.Domain.Tutorial.Dto.Response;

namespace Fantasy.Server.Domain.Tutorial.Service.Interface;

public interface ICompleteTutorialService
{
    Task<TutorialCompleteResponse> ExecuteAsync(string tutorialId);
}
