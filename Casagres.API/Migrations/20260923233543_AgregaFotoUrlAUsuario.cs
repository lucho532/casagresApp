using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casagres.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregaFotoUrlAUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "foto_url",
                table: "casagres_login",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "foto_url",
                table: "casagres_login");
        }
    }
}
