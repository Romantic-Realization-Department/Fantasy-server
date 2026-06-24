using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fantasy.Server.Migrations
{
    /// <inheritdoc />
    public partial class NormalizePlayerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // player 테이블에서 정규화 대상 컬럼 제거
            migrationBuilder.DropColumn(
                name: "ActiveSkills",
                schema: "player",
                table: "player");

            migrationBuilder.DropColumn(
                name: "LastWeaponId",
                schema: "player",
                table: "player");

            migrationBuilder.DropColumn(
                name: "MaxStage",
                schema: "player",
                table: "player");

            // player 테이블에 Exp 추가
            migrationBuilder.AddColumn<long>(
                name: "Exp",
                schema: "player",
                table: "player",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // player_resource 테이블에서 Exp 제거
            migrationBuilder.DropColumn(
                name: "Exp",
                schema: "player",
                table: "player_resource");

            // player_stage 테이블 생성
            migrationBuilder.CreateTable(
                name: "player_stage",
                schema: "player",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    MaxStage = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_stage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_stage_player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "player",
                        principalTable: "player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_stage_PlayerId",
                schema: "player",
                table: "player_stage",
                column: "PlayerId",
                unique: true);

            // player_session 테이블 생성
            migrationBuilder.CreateTable(
                name: "player_session",
                schema: "player",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    LastWeaponId = table.Column<int>(type: "integer", nullable: true),
                    ActiveSkills = table.Column<int[]>(type: "integer[]", nullable: false, defaultValueSql: "ARRAY[]::integer[]"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_session", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_session_player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "player",
                        principalTable: "player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_session_PlayerId",
                schema: "player",
                table: "player_session",
                column: "PlayerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_session",
                schema: "player");

            migrationBuilder.DropTable(
                name: "player_stage",
                schema: "player");

            migrationBuilder.DropColumn(
                name: "Exp",
                schema: "player",
                table: "player");

            migrationBuilder.AddColumn<long>(
                name: "Exp",
                schema: "player",
                table: "player_resource",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "MaxStage",
                schema: "player",
                table: "player",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<int>(
                name: "LastWeaponId",
                schema: "player",
                table: "player",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int[]>(
                name: "ActiveSkills",
                schema: "player",
                table: "player",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::integer[]");
        }
    }
}
