namespace Fantasy.Server.Domain.Tutorial.Dto.Response;

public record TutorialCompleteResponse(string TutorialId, bool WasAlreadyCompleted, DateTime CompletedAt);
