using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RdClean.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseIdsInsteadOfStoringFilesDirectly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageBytes",
                table: "Redraws");

            migrationBuilder.DropColumn(
                name: "ImageBytes",
                table: "Images");

            migrationBuilder.AddColumn<Guid>(
                name: "FileId",
                table: "Redraws",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FileId",
                table: "Images",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileId",
                table: "Redraws");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "Images");

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageBytes",
                table: "Redraws",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageBytes",
                table: "Images",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
