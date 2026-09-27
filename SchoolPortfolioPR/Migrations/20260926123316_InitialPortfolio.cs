using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortfolioPR.Migrations
{
    /// <inheritdoc />
    public partial class InitialPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "portfolio");

            migrationBuilder.CreateTable(
                name: "AcademicYear",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYear", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityCategory",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    User = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Entity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileObjects",
                schema: "portfolio",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileObjects", x => x.FileId);
                });

            migrationBuilder.CreateTable(
                name: "Publications",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    DirectorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publications", x => x.Id);
                    table.CheckConstraint("CK_Publication_Month", "[Month] BETWEEN 1 AND 12");
                });

            migrationBuilder.CreateTable(
                name: "SchoolProfile",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DirectorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Contact = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolProfile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubjectGroup",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tag", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "portfolio",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "portfolio",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Student",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Classroom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Student_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "portfolio",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicationFile",
                schema: "portfolio",
                columns: table => new
                {
                    MonthlyPublicationId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationFile", x => new { x.MonthlyPublicationId, x.FileId });
                    table.ForeignKey(
                        name: "FK_PublicationFile_FileObjects_FileId",
                        column: x => x.FileId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicationFile_Publications_MonthlyPublicationId",
                        column: x => x.MonthlyPublicationId,
                        principalSchema: "portfolio",
                        principalTable: "Publications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActivityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Organizer = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    SubjectGroupId = table.Column<int>(type: "int", nullable: true),
                    AcademicYearId = table.Column<int>(type: "int", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    CoverImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsProject = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_AcademicYear_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalSchema: "portfolio",
                        principalTable: "AcademicYear",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activities_ActivityCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "portfolio",
                        principalTable: "ActivityCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activities_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "portfolio",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activities_FileObjects_CoverImageId",
                        column: x => x.CoverImageId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activities_SubjectGroup_SubjectGroupId",
                        column: x => x.SubjectGroupId,
                        principalSchema: "portfolio",
                        principalTable: "SubjectGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "portfolio",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "portfolio",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "portfolio",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "portfolio",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "portfolio",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "portfolio",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "portfolio",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "portfolio",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teacher",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    SubjectGroupId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teacher", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teacher_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "portfolio",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teacher_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "portfolio",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teacher_SubjectGroup_SubjectGroupId",
                        column: x => x.SubjectGroupId,
                        principalSchema: "portfolio",
                        principalTable: "SubjectGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityFile",
                schema: "portfolio",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityFile", x => new { x.ActivityId, x.FileId });
                    table.ForeignKey(
                        name: "FK_ActivityFile_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "portfolio",
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityFile_FileObjects_FileId",
                        column: x => x.FileId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityImage",
                schema: "portfolio",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityImage", x => new { x.ActivityId, x.FileId });
                    table.ForeignKey(
                        name: "FK_ActivityImage_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "portfolio",
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityImage_FileObjects_FileId",
                        column: x => x.FileId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityStudent",
                schema: "portfolio",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityStudent", x => new { x.ActivityId, x.StudentId });
                    table.ForeignKey(
                        name: "FK_ActivityStudent_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "portfolio",
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityStudent_Student_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "portfolio",
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTag",
                schema: "portfolio",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTag", x => new { x.ActivityId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ActivityTag_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "portfolio",
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityTag_Tag_TagId",
                        column: x => x.TagId,
                        principalSchema: "portfolio",
                        principalTable: "Tag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Awards",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AwardName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CompetitionName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AwardLevel = table.Column<int>(type: "int", nullable: false),
                    AwardRank = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Organizer = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AwardDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcademicYearId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    SubjectGroupId = table.Column<int>(type: "int", nullable: true),
                    ActivityId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Awards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Awards_AcademicYear_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalSchema: "portfolio",
                        principalTable: "AcademicYear",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Awards_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "portfolio",
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Awards_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "portfolio",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Awards_SubjectGroup_SubjectGroupId",
                        column: x => x.SubjectGroupId,
                        principalSchema: "portfolio",
                        principalTable: "SubjectGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTeacher",
                schema: "portfolio",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTeacher", x => new { x.ActivityId, x.TeacherId });
                    table.ForeignKey(
                        name: "FK_ActivityTeacher_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "portfolio",
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityTeacher_Teacher_TeacherId",
                        column: x => x.TeacherId,
                        principalSchema: "portfolio",
                        principalTable: "Teacher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AwardCertificate",
                schema: "portfolio",
                columns: table => new
                {
                    AwardId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardCertificate", x => new { x.AwardId, x.FileId });
                    table.ForeignKey(
                        name: "FK_AwardCertificate_Awards_AwardId",
                        column: x => x.AwardId,
                        principalSchema: "portfolio",
                        principalTable: "Awards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardCertificate_FileObjects_FileId",
                        column: x => x.FileId,
                        principalSchema: "portfolio",
                        principalTable: "FileObjects",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AwardStudent",
                schema: "portfolio",
                columns: table => new
                {
                    AwardId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardStudent", x => new { x.AwardId, x.StudentId });
                    table.ForeignKey(
                        name: "FK_AwardStudent_Awards_AwardId",
                        column: x => x.AwardId,
                        principalSchema: "portfolio",
                        principalTable: "Awards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardStudent_Student_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "portfolio",
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AwardTeacher",
                schema: "portfolio",
                columns: table => new
                {
                    AwardId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardTeacher", x => new { x.AwardId, x.TeacherId });
                    table.ForeignKey(
                        name: "FK_AwardTeacher_Awards_AwardId",
                        column: x => x.AwardId,
                        principalSchema: "portfolio",
                        principalTable: "Awards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardTeacher_Teacher_TeacherId",
                        column: x => x.TeacherId,
                        principalSchema: "portfolio",
                        principalTable: "Teacher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicationItem",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyPublicationId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: true),
                    AwardId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationItem", x => x.Id);
                    table.CheckConstraint("CK_PublicationItem_Source", "([ActivityId] IS NOT NULL AND [AwardId] IS NULL) OR ([ActivityId] IS NULL AND [AwardId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PublicationItem_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "portfolio",
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicationItem_Awards_AwardId",
                        column: x => x.AwardId,
                        principalSchema: "portfolio",
                        principalTable: "Awards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicationItem_Publications_MonthlyPublicationId",
                        column: x => x.MonthlyPublicationId,
                        principalSchema: "portfolio",
                        principalTable: "Publications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AcademicYearId",
                schema: "portfolio",
                table: "Activities",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ActivityCode",
                schema: "portfolio",
                table: "Activities",
                column: "ActivityCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ActivityDate",
                schema: "portfolio",
                table: "Activities",
                column: "ActivityDate");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CategoryId",
                schema: "portfolio",
                table: "Activities",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CoverImageId",
                schema: "portfolio",
                table: "Activities",
                column: "CoverImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_DepartmentId",
                schema: "portfolio",
                table: "Activities",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_PublishedDate",
                schema: "portfolio",
                table: "Activities",
                column: "PublishedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Slug",
                schema: "portfolio",
                table: "Activities",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Status_IsPublic_ActivityDate",
                schema: "portfolio",
                table: "Activities",
                columns: new[] { "Status", "IsPublic", "ActivityDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_SubjectGroupId",
                schema: "portfolio",
                table: "Activities",
                column: "SubjectGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityFile_FileId",
                schema: "portfolio",
                table: "ActivityFile",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityImage_FileId",
                schema: "portfolio",
                table: "ActivityImage",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityStudent_StudentId",
                schema: "portfolio",
                table: "ActivityStudent",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTag_TagId",
                schema: "portfolio",
                table: "ActivityTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTeacher_TeacherId",
                schema: "portfolio",
                table: "ActivityTeacher",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "portfolio",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "portfolio",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "portfolio",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "portfolio",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "portfolio",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "portfolio",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DepartmentId",
                schema: "portfolio",
                table: "AspNetUsers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "portfolio",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                schema: "portfolio",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AwardCertificate_FileId",
                schema: "portfolio",
                table: "AwardCertificate",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Awards_AcademicYearId",
                schema: "portfolio",
                table: "Awards",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_Awards_ActivityId",
                schema: "portfolio",
                table: "Awards",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Awards_AwardDate",
                schema: "portfolio",
                table: "Awards",
                column: "AwardDate");

            migrationBuilder.CreateIndex(
                name: "IX_Awards_AwardLevel",
                schema: "portfolio",
                table: "Awards",
                column: "AwardLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Awards_DepartmentId",
                schema: "portfolio",
                table: "Awards",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Awards_PublishedDate",
                schema: "portfolio",
                table: "Awards",
                column: "PublishedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Awards_SubjectGroupId",
                schema: "portfolio",
                table: "Awards",
                column: "SubjectGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardStudent_StudentId",
                schema: "portfolio",
                table: "AwardStudent",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardTeacher_TeacherId",
                schema: "portfolio",
                table: "AwardTeacher",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_FileObjects_FileHash",
                schema: "portfolio",
                table: "FileObjects",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationFile_FileId",
                schema: "portfolio",
                table: "PublicationFile",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationItem_ActivityId",
                schema: "portfolio",
                table: "PublicationItem",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationItem_AwardId",
                schema: "portfolio",
                table: "PublicationItem",
                column: "AwardId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationItem_MonthlyPublicationId",
                schema: "portfolio",
                table: "PublicationItem",
                column: "MonthlyPublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Publications_Year_Month",
                schema: "portfolio",
                table: "Publications",
                columns: new[] { "Year", "Month" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Code",
                schema: "portfolio",
                table: "Student",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Student_DepartmentId",
                schema: "portfolio",
                table: "Student",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Teacher_Code",
                schema: "portfolio",
                table: "Teacher",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Teacher_DepartmentId",
                schema: "portfolio",
                table: "Teacher",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Teacher_SubjectGroupId",
                schema: "portfolio",
                table: "Teacher",
                column: "SubjectGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Teacher_UserId",
                schema: "portfolio",
                table: "Teacher",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityFile",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "ActivityImage",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "ActivityStudent",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "ActivityTag",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "ActivityTeacher",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AwardCertificate",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AwardStudent",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AwardTeacher",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "PublicationFile",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "PublicationItem",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "SchoolProfile",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "Tag",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "Student",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "Teacher",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "Awards",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "Publications",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "Activities",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "AcademicYear",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "ActivityCategory",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "Department",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "FileObjects",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "SubjectGroup",
                schema: "portfolio");
        }
    }
}
