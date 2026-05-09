using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Data;

public class TalentPilotDbContext : DbContext
{
    public TalentPilotDbContext(DbContextOptions<TalentPilotDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
    public DbSet<UserLoginAttempt> UserLoginAttempts => Set<UserLoginAttempt>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateConsent> CandidateConsents => Set<CandidateConsent>();
    public DbSet<JobPost> JobPosts => Set<JobPost>();
    public DbSet<ResumeSource> ResumeSources => Set<ResumeSource>();
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();
    public DbSet<InterviewInvitation> InterviewInvitations => Set<InterviewInvitation>();
    public DbSet<AIInterviewSession> AIInterviewSessions => Set<AIInterviewSession>();
    public DbSet<InterviewReport> InterviewReports => Set<InterviewReport>();
    public DbSet<ConversionFunnel> ConversionFunnels => Set<ConversionFunnel>();
    public DbSet<ResumeParsedRecord> ResumeParsedRecords => Set<ResumeParsedRecord>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<ChannelCredential> ChannelCredentials => Set<ChannelCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RoleKey).IsUnique();
        });

        // Department - self-referencing relationship
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Parent)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Department)
                  .WithMany()
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Role)
                  .WithMany()
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);

            // One-to-many: User -> UserLoginAttempt
            entity.HasMany(e => e.LoginAttempts)
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // RolePermission
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoleId, e.PermissionKey }).IsUnique();

            entity.HasOne(e => e.Role)
                  .WithMany()
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // OperationLog
        modelBuilder.Entity<OperationLog>(entity =>
        {
            entity.ToTable("OperationLogs");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // UserLoginAttempt
        modelBuilder.Entity<UserLoginAttempt>(entity =>
        {
            entity.ToTable("UserLoginAttempts");
            entity.HasKey(e => e.Id);
        });

        // Global query filter for soft delete
        modelBuilder.Entity<Role>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Department>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Candidate>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ChannelCredential>().HasQueryFilter(e => !e.IsDeleted);

        // Candidate
        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.ToTable("Candidates");
            entity.HasKey(e => e.Id);
        });

        // CandidateConsent
        modelBuilder.Entity<CandidateConsent>(entity =>
        {
            entity.ToTable("CandidateConsents");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Candidate)
                  .WithMany()
                  .HasForeignKey(e => e.CandidateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // JobPost
        modelBuilder.Entity<JobPost>(entity =>
        {
            entity.ToTable("JobPosts");
            entity.HasKey(e => e.Id);
        });

        // ResumeSource
        modelBuilder.Entity<ResumeSource>(entity =>
        {
            entity.ToTable("ResumeSources");
            entity.HasKey(e => e.Id);
        });

        // Resume
        modelBuilder.Entity<Resume>(entity =>
        {
            entity.ToTable("Resumes");
            entity.HasKey(e => e.Id);
        });

        // MatchResult
        modelBuilder.Entity<MatchResult>(entity =>
        {
            entity.ToTable("MatchResults");
            entity.HasKey(e => e.Id);
        });

        // InterviewInvitation
        modelBuilder.Entity<InterviewInvitation>(entity =>
        {
            entity.ToTable("InterviewInvitations");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Candidate)
                  .WithMany()
                  .HasForeignKey(e => e.CandidateId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.JobPost)
                  .WithMany()
                  .HasForeignKey(e => e.JobPostId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.InvitedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.InvitedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.InviteToken).IsUnique();
        });

        // AIInterviewSession
        modelBuilder.Entity<AIInterviewSession>(entity =>
        {
            entity.ToTable("AIInterviewSessions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.InterviewInvitation)
                  .WithMany()
                  .HasForeignKey(e => e.InterviewInvitationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Candidate)
                  .WithMany()
                  .HasForeignKey(e => e.CandidateId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.JobPost)
                  .WithMany()
                  .HasForeignKey(e => e.JobPostId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // InterviewReport
        modelBuilder.Entity<InterviewReport>(entity =>
        {
            entity.ToTable("InterviewReports");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.AIInterviewSession)
                  .WithMany()
                  .HasForeignKey(e => e.AIInterviewSessionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Candidate)
                  .WithMany()
                  .HasForeignKey(e => e.CandidateId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.JobPost)
                  .WithMany()
                  .HasForeignKey(e => e.JobPostId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ConversionFunnel
        modelBuilder.Entity<ConversionFunnel>(entity =>
        {
            entity.ToTable("ConversionFunnels");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.JobPost)
                  .WithMany()
                  .HasForeignKey(e => e.JobPostId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ResumeParsedRecord
        modelBuilder.Entity<ResumeParsedRecord>(entity =>
        {
            entity.ToTable("ResumeParsedRecords");
            entity.HasKey(e => e.Id);
        });

        // NotificationLog
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.ToTable("NotificationLogs");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Candidate)
                  .WithMany()
                  .HasForeignKey(e => e.CandidateId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ChannelCredential
        modelBuilder.Entity<ChannelCredential>(entity =>
        {
            entity.ToTable("ChannelCredentials");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChannelType).IsUnique();
        });
    }
}