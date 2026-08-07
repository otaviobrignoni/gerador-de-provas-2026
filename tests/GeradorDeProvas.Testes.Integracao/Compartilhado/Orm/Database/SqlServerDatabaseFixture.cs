using System.Data;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.Testes.Integracao.Compartilhado.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GeradorDeProvas.Testes.Integracao.Compartilhado.Orm.Database;

internal sealed class SqlServerDatabaseFixture : IAsyncDisposable
{
    public const string NomeConnectionString = "SqlServerTests";
    public const string NomeVariavelCadeiaConexao = "ConnectionStrings__SqlServerTests";
    public const string PrefixoBanco = "GeradorDeProvasTesting_";

    private readonly HashSet<string> bancosCriados = [];
    private readonly string idExecucao = Guid.NewGuid().ToString("N");
    private string? cadeiaConexaoServidor;
    private string? nomeBancoAtual;

    public string CadeiaConexao
    {
        get
        {
            if (nomeBancoAtual is null)
                throw new InvalidOperationException("O banco de teste ainda não foi criado.");

            var builder = new SqlConnectionStringBuilder(ObterCadeiaConexaoServidor())
            {
                InitialCatalog = nomeBancoAtual,
                TrustServerCertificate = true
            };

            return builder.ConnectionString;
        }
    }

    public async Task VerificarServidorDisponivelAsync()
    {
        string? configuracao = Environment.GetEnvironmentVariable(NomeVariavelCadeiaConexao);

        if (string.IsNullOrWhiteSpace(configuracao))
        {
            IConfiguration secrets = new ConfigurationBuilder()
                .AddUserSecrets<SqlServerDatabaseFixture>(optional: true)
                .Build();

            configuracao = secrets.GetConnectionString(NomeConnectionString);
        }

        if (string.IsNullOrWhiteSpace(configuracao))
        {
            throw new InvalidOperationException(
                $"Os testes Database exigem um SQL Server disponível. "
                + $"Configure o User Secret 'ConnectionStrings:{NomeConnectionString}' "
                + $"ou a variável de ambiente {NomeVariavelCadeiaConexao} com uma connection "
                + "string de uma conta autorizada a criar e excluir bancos."
            );
        }

        SqlConnectionStringBuilder builder;

        try
        {
            builder = new SqlConnectionStringBuilder(configuracao)
            {
                InitialCatalog = "master",
                TrustServerCertificate = true
            };
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"A connection string 'ConnectionStrings:{NomeConnectionString}' não é válida.",
                exception
            );
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            throw new InvalidOperationException(
                $"A connection string 'ConnectionStrings:{NomeConnectionString}' deve informar "
                + "o endereço do SQL Server."
            );
        }

        try
        {
            await using var conexao = new SqlConnection(builder.ConnectionString);
            await conexao.OpenAsync();

            await using SqlCommand comando = conexao.CreateCommand();
            comando.CommandText =
                """
                SELECT CASE
                    WHEN IS_SRVROLEMEMBER(N'sysadmin') = 1
                        OR HAS_PERMS_BY_NAME(NULL, NULL, N'CREATE ANY DATABASE') = 1
                    THEN 1
                    ELSE 0
                END;
                """;

            object? resultado = await comando.ExecuteScalarAsync();

            if (Convert.ToInt32(resultado) != 1)
            {
                throw new InvalidOperationException(
                    $"A conta configurada em 'ConnectionStrings:{NomeConnectionString}' não "
                    + "possui permissão "
                    + "para criar bancos descartáveis no SQL Server."
                );
            }
        }
        catch (SqlException exception)
        {
            throw new InvalidOperationException(
                $"Não foi possível acessar o SQL Server em '{builder.DataSource}'. "
                + $"Verifique o servidor e 'ConnectionStrings:{NomeConnectionString}'.",
                exception
            );
        }

        cadeiaConexaoServidor = builder.ConnectionString;
    }

    public async Task CriarBancoLimpoAsync()
    {
        if (cadeiaConexaoServidor is null)
            await VerificarServidorDisponivelAsync();

        await ExcluirBancoAtualAsync();

        string nomeBanco = $"{PrefixoBanco}{idExecucao}_{Guid.NewGuid():N}";

        await CriarBancoAsync(nomeBanco);

        nomeBancoAtual = nomeBanco;
        bancosCriados.Add(nomeBanco);

        try
        {
            await using GeradorDeProvasDbContext dbContext = CriarContextoSemUsuario();

            await dbContext.Database.MigrateAsync();
        }
        catch
        {
            await ExcluirBancoAtualAsync();
            throw;
        }
    }

    public GeradorDeProvasDbContext CriarContexto(Guid usuarioId)
    {
        var options = new DbContextOptionsBuilder<GeradorDeProvasDbContext>()
            .UseSqlServer(CadeiaConexao)
            .Options;

        return new GeradorDeProvasDbContext(options, new FalsoProvedorDeUsuario(usuarioId));
    }

    public GeradorDeProvasDbContext CriarContextoSemUsuario()
    {
        var options = new DbContextOptionsBuilder<GeradorDeProvasDbContext>()
            .UseSqlServer(CadeiaConexao)
            .Options;

        return new GeradorDeProvasDbContext(options);
    }

    public async Task ExcluirBancoAtualAsync()
    {
        if (nomeBancoAtual is null)
            return;

        string nomeBanco = nomeBancoAtual;
        nomeBancoAtual = null;

        await ExcluirBancoAsync(nomeBanco);
    }

    private async Task CriarBancoAsync(string nomeBanco)
    {
        await using var conexao = new SqlConnection(ObterCadeiaConexaoServidor());
        await conexao.OpenAsync();

        await using SqlCommand comando = conexao.CreateCommand();
        comando.CommandText = $"CREATE DATABASE {CitarIdentificador(nomeBanco)};";
        await comando.ExecuteNonQueryAsync();
    }

    private async Task ExcluirBancoAsync(string nomeBanco)
    {
        SqlConnection.ClearAllPools();

        await using var conexao = new SqlConnection(ObterCadeiaConexaoServidor());
        await conexao.OpenAsync();

        await using SqlCommand comando = conexao.CreateCommand();
        comando.CommandText =
            $"""
            IF DB_ID(@nomeBanco) IS NOT NULL
            BEGIN
                ALTER DATABASE {CitarIdentificador(nomeBanco)}
                    SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE {CitarIdentificador(nomeBanco)};
            END;
            """;
        comando.Parameters.Add("@nomeBanco", SqlDbType.NVarChar, 128).Value = nomeBanco;

        await comando.ExecuteNonQueryAsync();

        bancosCriados.Remove(nomeBanco);
        SqlConnection.ClearAllPools();
    }

    public async ValueTask DisposeAsync()
    {
        List<Exception> erros = [];
        nomeBancoAtual = null;

        foreach (string nomeBanco in bancosCriados.ToArray())
        {
            try
            {
                await ExcluirBancoAsync(nomeBanco);
            }
            catch (Exception exception)
            {
                erros.Add(exception);
            }
        }

        if (erros.Count > 0)
            throw new AggregateException("Não foi possível excluir todos os bancos de teste.", erros);
    }

    private string ObterCadeiaConexaoServidor() => cadeiaConexaoServidor
        ?? throw new InvalidOperationException("O SQL Server de testes ainda não foi validado.");

    private static string CitarIdentificador(string identificador) =>
        $"[{identificador.Replace("]", "]]", StringComparison.Ordinal)}]";
}
