using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ValetaxTest.Payouts.Infrastructure.Persistence.Migrations
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
                name: "payout_sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    items = table.Column<string>(type: "jsonb", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    step_attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    history = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payout_sagas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inbox_messages_processed_at",
                table: "inbox_messages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "ix_payout_sagas_next_attempt",
                table: "payout_sagas",
                column: "next_attempt_at",
                filter: "state IN ('Reserving', 'Crediting', 'Confirming', 'Compensating')");

            migrationBuilder.CreateIndex(
                name: "ix_payout_sagas_partner_id_created_at",
                table: "payout_sagas",
                columns: new[] { "partner_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_payout_sagas_active_partner",
                table: "payout_sagas",
                column: "partner_id",
                unique: true,
                filter: "state IN ('Reserving', 'Crediting', 'Confirming', 'Compensating')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "payout_sagas");
        }
    }
}
