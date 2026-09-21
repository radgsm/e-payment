using EPaymentMiddleware.Models;
using Microsoft.EntityFrameworkCore;

namespace EPaymentMiddleware.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        public DbSet<PaymentOrder> PaymentOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentOrder>(entity =>
            {
                entity.ToTable("PaymentOrders");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.OrderId);
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OrderId).HasMaxLength(100);
                entity.Property(e => e.ClientId).HasMaxLength(50);
                entity.Property(e => e.Lang).HasMaxLength(10);
                entity.Property(e => e.JsonParams).HasMaxLength(500);
                entity.Property(e => e.approvalCode).HasMaxLength(50);
                entity.Property(e => e.respCode).HasMaxLength(10);
                entity.Property(e => e.ErrorCode).HasMaxLength(10);
                entity.Property(e => e.actionCodeDescription).HasMaxLength(300);
            });
        }
    }
}
