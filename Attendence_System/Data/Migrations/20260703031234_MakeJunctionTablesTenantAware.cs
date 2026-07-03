using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendence_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeJunctionTablesTenantAware : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentLectures_Tenants_TenantId",
                table: "StudentLectures");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "StudentExams",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "LectureGrades",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "CourseGrades",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExams_TenantId",
                table: "StudentExams",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureGrades_TenantId",
                table: "LectureGrades",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrades_TenantId",
                table: "CourseGrades",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseGrades_Tenants_TenantId",
                table: "CourseGrades",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LectureGrades_Tenants_TenantId",
                table: "LectureGrades",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentExams_Tenants_TenantId",
                table: "StudentExams",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentLectures_Tenants_TenantId",
                table: "StudentLectures",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseGrades_Tenants_TenantId",
                table: "CourseGrades");

            migrationBuilder.DropForeignKey(
                name: "FK_LectureGrades_Tenants_TenantId",
                table: "LectureGrades");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentExams_Tenants_TenantId",
                table: "StudentExams");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentLectures_Tenants_TenantId",
                table: "StudentLectures");

            migrationBuilder.DropIndex(
                name: "IX_StudentExams_TenantId",
                table: "StudentExams");

            migrationBuilder.DropIndex(
                name: "IX_LectureGrades_TenantId",
                table: "LectureGrades");

            migrationBuilder.DropIndex(
                name: "IX_CourseGrades_TenantId",
                table: "CourseGrades");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StudentExams");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LectureGrades");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CourseGrades");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentLectures_Tenants_TenantId",
                table: "StudentLectures",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
