using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STA.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSftpToRotaDestino : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cn_conexao_sftp",
                schema: "sta",
                table: "tbl_rota_destino",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_protocolo",
                schema: "sta",
                table: "tbl_rota_destino",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "LOCAL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_rota_destino_cn_conexao_sftp",
                schema: "sta",
                table: "tbl_rota_destino",
                column: "cn_conexao_sftp");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_rota_destino_tbl_conexao_sftp_cn_conexao_sftp",
                schema: "sta",
                table: "tbl_rota_destino",
                column: "cn_conexao_sftp",
                principalSchema: "sta",
                principalTable: "tbl_conexao_sftp",
                principalColumn: "cn_conexao_sftp",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_rota_destino_tbl_conexao_sftp_cn_conexao_sftp",
                schema: "sta",
                table: "tbl_rota_destino");

            migrationBuilder.DropIndex(
                name: "IX_tbl_rota_destino_cn_conexao_sftp",
                schema: "sta",
                table: "tbl_rota_destino");

            migrationBuilder.DropColumn(
                name: "cn_conexao_sftp",
                schema: "sta",
                table: "tbl_rota_destino");

            migrationBuilder.DropColumn(
                name: "id_protocolo",
                schema: "sta",
                table: "tbl_rota_destino");
        }
    }
}
