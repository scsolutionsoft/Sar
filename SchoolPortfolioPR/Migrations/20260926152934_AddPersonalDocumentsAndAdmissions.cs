using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortfolioPR.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalDocumentsAndAdmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                schema: "portfolio",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdmissionPortfolios",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    School = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StudyPlan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GraduationYear = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gpax = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetUniversity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetFaculty = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AdmissionRound = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequirementsUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    Motivation = table.Column<string>(type: "nvarchar(2200)", maxLength: 2200, nullable: false),
                    Education = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    Skills = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    Goals = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Theme = table.Column<int>(type: "int", nullable: false),
                    IncludeContents = table.Column<bool>(type: "bit", nullable: false),
                    IncludeClosing = table.Column<bool>(type: "bit", nullable: false),
                    IncludeEvidence = table.Column<bool>(type: "bit", nullable: false),
                    MaxPages = table.Column<int>(type: "int", nullable: false),
                    MaxMegabytes = table.Column<int>(type: "int", nullable: false),
                    PortraitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PdfFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExportedPages = table.Column<int>(type: "int", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionPortfolios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionPortfolios_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "portfolio",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionPortfolios_FileObjects_PdfFileId",
                        column: x => x.PdfFileId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionPortfolios_FileObjects_PortraitId",
                        column: x => x.PortraitId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonalDocuments",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Pages = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalDocuments_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "portfolio",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonalDocuments_FileObjects_FileId",
                        column: x => x.FileId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioDocument",
                schema: "portfolio",
                columns: table => new
                {
                    AdmissionPortfolioId = table.Column<int>(type: "int", nullable: false),
                    PersonalDocumentId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioDocument", x => new { x.AdmissionPortfolioId, x.PersonalDocumentId });
                    table.ForeignKey(
                        name: "FK_PortfolioDocument_AdmissionPortfolios_AdmissionPortfolioId",
                        column: x => x.AdmissionPortfolioId,
                        principalSchema: "portfolio",
                        principalTable: "AdmissionPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PortfolioDocument_PersonalDocuments_PersonalDocumentId",
                        column: x => x.PersonalDocumentId,
                        principalSchema: "portfolio",
                        principalTable: "PersonalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioEntry",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdmissionPortfolioId = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Reflection = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PersonalDocumentId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioEntry_AdmissionPortfolios_AdmissionPortfolioId",
                        column: x => x.AdmissionPortfolioId,
                        principalSchema: "portfolio",
                        principalTable: "AdmissionPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PortfolioEntry_FileObjects_ImageId",
                        column: x => x.ImageId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PortfolioEntry_PersonalDocuments_PersonalDocumentId",
                        column: x => x.PersonalDocumentId,
                        principalSchema: "portfolio",
                        principalTable: "PersonalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_StudentId",
                schema: "portfolio",
                table: "AspNetUsers",
                column: "StudentId",
                unique: true,
                filter: "[StudentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionPortfolios_OwnerId",
                schema: "portfolio",
                table: "AdmissionPortfolios",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionPortfolios_PdfFileId",
                schema: "portfolio",
                table: "AdmissionPortfolios",
                column: "PdfFileId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionPortfolios_PortraitId",
                schema: "portfolio",
                table: "AdmissionPortfolios",
                column: "PortraitId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalDocuments_FileId",
                schema: "portfolio",
                table: "PersonalDocuments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalDocuments_OwnerId_DocumentDate",
                schema: "portfolio",
                table: "PersonalDocuments",
                columns: new[] { "OwnerId", "DocumentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioDocument_PersonalDocumentId",
                schema: "portfolio",
                table: "PortfolioDocument",
                column: "PersonalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioEntry_AdmissionPortfolioId",
                schema: "portfolio",
                table: "PortfolioEntry",
                column: "AdmissionPortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioEntry_ImageId",
                schema: "portfolio",
                table: "PortfolioEntry",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioEntry_PersonalDocumentId",
                schema: "portfolio",
                table: "PortfolioEntry",
                column: "PersonalDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Student_StudentId",
                schema: "portfolio",
                table: "AspNetUsers",
                column: "StudentId",
                principalSchema: "portfolio",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Student_StudentId",
                schema: "portfolio",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "PortfolioDocument",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "PortfolioEntry",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AdmissionPortfolios",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "PersonalDocuments",
                schema: "portfolio");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_StudentId",
                schema: "portfolio",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StudentId",
                schema: "portfolio",
                table: "AspNetUsers");
        }
    }
}
