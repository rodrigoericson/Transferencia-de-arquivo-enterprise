using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STA.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDsPadraoRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DsPadraoRename",
                schema: "sta",
                table: "tbl_rota_destino",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DsPadraoRename",
                schema: "sta",
                table: "tbl_rota_destino");
        }
    }
}
