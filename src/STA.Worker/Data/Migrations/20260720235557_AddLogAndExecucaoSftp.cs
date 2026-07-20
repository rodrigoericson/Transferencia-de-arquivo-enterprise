using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace STA.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogAndExecucaoSftp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_execucao_sftp",
                schema: "sta",
                columns: table => new
                {
                    cn_execucao_sftp = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cn_conexao_sftp = table.Column<int>(type: "integer", nullable: false),
                    dt_dia = table.Column<DateOnly>(type: "date", nullable: false),
                    hr_horario = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    id_resultado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dt_executado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_execucao_sftp", x => x.cn_execucao_sftp);
                    table.ForeignKey(
                        name: "FK_tbl_execucao_sftp_tbl_conexao_sftp_cn_conexao_sftp",
                        column: x => x.cn_conexao_sftp,
                        principalSchema: "sta",
                        principalTable: "tbl_conexao_sftp",
                        principalColumn: "cn_conexao_sftp",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_log_sftp",
                schema: "sta",
                columns: table => new
                {
                    cn_log_sftp = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cn_conexao_sftp = table.Column<int>(type: "integer", nullable: false),
                    cn_rota_destino = table.Column<int>(type: "integer", nullable: true),
                    id_tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    id_status = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    nm_arquivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    nr_tamanho_bytes = table.Column<long>(type: "bigint", nullable: true),
                    nr_duracao_ms = table.Column<int>(type: "integer", nullable: true),
                    ds_mensagem = table.Column<string>(type: "text", nullable: true),
                    dt_evento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_log_sftp", x => x.cn_log_sftp);
                    table.ForeignKey(
                        name: "FK_tbl_log_sftp_tbl_conexao_sftp_cn_conexao_sftp",
                        column: x => x.cn_conexao_sftp,
                        principalSchema: "sta",
                        principalTable: "tbl_conexao_sftp",
                        principalColumn: "cn_conexao_sftp",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_log_sftp_tbl_rota_destino_cn_rota_destino",
                        column: x => x.cn_rota_destino,
                        principalSchema: "sta",
                        principalTable: "tbl_rota_destino",
                        principalColumn: "cn_rota_destino",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_execucao_sftp_cn_conexao_sftp_dt_dia_hr_horario",
                schema: "sta",
                table: "tbl_execucao_sftp",
                columns: new[] { "cn_conexao_sftp", "dt_dia", "hr_horario" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_log_sftp_cn_conexao_sftp_dt_evento",
                schema: "sta",
                table: "tbl_log_sftp",
                columns: new[] { "cn_conexao_sftp", "dt_evento" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_log_sftp_cn_rota_destino",
                schema: "sta",
                table: "tbl_log_sftp",
                column: "cn_rota_destino");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_log_sftp_id_tipo_dt_evento",
                schema: "sta",
                table: "tbl_log_sftp",
                columns: new[] { "id_tipo", "dt_evento" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_execucao_sftp",
                schema: "sta");

            migrationBuilder.DropTable(
                name: "tbl_log_sftp",
                schema: "sta");
        }
    }
}
