using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coneic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPasswordResetThrottle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordResetRequestAt",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPasswordResetRequestAt",
                table: "Users");
        }
    }
}
