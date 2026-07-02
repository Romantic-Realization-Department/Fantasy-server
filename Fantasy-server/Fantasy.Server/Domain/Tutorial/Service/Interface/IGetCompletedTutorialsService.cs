using Fantasy.Server.Domain.Tutorial.Dto.Response;

namespace Fantasy.Server.Domain.Tutorial.Service.Interface;

public interface IGetCompletedTutorialsService
{
    Task<CompletedTutorialsResponse> ExecuteAsync();
}
