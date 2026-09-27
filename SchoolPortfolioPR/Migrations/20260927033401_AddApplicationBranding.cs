using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortfolioPR.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branding",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ProgramName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogoFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branding", x => x.Id);
                    table.CheckConstraint("CK_Branding_Singleton", "[Id] = 1");
                    table.ForeignKey(
                        name: "FK_Branding_FileObjects_LogoFileId",
                        column: x => x.LogoFileId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "portfolio",
                table: "Branding",
                columns: new[] { "Id", "LogoFileId", "ProgramName" },
                values: new object[] { 1, null, "SchoolPortfolioPR" });

            migrationBuilder.CreateIndex(
                name: "IX_Branding_LogoFileId",
                schema: "portfolio",
                table: "Branding",
                column: "LogoFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Branding",
                schema: "portfolio");
        }
    }
}
