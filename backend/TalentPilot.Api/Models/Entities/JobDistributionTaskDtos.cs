namespace TalentPilot.Api.Models.Entities;

// === DTOs ===

public record DistributionTaskDto(
    long Id,
    int JobPostId,
    string JobTitle,
    string ChannelType,
    string ChannelName,
    string TaskStatus,
    string StatusText,
    DateTime? ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? FailureReason,
    DateTime CreatedAt
);

public record CreateDistributionTaskDto(
    int JobPostId,
    List<string> ChannelTypes,
    DateTime? ScheduledAt
);

public record TriggerDistributionDto(
    int JobPostId,
    List<string> ChannelTypes
);

public record DistributionLogDto(
    long Id,
    long TaskId,
    string LogLevel,
    string Message,
    string? Details,
    DateTime CreatedAt
);

public record TriggerResultDto(
    bool Success,
    string Message,
    List<DistributionTaskDto> Tasks
);
