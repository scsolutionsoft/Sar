using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortfolioPR.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalWorks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonalWorks",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Organizer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Achievement = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalWorks", x => x.Id);
                    table.CheckConstraint("CK_PersonalWork_Kind", "[Kind] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_PersonalWorks_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "portfolio",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonalWorkMedia",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonalWorkId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalWorkMedia", x => x.Id);
                    table.CheckConstraint("CK_PersonalWorkMedia_Source", "([FileId] IS NOT NULL AND [ExternalUrl] IS NULL) OR ([FileId] IS NULL AND [ExternalUrl] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PersonalWorkMedia_FileObjects_FileId",
                        column: x => x.FileId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonalWorkMedia_PersonalWorks_PersonalWorkId",
                        column: x => x.PersonalWorkId,
                        principalSchema: "portfolio",
                        principalTable: "PersonalWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalWorkMedia_FileId",
                schema: "portfolio",
                table: "PersonalWorkMedia",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalWorkMedia_PersonalWorkId",
                schema: "portfolio",
                table: "PersonalWorkMedia",
                column: "PersonalWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalWorks_OwnerId_Kind_WorkDate",
                schema: "portfolio",
                table: "PersonalWorks",
                columns: new[] { "OwnerId", "Kind", "WorkDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonalWorkMedia",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "PersonalWorks",
                schema: "portfolio");
        }
    }
}
