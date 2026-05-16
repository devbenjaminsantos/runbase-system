using Microsoft.EntityFrameworkCore;
using RunBase.Application.Security;
using RunBase.Infrastructure.Clients;
using RunBase.Domain.Notifications;
using RunBase.Domain.Orders;
using RunBase.Domain.Plans;
using RunBase.Domain.Users;

namespace RunBase.Infrastructure.Persistence;

public sealed class RunBaseDbContext : DbContext
{
    public RunBaseDbContext(DbContextOptions<RunBaseDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<ClientRecord> Clients => Set<ClientRecord>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<NotificationCampaign> NotificationCampaigns => Set<NotificationCampaign>();

    public DbSet<SensitiveDataAuditEntry> SensitiveDataAuditEntries => Set<SensitiveDataAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureClients(modelBuilder);
        ConfigurePlans(modelBuilder);
        ConfigureOrders(modelBuilder);
        ConfigureNotificationCampaigns(modelBuilder);
        ConfigureSensitiveDataAuditEntries(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Name).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(254).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(user => user.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(user => user.CreatedAt).IsRequired();
            entity.Property(user => user.UpdatedAt).IsRequired();
        });
    }

    private static void ConfigureClients(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientRecord>(entity =>
        {
            entity.ToTable("clients");
            entity.HasKey(client => client.Id);
            entity.Property(client => client.Name).HasMaxLength(160).IsRequired();
            entity.Property(client => client.EmailCipherText).HasMaxLength(1024).IsRequired();
            entity.Property(client => client.EmailLookupHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(client => client.EmailLookupHash).IsUnique();
            entity.Property(client => client.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(client => client.PlanStage).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(client => client.DataSource).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(client => client.NextBillingAt);
            entity.Property(client => client.CreatedAt).IsRequired();
            entity.Property(client => client.UpdatedAt).IsRequired();
        });
    }

    private static void ConfigurePlans(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("plans");
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Name).HasMaxLength(120).IsRequired();
            entity.Property(plan => plan.Stage).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(plan => plan.Stage).IsUnique();
            entity.Property(plan => plan.Price).HasPrecision(18, 2).IsRequired();
            entity.Property(plan => plan.BillingCycle).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(plan => plan.IsActive).IsRequired();
            entity.Property(plan => plan.NextBillingAt);
            entity.Property(plan => plan.CreatedAt).IsRequired();
            entity.Property(plan => plan.UpdatedAt).IsRequired();
        });
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.ClientId).IsRequired();
            entity.HasIndex(order => order.ClientId);
            entity.Property(order => order.PlanStage).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(order => order.FinalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(order => order.CreatedAt).IsRequired();
            entity.Property(order => order.UpdatedAt).IsRequired();
        });
    }

    private static void ConfigureNotificationCampaigns(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationCampaign>(entity =>
        {
            entity.ToTable("notification_campaigns");
            entity.HasKey(campaign => campaign.Id);
            entity.Property(campaign => campaign.Name).HasMaxLength(120).IsRequired();
            entity.Property(campaign => campaign.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(campaign => campaign.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(campaign => campaign.TargetPlanStage).HasConversion<string>().HasMaxLength(32);
            entity.Property(campaign => campaign.ScheduledAt);
            entity.Property(campaign => campaign.CreatedAt).IsRequired();
            entity.Property(campaign => campaign.UpdatedAt).IsRequired();
        });
    }

    private static void ConfigureSensitiveDataAuditEntries(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensitiveDataAuditEntry>(entity =>
        {
            entity.ToTable("sensitive_data_audit_entries");
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.UserId);
            entity.Property(entry => entry.UserEmail).HasMaxLength(254);
            entity.Property(entry => entry.ResourceType).HasMaxLength(80).IsRequired();
            entity.Property(entry => entry.ResourceId).IsRequired();
            entity.Property(entry => entry.SensitiveField).HasMaxLength(80).IsRequired();
            entity.Property(entry => entry.Outcome).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(entry => entry.AttemptCount).IsRequired();
            entity.Property(entry => entry.CreatedAtUtc).IsRequired();
        });
    }
}
