using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Portfolio.Backend.Migrations
{
	/// <inheritdoc />
	public partial class RefineRefreshTokens : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "refresh_token_value",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
					referring_token_id = table.Column<long>(type: "bigint", nullable: false),
					creation_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
					expiration_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
					token_hash = table.Column<byte[]>(type: "bytea", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("pk_refresh_token_value", x => x.id);
					table.ForeignKey(
						name: "fk_refresh_token_value_refresh_token_referring_token_id",
						column: x => x.referring_token_id,
						principalTable: "refresh_token",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "ix_refresh_token_value_referring_token_id",
				table: "refresh_token_value",
				column: "referring_token_id");

			// copy the tokens to the new refresh_token_value table,
			// and update the referring token to point to the new value
			migrationBuilder.Sql("""
			INSERT INTO refresh_token_value (creation_date, expiration_date, referring_token_id, token_hash)
			    SELECT NOW(), expiration_date, id, token_hash FROM refresh_token
			""");

			migrationBuilder.DropColumn(
				name: "creation_date",
				table: "refresh_token");

			migrationBuilder.DropColumn(
				name: "expiration_date",
				table: "refresh_token");

			migrationBuilder.DropColumn(
				name: "last_updated_date",
				table: "refresh_token");

			migrationBuilder.DropColumn(
				name: "token_hash",
				table: "refresh_token");

		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<DateTimeOffset>(
				name: "creation_date",
				table: "refresh_token",
				type: "timestamp with time zone",
				nullable: false,
				defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

			migrationBuilder.AddColumn<DateTimeOffset>(
				name: "expiration_date",
				table: "refresh_token",
				type: "timestamp with time zone",
				nullable: false,
				defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

			migrationBuilder.AddColumn<DateTimeOffset>(
				name: "last_updated_date",
				table: "refresh_token",
				type: "timestamp with time zone",
				nullable: false,
				defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

			migrationBuilder.AddColumn<byte[]>(
				name: "token_hash",
				table: "refresh_token",
				type: "bytea",
				nullable: false,
				defaultValue: Array.Empty<byte>());

			// copy the values back to the refresh_token table from the newest refresh_token_value
			// linked to the referring token
			migrationBuilder.Sql("""
                UPDATE refresh_token
                SET token_hash = sub.token_hash,
                    creation_date = sub.creation_date,
                    expiration_date = sub.expiration_date,
                    last_updated_date = sub.creation_date
                FROM (
                    SELECT DISTINCT ON (referring_token_id) *
                    FROM refresh_token_value
                    ORDER BY referring_token_id, creation_date DESC
                ) AS sub
                WHERE sub.referring_token_id = refresh_token.id;
                """);

			migrationBuilder.DropTable(
				name: "refresh_token_value");

		}
	}
}
