using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RdClean.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaskFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaskFileId",
                table: "Images",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaskMimeType",
                table: "Images",
                type: "TEXT",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaskFileId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "MaskMimeType",
                table: "Images");
        }
    }
}
