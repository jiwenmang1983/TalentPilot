CREATE TABLE IF NOT EXISTS JobChannelContents (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    JobPostId INT NOT NULL,
    ChannelType VARCHAR(20) NOT NULL,          -- liepin/lagou/boss/linkedin/xiaohongshu/custom
    ChannelName VARCHAR(50) NOT NULL,
    AdaptedTitle VARCHAR(200),                  -- 渠道专属标题
    AdaptedContent TEXT,                          -- 适配后正文（HTML/Markdown）
    WordCount INT DEFAULT 0,
    SkillTags JSON,                             -- 技能标签数组
    SalaryMin INT,
    SalaryMax INT,
    Status VARCHAR(20) DEFAULT 'pending',       -- pending/ready/failed
    AdaptationPrompt TEXT,                       -- 使用的Prompt快照
    ErrorMessage VARCHAR(500),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_job_channel (JobPostId, ChannelType),
    INDEX idx_status (Status)
);