using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STA.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRetornoSftpToRota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cn_conexao_sftp_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ds_diretorio_local_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ds_diretorio_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ds_mascara_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "*");

            migrationBuilder.AddColumn<bool>(
                name: "fl_habilitar_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_rota_transferencia_cn_conexao_sftp_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia",
                column: "cn_conexao_sftp_retorno");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_rota_transferencia_tbl_conexao_sftp_cn_conexao_sftp_ret~",
                schema: "sta",
                table: "tbl_rota_transferencia",
                column: "cn_conexao_sftp_retorno",
                principalSchema: "sta",
                principalTable: "tbl_conexao_sftp",
                principalColumn: "cn_conexao_sftp",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_rota_transferencia_tbl_conexao_sftp_cn_conexao_sftp_ret~",
                schema: "sta",
                table: "tbl_rota_transferencia");

            migrationBuilder.DropIndex(
                name: "IX_tbl_rota_transferencia_cn_conexao_sftp_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia");

            migrationBuilder.DropColumn(
                name: "cn_conexao_sftp_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia");

            migrationBuilder.DropColumn(
                name: "ds_diretorio_local_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia");

            migrationBuilder.DropColumn(
                name: "ds_diretorio_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia");

            migrationBuilder.DropColumn(
                name: "ds_mascara_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia");

            migrationBuilder.DropColumn(
                name: "fl_habilitar_retorno",
                schema: "sta",
                table: "tbl_rota_transferencia");
        }
    }
}
