using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ValetaxTest.Events.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commission_payments",
                columns: table => new
                {
                    commission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commission_payments", x => x.commission_id);
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
                name: "profit_events",
                columns: table => new
                {
                    external_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    profit = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    schema_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profit_events", x => x.external_id);
                });

            migrationBuilder.CreateTable(
                name: "event_commissions",
                columns: table => new
                {
                    commission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    partner_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    rate_percent = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    schema_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_commissions", x => x.commission_id);
                    table.ForeignKey(
                        name: "fk_event_commissions_profit_events_event_id",
                        column: x => x.event_id,
                        principalTable: "profit_events",
                        principalColumn: "external_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_commission_payments_payout_id",
                table: "commission_payments",
                column: "payout_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_commissions_event_id",
                table: "event_commissions",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_inbox_messages_processed_at",
                table: "inbox_messages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "ix_profit_events_user_id_occurred_at",
                table: "profit_events",
                columns: new[] { "user_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commission_payments");

            migrationBuilder.DropTable(
                name: "event_commissions");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "profit_events");
        }
    }
}
