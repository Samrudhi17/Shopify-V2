using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QRShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AmountPaise = table.Column<int>(type: "int", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.PlanId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Payment_Transactions",
                columns: table => new
                {
                    PaymentTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    RazorpayOrderId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RazorpayPaymentId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AmountPaise = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubscriptionId = table.Column<int>(type: "int", nullable: true),
                    RawPayload = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment_Transactions", x => x.PaymentTransactionId);
                    table.ForeignKey(
                        name: "FK_Payment_Transactions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_Transactions_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "SubscriptionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Payment_Transactions_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "PlanId", "AmountPaise", "Code", "DisplayOrder", "DurationDays", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, 0, "trial", 1, 15, true, "Free Trial" },
                    { 2, 29900, "monthly", 2, 30, true, "Monthly" },
                    { 3, 119900, "half_yearly", 3, 182, true, "6 Months" },
                    { 4, 199900, "yearly", 4, 365, true, "12 Months" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Transactions_PlanId",
                table: "Payment_Transactions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Transactions_RazorpayOrderId",
                table: "Payment_Transactions",
                column: "RazorpayOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Transactions_SubscriptionId",
                table: "Payment_Transactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Transactions_VendorId",
                table: "Payment_Transactions",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Code",
                table: "Plans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_VendorId_EndsAt",
                table: "Subscriptions",
                columns: new[] { "VendorId", "EndsAt" });

            // Give every vendor who already existed the same 15-day trial a new
            // signup gets. Access is decided purely by whether a term is running,
            // so without this every existing vendor is locked out and their shop
            // goes offline the moment this migration lands — customers scanning a
            // printed QR code would hit a dead page with no warning.
            //
            // PlanId 1 is the trial row seeded above. The table is created by this
            // same migration, so there is nothing to collide with.
            migrationBuilder.Sql(@"
                INSERT INTO `Subscriptions` (`VendorId`, `PlanId`, `StartsAt`, `EndsAt`, `Status`, `CreatedAt`)
                SELECT v.`VendorId`, 1, UTC_TIMESTAMP(),
                       DATE_ADD(UTC_TIMESTAMP(), INTERVAL 15 DAY), 'Trialing', UTC_TIMESTAMP()
                FROM `Vendors` v;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payment_Transactions");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Plans");
        }
    }
}
