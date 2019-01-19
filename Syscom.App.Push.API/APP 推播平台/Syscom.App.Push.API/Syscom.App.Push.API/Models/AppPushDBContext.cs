using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Syscom.App.Push.API.Models
{
    public class AppPushDBContext : DbContext
    {

        public virtual DbSet<ApiChannels> ApiChannels { get; set; }
        public virtual DbSet<Apis> Apis { get; set; }
        public virtual DbSet<Channels> Channels { get; set; }
        public virtual DbSet<Histories> Histories { get; set; }
        public virtual DbSet<HistoryDetails> HistoryDetails { get; set; }
        public virtual DbSet<Logs> Logs { get; set; }
        public virtual DbSet<Subscribers> Subscribers { get; set; }

        public virtual DbSet<Tokens> Tokens { get; set; }

        //https://docs.microsoft.com/en-us/ef/core/miscellaneous/logging
        //public static readonly Microsoft.Extensions.Logging.LoggerFactory _myLoggerFactory = new LoggerFactory(new[] {
        //    //new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider(),

        //    new ConsoleLoggerProvider((category, level)
        //    => category == DbLoggerCategory.Database.Command.Name
        //                              && level == LogLevel.Information, true)


        //});
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var factory = new LoggerFactory().AddDebug().AddConsole();

            optionsBuilder.UseLoggerFactory(factory);
        }

        public AppPushDBContext(DbContextOptions<AppPushDBContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApiChannels>(entity =>
            {
                entity.HasKey(e => new { e.ApiId, e.ChannelId });

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DeletedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Apis>(entity =>
            {
                entity.Property(e => e.Id)
                      .HasColumnName("ID")
                      .UseSqlServerIdentityColumn();

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DeletedAt).HasColumnType("datetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.SecretKey)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Uuid)
                    .HasColumnName("UUID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.Webhook)
                    .IsRequired()
                    .HasMaxLength(512);
            });

            modelBuilder.Entity<Channels>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID").UseSqlServerIdentityColumn();

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DeletedAt).HasColumnType("datetime");

                entity.Property(e => e.ExtraUrlA).HasMaxLength(512);

                entity.Property(e => e.ExtraUrlB).HasMaxLength(512);

                entity.Property(e => e.ForeignId).HasMaxLength(255);

                entity.Property(e => e.ForeignWebhook).HasMaxLength(255);

                entity.Property(e => e.Image)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Name).HasMaxLength(50);

                entity.Property(e => e.SecretKey).HasMaxLength(255);

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
            });


            modelBuilder.Entity<Histories>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID").UseSqlServerIdentityColumn();

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.FinishedAt).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(50);

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<HistoryDetails>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID").UseSqlServerIdentityColumn();

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.FinishedAt).HasColumnType("datetime");

                entity.Property(e => e.Message).HasMaxLength(512);

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
            });


            modelBuilder.Entity<Logs>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedNever();

                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ServerIp)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.ServerName)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Subscribers>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DeletedAt).HasColumnType("datetime");

                entity.Property(e => e.DeviceToken).HasMaxLength(512);

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Tokens>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Account)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Token).HasMaxLength(512);

                entity.Property(e => e.CreatedAt)
                                    .HasColumnType("datetime")
                                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.TimeOutAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
            });
        }
    }
}
