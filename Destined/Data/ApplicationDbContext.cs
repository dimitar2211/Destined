using Destined.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Destined.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<JournalPage> JournalPages { get; set; }
        public DbSet<TicketComment> TicketComments { get; set; }
        public DbSet<TicketReport> TicketReports { get; set; }
        public DbSet<UserFriendCode> UserFriendCodes { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Friendship> Friendships { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserFriendCode>()
                .HasOne(u => u.User)
                .WithOne()
                .HasForeignKey<UserFriendCode>(u => u.UserId)
                .IsRequired();

            builder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany()
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany()
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Friendship>()
                .HasOne(fs => fs.User)
                .WithMany()
                .HasForeignKey(fs => fs.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Friendship>()
                .HasOne(fs => fs.Friend)
                .WithMany()
                .HasForeignKey(fs => fs.FriendId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}