using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendence_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeCodeAndAgeLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgeReferenceDate",
                table: "Students");

            migrationBuilder.AddColumn<int>(
                name: "Code",
                table: "Grades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxAge",
                table: "Grades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinAge",
                table: "Grades",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "MaxAge",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "MinAge",
                table: "Grades");

            migrationBuilder.AddColumn<DateOnly>(
                name: "AgeReferenceDate",
                table: "Students",
                type: "date",
                nullable: true);
        }
    }
}
