using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendence_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToLectures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "StudentLectures",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Lectures",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            // ────────────────────────────────────────────────────────
            // DATA MIGRATION: Update TenantId and Counters for existing data
            // ────────────────────────────────────────────────────────
            migrationBuilder.Sql(
                @"
                -- Update Lectures TenantId from their associated Courses
                UPDATE l
                SET l.TenantId = c.TenantId
                FROM Lectures l
                INNER JOIN Courses c ON l.CourseId = c.CourseId;

                -- Update StudentLectures TenantId from their associated Students
                UPDATE sl
                SET sl.TenantId = s.TenantId
                FROM StudentLectures sl
                INNER JOIN Students s ON sl.StudentId = s.StudentId;

                -- Update AttendedCount for historical Lectures
                UPDATE l
                SET l.AttendedCount = (SELECT COUNT(*) FROM StudentLectures sl WHERE sl.LectureId = l.LectureId)
                FROM Lectures l;

                -- Update StudentCount for historical Grades
                UPDATE g
                SET g.StudentCount = (SELECT COUNT(*) FROM Students s WHERE s.GradeId = g.GradeId)
                FROM Grades g;

                -- Fail-Safe: Abort if orphaned records exist instead of silent deletion
                IF EXISTS (SELECT 1 FROM Lectures WHERE TenantId = '') OR EXISTS (SELECT 1 FROM StudentLectures WHERE TenantId = '')
                BEGIN
                    RAISERROR(N'Migration aborted: Orphaned Lectures or StudentLectures found without a valid TenantId. Please review the data manually.', 16, 1);
                END
                ");
            // ────────────────────────────────────────────────────────

            migrationBuilder.CreateIndex(
                name: "IX_StudentLectures_TenantId",
                table: "StudentLectures",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_TenantId",
                table: "Lectures",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lectures_Tenants_TenantId",
                table: "Lectures",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentLectures_Tenants_TenantId",
                table: "StudentLectures",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lectures_Tenants_TenantId",
                table: "Lectures");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentLectures_Tenants_TenantId",
                table: "StudentLectures");

            migrationBuilder.DropIndex(
                name: "IX_StudentLectures_TenantId",
                table: "StudentLectures");

            migrationBuilder.DropIndex(
                name: "IX_Lectures_TenantId",
                table: "Lectures");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StudentLectures");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Lectures");
        }
    }
}
