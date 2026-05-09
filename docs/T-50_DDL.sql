CREATE TABLE IF NOT EXISTS JobDistributionTasks (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    JobPostId BIGINT NOT NULL,
    ChannelType VARCHAR(20) NOT NULL COMMENT 'liepin/lagou/boss/linkedin/xiaohongshu/custom',
    TaskStatus VARCHAR(20) NOT NULL DEFAULT 'pending' COMMENT 'pending/running/success/failed',
    ScheduledAt DATETIME NULL COMMENT '定时发布时间',
    StartedAt DATETIME NULL,
    CompletedAt DATETIME NULL,
    FailureReason VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_job_channel (JobPostId, ChannelType),
    INDEX idx_status (TaskStatus),
    INDEX idx_scheduled (ScheduledAt),
    INDEX idx_pending_scheduled (TaskStatus, ScheduledAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='职位分发任务表';

CREATE TABLE IF NOT EXISTS JobDistributionLogs (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    TaskId BIGINT NOT NULL,
    LogLevel VARCHAR(10) NOT NULL COMMENT 'info/warn/error',
    Message VARCHAR(1000) NOT NULL,
    Details TEXT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_task (TaskId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='职位分发日志表';
