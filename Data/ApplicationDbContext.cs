using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Models;

namespace Pharmacy.Data;

// IdentityDbContext<ApplicationUser> уже содержит таблицы Identity:
// AspNetUsers, AspNetRoles, AspNetUserRoles и др.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Каждый DbSet = одна таблица в базе
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Обязательно: настраивает таблицы Identity
        base.OnModelCreating(builder);

        // ---------- Category ----------
        builder.Entity<Category>(entity =>
        {
            // Названия категорий не должны повторяться
            entity.HasIndex(c => c.Name).IsUnique();
        });

        // ---------- Medicine ----------
        builder.Entity<Medicine>(entity =>
        {
            // Штрихкод уникален
            entity.HasIndex(m => m.Barcode).IsUnique();

            // Category 1 -> много Medicines
            entity.HasOne(m => m.Category)
                  .WithMany(c => c.Medicines)
                  .HasForeignKey(m => m.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Manufacturer 1 -> много Medicines
            entity.HasOne(m => m.Manufacturer)
                  .WithMany(x => x.Medicines)
                  .HasForeignKey(m => m.ManufacturerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Supplier 1 -> много Medicines
            entity.HasOne(m => m.Supplier)
                  .WithMany(s => s.Medicines)
                  .HasForeignKey(m => m.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Sale ----------
        builder.Entity<Sale>(entity =>
        {
            // User 1 -> много Sales
            entity.HasOne(s => s.User)
                  .WithMany(u => u.Sales)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- SaleItem ----------
        builder.Entity<SaleItem>(entity =>
        {
            // Sale 1 -> много SaleItems.
            // Cascade: при удалении продажи удаляется и её состав
            entity.HasOne(i => i.Sale)
                  .WithMany(s => s.Items)
                  .HasForeignKey(i => i.SaleId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Medicine 1 -> много SaleItems
            entity.HasOne(i => i.Medicine)
                  .WithMany(m => m.SaleItems)
                  .HasForeignKey(i => i.MedicineId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}