using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeradorDeProvas.Infra.Compartilhado.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Add_TBProva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBProva",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisciplinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MateriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Serie = table.Column<int>(type: "int", nullable: false),
                    QuantidadeQuestoes = table.Column<int>(type: "int", nullable: false),
                    ProvaRecuperacao = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBProva", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBProva_TBDisciplina",
                        column: x => x.DisciplinaId,
                        principalTable: "TBDisciplina",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TBProva_TBMateria",
                        column: x => x.MateriaId,
                        principalTable: "TBMateria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TBProvaQuestao",
                columns: table => new
                {
                    ProvasId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestoesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBProvaQuestao", x => new { x.ProvasId, x.QuestoesId });
                    table.ForeignKey(
                        name: "FK_TBProvaQuestao_TBProva_ProvasId",
                        column: x => x.ProvasId,
                        principalTable: "TBProva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TBProvaQuestao_TBQuestao_QuestoesId",
                        column: x => x.QuestoesId,
                        principalTable: "TBQuestao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBProva_DisciplinaId",
                table: "TBProva",
                column: "DisciplinaId");

            migrationBuilder.CreateIndex(
                name: "IX_TBProva_MateriaId",
                table: "TBProva",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "UQ_TBProva_UserId_Titulo",
                table: "TBProva",
                columns: new[] { "UserId", "Titulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBProvaQuestao_QuestoesId",
                table: "TBProvaQuestao",
                column: "QuestoesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBProvaQuestao");

            migrationBuilder.DropTable(
                name: "TBProva");
        }
    }
}
