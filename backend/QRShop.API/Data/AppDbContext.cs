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
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

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
        b.Entity<Plan>().ToTable("Plans");
        b.Entity<Subscription>().ToTable("Subscriptions");
        b.Entity<PaymentTransaction>().ToTable("Payment_Transactions");

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

        // Avoid multiple cascade delete paths to the same table.
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

        // --- Billing ---
        b.Entity<Plan>().HasIndex(p => p.Code).IsUnique();

        // Every access check reads "this vendor's latest term", so index the way
        // that query sorts.
        b.Entity<Subscription>().HasIndex(s => new { s.VendorId, s.EndsAt });

        // Razorpay retries webhooks and the browser callback can race them. The
        // unique order id is what makes settling a payment idempotent.
        b.Entity<PaymentTransaction>().HasIndex(t => t.RazorpayOrderId).IsUnique();

        // Deleting a plan that a vendor has paid for would erase what they
        // bought, so the FK blocks it.
        b.Entity<Subscription>()
            .HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<PaymentTransaction>()
            .HasOne(t => t.Plan)
            .WithMany()
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<PaymentTransaction>()
            .HasOne(t => t.Subscription)
            .WithMany()
            .HasForeignKey(t => t.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Prices in paise. The trial is a real plan row so a trial term is the
        // same shape as a paid one and needs no special case when checking access.
        b.Entity<Plan>().HasData(
            new Plan { PlanId = 1, Code = PlanCodes.Trial, Name = "Free Trial", AmountPaise = 0, DurationDays = 15, DisplayOrder = 1 },
            new Plan { PlanId = 2, Code = PlanCodes.Monthly, Name = "Monthly", AmountPaise = 29_900, DurationDays = 30, DisplayOrder = 2 },
            new Plan { PlanId = 3, Code = PlanCodes.HalfYearly, Name = "6 Months", AmountPaise = 119_900, DurationDays = 182, DisplayOrder = 3 },
            new Plan { PlanId = 4, Code = PlanCodes.Yearly, Name = "12 Months", AmountPaise = 199_900, DurationDays = 365, DisplayOrder = 4 });
    }
}

public static class PlanCodes
{
    public const string Trial = "trial";
    public const string Monthly = "monthly";
    public const string HalfYearly = "half_yearly";
    public const string Yearly = "yearly";
}
