using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Portfolio.Backend.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    name_slug = table.Column<string>(type: "text", nullable: false, computedColumnSql: "lower(regexp_replace(full_name, E'[^a-zA-Z0-9_]+', '-', 'gi'))", stored: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    profile_image = table.Column<byte[]>(type: "bytea", nullable: true),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    password_reset_token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    password_reset_expiration = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_password_reset_request = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    expiration_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creation_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_token_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "description", "email", "full_name", "last_password_reset_request", "password_hash", "password_reset_expiration", "password_reset_token_hash", "profile_image" },
                values: new object[] { 1L, "Yes. It's me.", "me@jonathanbout.com", "Jonathan Bout", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new byte[0], new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new byte[0], null });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_owner_id",
                table: "refresh_token",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
