using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionClubs.Infrastructure.SqliteDbContext.Migrations
{
    /// <inheritdoc />
    public partial class addName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_Clubs_ClubId",
                table: "Members");

            migrationBuilder.AlterColumn<int>(
                name: "ClubId",
                table: "Members",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Members",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Adhesions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Adhesions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Clubs_ClubId",
                table: "Members",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_Clubs_ClubId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Adhesions");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Adhesions");

            migrationBuilder.AlterColumn<int>(
                name: "ClubId",
                table: "Members",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Clubs_ClubId",
                table: "Members",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id");
        }
    }
}
