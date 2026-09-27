using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortfolioPR.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateStudentRecipient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                schema: "portfolio",
                table: "AwardCertificate",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AwardCertificate_StudentId",
                schema: "portfolio",
                table: "AwardCertificate",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AwardCertificate_Student_StudentId",
                schema: "portfolio",
                table: "AwardCertificate",
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
                name: "FK_AwardCertificate_Student_StudentId",
                schema: "portfolio",
                table: "AwardCertificate");

            migrationBuilder.DropIndex(
                name: "IX_AwardCertificate_StudentId",
                schema: "portfolio",
                table: "AwardCertificate");

            migrationBuilder.DropColumn(
                name: "StudentId",
                schema: "portfolio",
                table: "AwardCertificate");
        }
    }
}
