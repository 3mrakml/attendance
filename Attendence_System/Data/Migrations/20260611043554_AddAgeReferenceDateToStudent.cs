using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendence_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgeReferenceDateToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AgeReferenceDate",
                table: "Students",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgeReferenceDate",
                table: "Students");
        }
    }
}
