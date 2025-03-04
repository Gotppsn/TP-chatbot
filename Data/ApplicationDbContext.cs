using AIHelpdeskSupport.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdeskSupport.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Chatbot> Chatbots { get; set; } = null!;
        public DbSet<KnowledgeBase> KnowledgeBases { get; set; } = null!;
        public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; } = null!;
        public DbSet<ChatSession> ChatSessions { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

        public DbSet<UserPermission> UserPermissions { get; set; }


        // Add additional DbSet for relationship
        public DbSet<ChatbotKnowledgeBase> ChatbotKnowledgeBases { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure many-to-many relationship
            builder.Entity<ChatbotKnowledgeBase>()
                .HasKey(ck => new { ck.ChatbotId, ck.KnowledgeBaseId });

            builder.Entity<ChatbotKnowledgeBase>()
                .HasOne(ck => ck.Chatbot)
                .WithMany(c => c.KnowledgeBases)
                .HasForeignKey(ck => ck.ChatbotId);

            builder.Entity<ChatbotKnowledgeBase>()
                .HasOne(ck => ck.KnowledgeBase)
                .WithMany(k => k.Chatbots)
                .HasForeignKey(ck => ck.KnowledgeBaseId);

            builder.Entity<UserPermission>()
                .HasKey(up => new { up.UserId, up.PermissionName });

            builder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany()
                .HasForeignKey(up => up.UserId);
        }
    }
}