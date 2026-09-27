using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortfolioPR.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Announcements",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 30000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    Tone = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                    table.CheckConstraint("CK_Announcement_Dates", "[EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_Announcement_Size", "[Size] BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_Announcement_Tone", "[Tone] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_Announcements_FileObjects_ImageId",
                        column: x => x.ImageId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_ImageId",
                schema: "portfolio",
                table: "Announcements",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_IsEnabled_StartDate_EndDate",
                schema: "portfolio",
                table: "Announcements",
                columns: new[] { "IsEnabled", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_ReferenceCode",
                schema: "portfolio",
                table: "Announcements",
                column: "ReferenceCode",
                unique: true,
                filter: "[ReferenceCode] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements",
                schema: "portfolio");
        }
    }
}
