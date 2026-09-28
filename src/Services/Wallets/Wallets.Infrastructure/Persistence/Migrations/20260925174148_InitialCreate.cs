using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ValetaxTest.Wallets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_messages", x => new { x.message_id, x.consumer });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    trace_context = table.Column<string>(type: "jsonb", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wallet_payouts",
                columns: table => new
                {
                    payout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_payouts", x => x.payout_id);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    balance = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallets", x => x.user_id);
                    table.CheckConstraint("ck_wallets_balance_not_negative", "balance >= 0");
                });

            migrationBuilder.CreateTable(
                name: "wallet_payout_items",
                columns: table => new
                {
                    commission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    schema_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_payout_items", x => x.commission_id);
                    table.ForeignKey(
                        name: "fk_wallet_payout_items_wallet_payouts_payout_id",
                        column: x => x.payout_id,
                        principalTable: "wallet_payouts",
                        principalColumn: "payout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inbox_messages_processed_at",
                table: "inbox_messages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_payout_items_payout_id",
                table: "wallet_payout_items",
                column: "payout_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_payouts_user_id_processed_at",
                table: "wallet_payouts",
                columns: new[] { "user_id", "processed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "wallet_payout_items");

            migrationBuilder.DropTable(
                name: "wallets");

            migrationBuilder.DropTable(
                name: "wallet_payouts");
        }
    }
}
