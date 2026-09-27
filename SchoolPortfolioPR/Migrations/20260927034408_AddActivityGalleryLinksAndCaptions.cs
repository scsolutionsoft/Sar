using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortfolioPR.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityGalleryLinksAndCaptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Caption",
                schema: "portfolio",
                table: "ActivityImage",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivityImageLinks",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityImageLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityImageLinks_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "portfolio",
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityImageLinks_ActivityId",
                schema: "portfolio",
                table: "ActivityImageLinks",
                column: "ActivityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityImageLinks",
                schema: "portfolio");

            migrationBuilder.DropColumn(
                name: "Caption",
                schema: "portfolio",
                table: "ActivityImage");
        }
    }
}
