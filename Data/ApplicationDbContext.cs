// Data/ApplicationDbContext.cs
using AIHelpdeskSupport.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdeskSupport.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) {}
    
    public DbSet<Chatbot> Chatbots { get; set; } = null!;
    public DbSet<KnowledgeBase> KnowledgeBases { get; set; } = null!;
public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; } = null!;
}