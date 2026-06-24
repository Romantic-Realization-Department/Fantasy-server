using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "player");

            migrationBuilder.CreateTable(
                name: "player",
                schema: "player",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    JobType = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    MaxStage = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    LastWeaponId = table.Column<int>(type: "integer", nullable: true),
                    ActiveSkills = table.Column<int[]>(type: "integer[]", nullable: false, defaultValueSql: "ARRAY[]::integer[]"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "player_resource",
                schema: "player",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    Gold = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Exp = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    EnhancementScroll = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Mithril = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Sp = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_resource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_resource_player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "player",
                        principalTable: "player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_skill",
                schema: "player",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    IsUnlocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_skill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_skill_player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "player",
                        principalTable: "player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_weapon",
                schema: "player",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    WeaponId = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    EnhancementLevel = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    AwakeningCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_weapon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_weapon_player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "player",
                        principalTable: "player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_AccountId_JobType",
                schema: "player",
                table: "player",
                columns: new[] { "AccountId", "JobType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_resource_PlayerId",
                schema: "player",
                table: "player_resource",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_skill_PlayerId_SkillId",
                schema: "player",
                table: "player_skill",
                columns: new[] { "PlayerId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_weapon_PlayerId_WeaponId",
                schema: "player",
                table: "player_weapon",
                columns: new[] { "PlayerId", "WeaponId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_resource",
                schema: "player");

            migrationBuilder.DropTable(
                name: "player_skill",
                schema: "player");

            migrationBuilder.DropTable(
                name: "player_weapon",
                schema: "player");

            migrationBuilder.DropTable(
                name: "player",
                schema: "player");
        }
    }
}
