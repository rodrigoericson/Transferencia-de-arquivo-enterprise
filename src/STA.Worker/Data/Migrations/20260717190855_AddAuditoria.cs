using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace STA.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_auditoria",
                schema: "sta",
                columns: table => new
                {
                    cn_auditoria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cn_usuario = table.Column<int>(type: "integer", nullable: true),
                    nm_usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    id_entidade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    id_referencia = table.Column<int>(type: "integer", nullable: false),
                    id_acao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dt_acao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ds_detalhe = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_auditoria", x => x.cn_auditoria);
                    table.ForeignKey(
                        name: "FK_tbl_auditoria_tbl_usuario_cn_usuario",
                        column: x => x.cn_usuario,
                        principalSchema: "sta",
                        principalTable: "tbl_usuario",
                        principalColumn: "cn_usuario",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_auditoria_cn_usuario",
                schema: "sta",
                table: "tbl_auditoria",
                column: "cn_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_auditoria_dt_acao",
                schema: "sta",
                table: "tbl_auditoria",
                column: "dt_acao",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_auditoria_id_entidade_id_acao",
                schema: "sta",
                table: "tbl_auditoria",
                columns: new[] { "id_entidade", "id_acao" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_auditoria_nm_usuario",
                schema: "sta",
                table: "tbl_auditoria",
                column: "nm_usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_auditoria",
                schema: "sta");
        }
    }
}
