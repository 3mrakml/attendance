using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendence_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizationFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lectures_Grades_GradeId",
                table: "Lectures");

            migrationBuilder.DropColumn(
                name: "AndroidId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "AttendenceNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentLectureId",
                table: "StudentLectures");

            // تحديد حجم idcollege ليكون قابلاً للاستخدام كمفتاح Index (كان nvarchar(max))
            migrationBuilder.AlterColumn<string>(
                name: "idcollege",
                table: "Students",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // كذلك code للتوحيد
            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "Students",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // إعادة تسمية idcollege إلى QRToken (البيانات محفوظة)
            migrationBuilder.RenameColumn(
                name: "idcollege",
                table: "Students",
                newName: "QRToken");

            // إعادة تسمية code إلى SeatNumber (البيانات محفوظة)
            migrationBuilder.RenameColumn(
                name: "code",
                table: "Students",
                newName: "SeatNumber");

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendedAt",
                table: "StudentLectures",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "GradeId",
                table: "Lectures",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_QRToken",
                table: "Students",
                column: "QRToken",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lectures_Grades_GradeId",
                table: "Lectures",
                column: "GradeId",
                principalTable: "Grades",
                principalColumn: "GradeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lectures_Grades_GradeId",
                table: "Lectures");

            migrationBuilder.DropIndex(
                name: "IX_Students_QRToken",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "AttendedAt",
                table: "StudentLectures");

            // عكس التغييرات
            migrationBuilder.RenameColumn(
                name: "QRToken",
                table: "Students",
                newName: "idcollege");

            migrationBuilder.RenameColumn(
                name: "SeatNumber",
                table: "Students",
                newName: "code");

            migrationBuilder.AddColumn<string>(
                name: "AndroidId",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttendenceNumber",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StudentLectureId",
                table: "StudentLectures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "GradeId",
                table: "Lectures",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Lectures_Grades_GradeId",
                table: "Lectures",
                column: "GradeId",
                principalTable: "Grades",
                principalColumn: "GradeId");
        }
    }
}
