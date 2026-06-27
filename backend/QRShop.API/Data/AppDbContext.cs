using Microsoft.EntityFrameworkCore;
using QRShop.API.Models.Entities;

namespace QRShop.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<QrCode> QrCodes => Set<QrCode>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<StockHistory> StockHistory => Set<StockHistory>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Table names to match the ER diagram.
        b.Entity<Admin>().ToTable("Admins");
        b.Entity<Vendor>().ToTable("Vendors");
        b.Entity<Shop>().ToTable("Shops");
        b.Entity<QrCode>().ToTable("QR_Codes");
        b.Entity<Category>().ToTable("Categories");
        b.Entity<ProductCategory>().ToTable("Product_Categories");
        b.Entity<Product>().ToTable("Products");
        b.Entity<ProductImage>().ToTable("Product_Images");
        b.Entity<ProductVariant>().ToTable("Product_Variants");
        b.Entity<Inventory>().ToTable("Inventory");
        b.Entity<StockHistory>().ToTable("Stock_History");

        // Unique constraints.
        b.Entity<Vendor>().HasIndex(v => v.Email).IsUnique();
        b.Entity<Admin>().HasIndex(a => a.Email).IsUnique();
        b.Entity<Shop>().HasIndex(s => s.ShopName).IsUnique();
        b.Entity<Shop>().HasIndex(s => s.Slug).IsUnique();

        // One-to-one relationships.
        b.Entity<Shop>()
            .HasOne(s => s.QrCode)
            .WithOne(q => q.Shop)
            .HasForeignKey<QrCode>(q => q.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ProductVariant>()
            .HasOne(v => v.Inventory)
            .WithOne(i => i.Variant)
            .HasForeignKey<Inventory>(i => i.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Avoid multiple cascade paths on SQL Server.
        b.Entity<Product>()
            .HasOne(p => p.ProductCategory)
            .WithMany(pc => pc.Products)
            .HasForeignKey(p => p.ProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
