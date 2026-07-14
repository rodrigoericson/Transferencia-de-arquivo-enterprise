using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace STA.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEtapasRotasLogArquivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_etapa_transferencia",
                schema: "sta",
                columns: table => new
                {
                    cn_etapa = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cn_sistema = table.Column<int>(type: "integer", nullable: false),
                    nm_etapa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fl_ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    nr_ordem_execucao = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    hr_inicio_janela = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    hr_fim_janela = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    nr_intervalo_minutos = table.Column<int>(type: "integer", nullable: true),
                    dt_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    dt_alteracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_etapa_transferencia", x => x.cn_etapa);
                    table.ForeignKey(
                        name: "FK_tbl_etapa_transferencia_tbl_sistema_cn_sistema",
                        column: x => x.cn_sistema,
                        principalSchema: "sta",
                        principalTable: "tbl_sistema",
                        principalColumn: "cn_sistema",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_rota_transferencia",
                schema: "sta",
                columns: table => new
                {
                    cn_rota = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cn_etapa = table.Column<int>(type: "integer", nullable: false),
                    nr_ordem = table.Column<int>(type: "integer", nullable: false),
                    ds_diretorio_origem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ds_diretorio_backup = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ds_mascara_arquivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "*"),
                    ds_compacta_origem_tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    nr_dias_excluir = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    nr_tamanho_inicial_bytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    nr_tamanho_final_bytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    fl_excluir_origem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fl_ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_rota_transferencia", x => x.cn_rota);
                    table.ForeignKey(
                        name: "FK_tbl_rota_transferencia_tbl_etapa_transferencia_cn_etapa",
                        column: x => x.cn_etapa,
                        principalSchema: "sta",
                        principalTable: "tbl_etapa_transferencia",
                        principalColumn: "cn_etapa",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_log_arquivo",
                schema: "sta",
                columns: table => new
                {
                    cn_log_arquivo = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cn_log_processo = table.Column<int>(type: "integer", nullable: true),
                    cn_etapa = table.Column<int>(type: "integer", nullable: true),
                    cn_rota = table.Column<int>(type: "integer", nullable: true),
                    nm_arquivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ds_diretorio_origem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ds_diretorio_destino = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    nr_tamanho_bytes = table.Column<long>(type: "bigint", nullable: false),
                    dt_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dt_fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_status = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    ds_mensagem = table.Column<string>(type: "text", nullable: true),
                    fl_compactado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    fl_descompactado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_log_arquivo", x => x.cn_log_arquivo);
                    table.ForeignKey(
                        name: "FK_tbl_log_arquivo_tbl_etapa_transferencia_cn_etapa",
                        column: x => x.cn_etapa,
                        principalSchema: "sta",
                        principalTable: "tbl_etapa_transferencia",
                        principalColumn: "cn_etapa",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tbl_log_arquivo_tbl_log_processo_cn_log_processo",
                        column: x => x.cn_log_processo,
                        principalSchema: "sta",
                        principalTable: "tbl_log_processo",
                        principalColumn: "cn_log_processo",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tbl_log_arquivo_tbl_rota_transferencia_cn_rota",
                        column: x => x.cn_rota,
                        principalSchema: "sta",
                        principalTable: "tbl_rota_transferencia",
                        principalColumn: "cn_rota",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tbl_rota_destino",
                schema: "sta",
                columns: table => new
                {
                    cn_rota_destino = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cn_rota = table.Column<int>(type: "integer", nullable: false),
                    nr_ordem = table.Column<int>(type: "integer", nullable: false),
                    ds_diretorio_destino = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ds_descompacta_destino = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    fl_ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_rota_destino", x => x.cn_rota_destino);
                    table.ForeignKey(
                        name: "FK_tbl_rota_destino_tbl_rota_transferencia_cn_rota",
                        column: x => x.cn_rota,
                        principalSchema: "sta",
                        principalTable: "tbl_rota_transferencia",
                        principalColumn: "cn_rota",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_etapa_transferencia_cn_sistema_fl_ativo",
                schema: "sta",
                table: "tbl_etapa_transferencia",
                columns: new[] { "cn_sistema", "fl_ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_log_arquivo_cn_etapa_dt_inicio",
                schema: "sta",
                table: "tbl_log_arquivo",
                columns: new[] { "cn_etapa", "dt_inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_log_arquivo_cn_log_processo",
                schema: "sta",
                table: "tbl_log_arquivo",
                column: "cn_log_processo");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_log_arquivo_cn_rota",
                schema: "sta",
                table: "tbl_log_arquivo",
                column: "cn_rota");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_log_arquivo_id_status_dt_inicio",
                schema: "sta",
                table: "tbl_log_arquivo",
                columns: new[] { "id_status", "dt_inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_log_arquivo_nm_arquivo",
                schema: "sta",
                table: "tbl_log_arquivo",
                column: "nm_arquivo");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_rota_destino_cn_rota",
                schema: "sta",
                table: "tbl_rota_destino",
                column: "cn_rota");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_rota_transferencia_cn_etapa_nr_ordem",
                schema: "sta",
                table: "tbl_rota_transferencia",
                columns: new[] { "cn_etapa", "nr_ordem" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_log_arquivo",
                schema: "sta");

            migrationBuilder.DropTable(
                name: "tbl_rota_destino",
                schema: "sta");

            migrationBuilder.DropTable(
                name: "tbl_rota_transferencia",
                schema: "sta");

            migrationBuilder.DropTable(
                name: "tbl_etapa_transferencia",
                schema: "sta");
        }
    }
}
