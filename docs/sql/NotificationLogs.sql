-- NotificationLogs table for TalentPilot
CREATE TABLE IF NOT EXISTS `NotificationLogs` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `CandidateId` BIGINT NOT NULL,
    `NotificationType` VARCHAR(50) NOT NULL DEFAULT 'InterviewInvitation',
    `Channel` VARCHAR(20) NOT NULL DEFAULT 'Email',
    `Recipient` VARCHAR(200) NOT NULL,
    `Subject` VARCHAR(500) NULL,
    `Content` TEXT NULL,
    `Status` VARCHAR(20) NOT NULL DEFAULT 'Pending',
    `SentAt` DATETIME NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `ErrorMessage` VARCHAR(1000) NULL,
    PRIMARY KEY (`Id`),
    INDEX `idx_candidate_id` (`CandidateId`),
    INDEX `idx_notification_type` (`NotificationType`),
    INDEX `idx_status` (`Status`),
    INDEX `idx_created_at` (`CreatedAt`),
    CONSTRAINT `fk_notificationlogs_candidate`
        FOREIGN KEY (`CandidateId`) REFERENCES `Candidates` (`Id`)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;