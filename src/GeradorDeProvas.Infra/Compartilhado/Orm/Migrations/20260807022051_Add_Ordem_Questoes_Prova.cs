using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeradorDeProvas.Infra.Compartilhado.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Add_Ordem_Questoes_Prova : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Ordem",
                table: "TBProvaQuestao",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                ;WITH QuestoesOrdenadas AS
                (
                    SELECT
                        ProvasId,
                        QuestoesId,
                        ROW_NUMBER() OVER (
                            PARTITION BY ProvasId
                            ORDER BY QuestoesId
                        ) - 1 AS Ordem
                    FROM TBProvaQuestao
                )
                UPDATE associacao
                SET associacao.Ordem = ordenada.Ordem
                FROM TBProvaQuestao AS associacao
                INNER JOIN QuestoesOrdenadas AS ordenada
                    ON ordenada.ProvasId = associacao.ProvasId
                    AND ordenada.QuestoesId = associacao.QuestoesId;
                """
            );

            migrationBuilder.AlterColumn<int>(
                name: "Ordem",
                table: "TBProvaQuestao",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBProvaQuestao_ProvasId_Ordem",
                table: "TBProvaQuestao",
                columns: new[] { "ProvasId", "Ordem" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TBProvaQuestao_ProvasId_Ordem",
                table: "TBProvaQuestao");

            migrationBuilder.DropColumn(
                name: "Ordem",
                table: "TBProvaQuestao");
        }
    }
}
