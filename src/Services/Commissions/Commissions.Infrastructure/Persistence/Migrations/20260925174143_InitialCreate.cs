using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ValetaxTest.Commissions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commission_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    schema_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commission_settings", x => x.id);
                    table.CheckConstraint("ck_commission_settings_singleton", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "commissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    source_user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    partner_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    rate_percent = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    schema_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accrued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reserved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commissions", x => x.id);
                });

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
                name: "payout_reservations",
                columns: table => new
                {
                    payout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    items = table.Column<string>(type: "jsonb", nullable: false),
                    reserved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    release_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payout_reservations", x => x.payout_id);
                });

            migrationBuilder.CreateTable(
                name: "processed_events",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    profit = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    schema_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    commissions_count = table.Column<int>(type: "integer", nullable: false),
                    commissions_total = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_events", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "schema_changes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    previous_schema_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    schema_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schema_changes", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "commission_settings",
                columns: new[] { "id", "schema_type", "updated_at" },
                values: new object[] { 1, "Linear", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "ix_commissions_accrued_by_partner",
                table: "commissions",
                columns: new[] { "partner_id", "accrued_at" },
                filter: "status = 'Accrued'");

            migrationBuilder.CreateIndex(
                name: "ix_commissions_event_id_level",
                table: "commissions",
                columns: new[] { "event_id", "level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commissions_payout_id",
                table: "commissions",
                column: "payout_id");

            migrationBuilder.CreateIndex(
                name: "ix_inbox_messages_processed_at",
                table: "inbox_messages",
                column: "processed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commission_settings");

            migrationBuilder.DropTable(
                name: "commissions");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "payout_reservations");

            migrationBuilder.DropTable(
                name: "processed_events");

            migrationBuilder.DropTable(
                name: "schema_changes");
        }
    }
}
