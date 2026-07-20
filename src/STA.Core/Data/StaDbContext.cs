using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STA.Core.Data.Entities;

namespace STA.Core.Data;

/// <summary>
/// DbContext principal da aplicação STA.
/// Mapeia entidades para o schema 'sta' do PostgreSQL.
/// </summary>
public class StaDbContext : DbContext
{
    public StaDbContext(DbContextOptions<StaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sistema> Sistemas { get; set; } = null!;
    public DbSet<ParametroSistema> Parametros { get; set; } = null!;
    public DbSet<LogProcesso> Logs { get; set; } = null!;
    public DbSet<EtapaTransferencia> Etapas { get; set; } = null!;
    public DbSet<RotaTransferencia> Rotas { get; set; } = null!;
    public DbSet<RotaDestino> RotaDestinos { get; set; } = null!;
    public DbSet<LogArquivo> LogArquivos { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Auditoria> Auditorias { get; set; } = null!;
    public DbSet<ConexaoSftp> ConexoesSftp { get; set; } = null!;
    public DbSet<LogSftp> LogsSftp { get; set; } = null!;
    public DbSet<ExecucaoSftp> ExecucoesSftp { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Usar schema 'sta' para todas as tabelas
        modelBuilder.HasDefaultSchema("sta");

        ConfigureSistema(modelBuilder.Entity<Sistema>());
        ConfigureParametroSistema(modelBuilder.Entity<ParametroSistema>());
        ConfigureLogProcesso(modelBuilder.Entity<LogProcesso>());
        ConfigureEtapaTransferencia(modelBuilder.Entity<EtapaTransferencia>());
        ConfigureRotaTransferencia(modelBuilder.Entity<RotaTransferencia>());
        ConfigureRotaDestino(modelBuilder.Entity<RotaDestino>());
        ConfigureLogArquivo(modelBuilder.Entity<LogArquivo>());
        ConfigureUsuario(modelBuilder.Entity<Usuario>());
        ConfigureAuditoria(modelBuilder.Entity<Auditoria>());
        ConfigureConexaoSftp(modelBuilder.Entity<ConexaoSftp>());
        ConfigureLogSftp(modelBuilder.Entity<LogSftp>());
        ConfigureExecucaoSftp(modelBuilder.Entity<ExecucaoSftp>());
    }

    private static void ConfigureSistema(EntityTypeBuilder<Sistema> builder)
    {
        builder.ToTable("tbl_sistema");

        builder.HasKey(s => s.CnSistema);

        builder.Property(s => s.CnSistema)
            .HasColumnName("cn_sistema")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.CdAliasSistema)
            .HasColumnName("cd_alias_sistema")
            .HasMaxLength(20)
            .IsRequired();

        // Índice único para alias (segurança e performance)
        builder.HasIndex(s => s.CdAliasSistema).IsUnique();

        // Relacionamentos
        builder.HasMany(s => s.Parametros)
            .WithOne(p => p.Sistema)
            .HasForeignKey(p => p.CnSistema)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Logs)
            .WithOne(l => l.Sistema)
            .HasForeignKey(l => l.CnSistema)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureParametroSistema(EntityTypeBuilder<ParametroSistema> builder)
    {
        builder.ToTable("tbl_parametro_sistema");

        builder.HasKey(p => new { p.CnParametroSistema, p.CnSistema });

        builder.Property(p => p.CnParametroSistema)
            .HasColumnName("cn_parametro_sistema")
            .ValueGeneratedNever();

        builder.Property(p => p.CnSistema)
            .HasColumnName("cn_sistema");

        builder.Property(p => p.CdParametroSistema)
            .HasColumnName("cd_parametro_sistema")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(p => p.Sistema)
            .WithMany(s => s.Parametros)
            .HasForeignKey(p => p.CnSistema)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureLogProcesso(EntityTypeBuilder<LogProcesso> builder)
    {
        builder.ToTable("tbl_log_processo");

        builder.HasKey(l => l.CnLogProcesso);

        builder.Property(l => l.CnLogProcesso)
            .HasColumnName("cn_log_processo")
            .ValueGeneratedOnAdd();

        builder.Property(l => l.CnSistema)
            .HasColumnName("cn_sistema")
            .IsRequired();

        builder.Property(l => l.CnProcesso)
            .HasColumnName("cn_processo")
            .IsRequired();

        builder.Property(l => l.DtInicio)
            .HasColumnName("dt_inicio")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(l => l.DtFimProcesso)
            .HasColumnName("dt_fim_processo");

        builder.Property(l => l.IdStatusProcesso)
            .HasColumnName("id_status_processo")
            .HasMaxLength(1)
            .IsRequired();

        builder.Property(l => l.QtRegistrosProcessados)
            .HasColumnName("qt_registros_processados")
            .HasDefaultValue(0L);

        builder.Property(l => l.VlRegistrosProcessados)
            .HasColumnName("vl_registros_processados")
            .HasDefaultValue(0L);

        builder.Property(l => l.QtRegistrosErro)
            .HasColumnName("qt_registros_erro")
            .HasDefaultValue(0L);

        builder.Property(l => l.VlRegistrosErro)
            .HasColumnName("vl_registros_erro")
            .HasDefaultValue(0L);

        builder.Property(l => l.XmlObsProcesso)
            .HasColumnName("xml_obs_processo")
            .HasColumnType("text");

        builder.HasOne(l => l.Sistema)
            .WithMany(s => s.Logs)
            .HasForeignKey(l => l.CnSistema)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices para queries comuns
        builder.HasIndex(l => new { l.CnSistema, l.CnProcesso });
        builder.HasIndex(l => l.DtInicio);
    }

    private static void ConfigureEtapaTransferencia(EntityTypeBuilder<EtapaTransferencia> builder)
    {
        builder.ToTable("tbl_etapa_transferencia");

        builder.HasKey(e => e.CnEtapa);

        builder.Property(e => e.CnEtapa)
            .HasColumnName("cn_etapa")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.CnSistema)
            .HasColumnName("cn_sistema")
            .IsRequired();

        builder.Property(e => e.NmEtapa)
            .HasColumnName("nm_etapa")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.FlAtivo)
            .HasColumnName("fl_ativo")
            .HasDefaultValue(true);

        builder.Property(e => e.NrOrdemExecucao)
            .HasColumnName("nr_ordem_execucao")
            .HasDefaultValue(0);

        builder.Property(e => e.HrInicioJanela)
            .HasColumnName("hr_inicio_janela")
            .HasMaxLength(8);

        builder.Property(e => e.HrFimJanela)
            .HasColumnName("hr_fim_janela")
            .HasMaxLength(8);

        builder.Property(e => e.NrIntervaloMinutos)
            .HasColumnName("nr_intervalo_minutos");

        builder.Property(e => e.DtCriacao)
            .HasColumnName("dt_criacao")
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.DtAlteracao)
            .HasColumnName("dt_alteracao");

        builder.HasOne(e => e.Sistema)
            .WithMany()
            .HasForeignKey(e => e.CnSistema)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Rotas)
            .WithOne(r => r.Etapa)
            .HasForeignKey(r => r.CnEtapa)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CnSistema, e.FlAtivo });
    }

    private static void ConfigureRotaTransferencia(EntityTypeBuilder<RotaTransferencia> builder)
    {
        builder.ToTable("tbl_rota_transferencia");

        builder.HasKey(r => r.CnRota);

        builder.Property(r => r.CnRota)
            .HasColumnName("cn_rota")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.CnEtapa)
            .HasColumnName("cn_etapa")
            .IsRequired();

        builder.Property(r => r.NrOrdem)
            .HasColumnName("nr_ordem")
            .IsRequired();

        builder.Property(r => r.DsDiretorioOrigem)
            .HasColumnName("ds_diretorio_origem")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.DsDiretorioBackup)
            .HasColumnName("ds_diretorio_backup")
            .HasMaxLength(500);

        builder.Property(r => r.DsMascaraArquivo)
            .HasColumnName("ds_mascara_arquivo")
            .HasMaxLength(200)
            .HasDefaultValue("*");

        builder.Property(r => r.DsCompactaOrigemTipo)
            .HasColumnName("ds_compacta_origem_tipo")
            .HasMaxLength(10);

        builder.Property(r => r.NrDiasExcluir)
            .HasColumnName("nr_dias_excluir")
            .HasDefaultValue(0);

        builder.Property(r => r.NrTamanhoInicialBytes)
            .HasColumnName("nr_tamanho_inicial_bytes")
            .HasDefaultValue(0L);

        builder.Property(r => r.NrTamanhoFinalBytes)
            .HasColumnName("nr_tamanho_final_bytes")
            .HasDefaultValue(0L);

        builder.Property(r => r.FlExcluirOrigem)
            .HasColumnName("fl_excluir_origem")
            .HasDefaultValue(true);

        builder.Property(r => r.FlAtivo)
            .HasColumnName("fl_ativo")
            .HasDefaultValue(true);

        builder.HasOne(r => r.Etapa)
            .WithMany(e => e.Rotas)
            .HasForeignKey(r => r.CnEtapa)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Destinos)
            .WithOne(d => d.Rota)
            .HasForeignKey(d => d.CnRota)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.CnEtapa, r.NrOrdem });
    }

    private static void ConfigureRotaDestino(EntityTypeBuilder<RotaDestino> builder)
    {
        builder.ToTable("tbl_rota_destino");

        builder.HasKey(d => d.CnRotaDestino);

        builder.Property(d => d.CnRotaDestino)
            .HasColumnName("cn_rota_destino")
            .ValueGeneratedOnAdd();

        builder.Property(d => d.CnRota)
            .HasColumnName("cn_rota")
            .IsRequired();

        builder.Property(d => d.NrOrdem)
            .HasColumnName("nr_ordem")
            .IsRequired();

        builder.Property(d => d.DsDiretorioDestino)
            .HasColumnName("ds_diretorio_destino")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.DsDescompactaDestino)
            .HasColumnName("ds_descompacta_destino")
            .HasMaxLength(10);

        builder.Property(d => d.DsPadraoRename)
            .HasColumnName("DsPadraoRename")
            .HasMaxLength(200);

        builder.Property(d => d.IdProtocolo)
            .HasColumnName("id_protocolo")
            .HasMaxLength(10)
            .HasDefaultValue("LOCAL")
            .IsRequired();

        builder.Property(d => d.CnConexaoSftp)
            .HasColumnName("cn_conexao_sftp");

        builder.Property(d => d.FlAtivo)
            .HasColumnName("fl_ativo")
            .HasDefaultValue(true);

        builder.HasOne(d => d.Rota)
            .WithMany(r => r.Destinos)
            .HasForeignKey(d => d.CnRota)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.ConexaoSftp)
            .WithMany()
            .HasForeignKey(d => d.CnConexaoSftp)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureLogArquivo(EntityTypeBuilder<LogArquivo> builder)
    {
        builder.ToTable("tbl_log_arquivo");

        builder.HasKey(l => l.CnLogArquivo);

        builder.Property(l => l.CnLogArquivo)
            .HasColumnName("cn_log_arquivo")
            .ValueGeneratedOnAdd();

        builder.Property(l => l.CnLogProcesso)
            .HasColumnName("cn_log_processo");

        builder.Property(l => l.CnEtapa)
            .HasColumnName("cn_etapa");

        builder.Property(l => l.CnRota)
            .HasColumnName("cn_rota");

        builder.Property(l => l.NmArquivo)
            .HasColumnName("nm_arquivo")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.DsDiretorioOrigem)
            .HasColumnName("ds_diretorio_origem")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.DsDiretorioDestino)
            .HasColumnName("ds_diretorio_destino")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.NrTamanhoBytes)
            .HasColumnName("nr_tamanho_bytes");

        builder.Property(l => l.DtInicio)
            .HasColumnName("dt_inicio")
            .IsRequired();

        builder.Property(l => l.DtFim)
            .HasColumnName("dt_fim");

        builder.Property(l => l.IdStatus)
            .HasColumnName("id_status")
            .HasMaxLength(1)
            .IsRequired();

        builder.Property(l => l.DsMensagem)
            .HasColumnName("ds_mensagem")
            .HasColumnType("text");

        builder.Property(l => l.FlCompactado)
            .HasColumnName("fl_compactado")
            .HasDefaultValue(false);

        builder.Property(l => l.FlDescompactado)
            .HasColumnName("fl_descompactado")
            .HasDefaultValue(false);

        builder.HasOne(l => l.LogProcesso)
            .WithMany()
            .HasForeignKey(l => l.CnLogProcesso)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Etapa)
            .WithMany()
            .HasForeignKey(l => l.CnEtapa)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Rota)
            .WithMany()
            .HasForeignKey(l => l.CnRota)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => new { l.CnEtapa, l.DtInicio });
        builder.HasIndex(l => l.NmArquivo);
        builder.HasIndex(l => new { l.IdStatus, l.DtInicio });
        builder.HasIndex(l => l.CnLogProcesso);
    }

    private static void ConfigureUsuario(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("tbl_usuario");

        builder.HasKey(u => u.CnUsuario);

        builder.Property(u => u.CnUsuario)
            .HasColumnName("cn_usuario")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.NmUsuario)
            .HasColumnName("nm_usuario")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.NmDisplay)
            .HasColumnName("nm_display")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.DsSenhaHash)
            .HasColumnName("ds_senha_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.IdRole)
            .HasColumnName("id_role")
            .HasMaxLength(20)
            .HasDefaultValue("Viewer")
            .IsRequired();

        builder.Property(u => u.FlAtivo)
            .HasColumnName("fl_ativo")
            .HasDefaultValue(true);

        builder.Property(u => u.NrTentativasFalhas)
            .HasColumnName("nr_tentativas_falhas")
            .HasDefaultValue(0);

        builder.Property(u => u.DtBloqueadoAte)
            .HasColumnName("dt_bloqueado_ate");

        builder.Property(u => u.DtCriacao)
            .HasColumnName("dt_criacao")
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.DtUltimoLogin)
            .HasColumnName("dt_ultimo_login");

        builder.HasIndex(u => u.NmUsuario).IsUnique();
    }

    private static void ConfigureAuditoria(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("tbl_auditoria");

        builder.HasKey(a => a.CnAuditoria);

        builder.Property(a => a.CnAuditoria)
            .HasColumnName("cn_auditoria")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.CnUsuario)
            .HasColumnName("cn_usuario");

        builder.Property(a => a.NmUsuario)
            .HasColumnName("nm_usuario")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.IdEntidade)
            .HasColumnName("id_entidade")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.IdReferencia)
            .HasColumnName("id_referencia");

        builder.Property(a => a.IdAcao)
            .HasColumnName("id_acao")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.DtAcao)
            .HasColumnName("dt_acao")
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.DsDetalhe)
            .HasColumnName("ds_detalhe")
            .HasColumnType("text");

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(a => a.CnUsuario)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.DtAcao).IsDescending();
        builder.HasIndex(a => a.NmUsuario);
        builder.HasIndex(a => new { a.IdEntidade, a.IdAcao });
    }

    private static void ConfigureConexaoSftp(EntityTypeBuilder<ConexaoSftp> builder)
    {
        builder.ToTable("tbl_conexao_sftp");

        builder.HasKey(c => c.CnConexaoSftp);

        builder.Property(c => c.CnConexaoSftp)
            .HasColumnName("cn_conexao_sftp")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.NmConexao)
            .HasColumnName("nm_conexao")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.DsHost)
            .HasColumnName("ds_host")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.NrPorta)
            .HasColumnName("nr_porta")
            .HasDefaultValue(22);

        builder.Property(c => c.DsUsuario)
            .HasColumnName("ds_usuario")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.DsSenhaCriptografada)
            .HasColumnName("ds_senha_criptografada")
            .HasColumnType("bytea");

        builder.Property(c => c.DsCaminhoChavePrivada)
            .HasColumnName("ds_caminho_chave_privada")
            .HasMaxLength(500);

        builder.Property(c => c.DsHorariosExecucao)
            .HasColumnName("ds_horarios_execucao")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.DsDiasSemana)
            .HasColumnName("ds_dias_semana")
            .HasMaxLength(30)
            .HasDefaultValue("seg,ter,qua,qui,sex");

        builder.Property(c => c.FlArquivoObrigatorio)
            .HasColumnName("fl_arquivo_obrigatorio")
            .HasDefaultValue(false);

        builder.Property(c => c.NrToleranciaMinutos)
            .HasColumnName("nr_tolerancia_minutos")
            .HasDefaultValue(10);

        builder.Property(c => c.FlAtivo)
            .HasColumnName("fl_ativo")
            .HasDefaultValue(true);

        builder.Property(c => c.DtCriacao)
            .HasColumnName("dt_criacao")
            .HasDefaultValueSql("NOW()");

        builder.Property(c => c.DtUltimoUso)
            .HasColumnName("dt_ultimo_uso");

        builder.HasIndex(c => c.NmConexao).IsUnique();
    }

    private static void ConfigureLogSftp(EntityTypeBuilder<LogSftp> builder)
    {
        builder.ToTable("tbl_log_sftp");

        builder.HasKey(l => l.CnLogSftp);

        builder.Property(l => l.CnLogSftp)
            .HasColumnName("cn_log_sftp")
            .ValueGeneratedOnAdd();

        builder.Property(l => l.CnConexaoSftp)
            .HasColumnName("cn_conexao_sftp")
            .IsRequired();

        builder.Property(l => l.CnRotaDestino)
            .HasColumnName("cn_rota_destino");

        builder.Property(l => l.IdTipo)
            .HasColumnName("id_tipo")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.IdStatus)
            .HasColumnName("id_status")
            .HasMaxLength(1)
            .IsRequired();

        builder.Property(l => l.NmArquivo)
            .HasColumnName("nm_arquivo")
            .HasMaxLength(500);

        builder.Property(l => l.NrTamanhoBytes)
            .HasColumnName("nr_tamanho_bytes");

        builder.Property(l => l.NrDuracaoMs)
            .HasColumnName("nr_duracao_ms");

        builder.Property(l => l.DsMensagem)
            .HasColumnName("ds_mensagem")
            .HasColumnType("text");

        builder.Property(l => l.DtEvento)
            .HasColumnName("dt_evento")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(l => l.ConexaoSftp)
            .WithMany()
            .HasForeignKey(l => l.CnConexaoSftp)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.RotaDestino)
            .WithMany()
            .HasForeignKey(l => l.CnRotaDestino)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => new { l.CnConexaoSftp, l.DtEvento }).IsDescending(false, true);
        builder.HasIndex(l => new { l.IdTipo, l.DtEvento }).IsDescending(false, true);
    }

    private static void ConfigureExecucaoSftp(EntityTypeBuilder<ExecucaoSftp> builder)
    {
        builder.ToTable("tbl_execucao_sftp");

        builder.HasKey(e => e.CnExecucaoSftp);

        builder.Property(e => e.CnExecucaoSftp)
            .HasColumnName("cn_execucao_sftp")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.CnConexaoSftp)
            .HasColumnName("cn_conexao_sftp")
            .IsRequired();

        builder.Property(e => e.DtDia)
            .HasColumnName("dt_dia")
            .IsRequired();

        builder.Property(e => e.HrHorario)
            .HasColumnName("hr_horario")
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.IdResultado)
            .HasColumnName("id_resultado")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.DtExecutadoEm)
            .HasColumnName("dt_executado_em")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(e => e.ConexaoSftp)
            .WithMany()
            .HasForeignKey(e => e.CnConexaoSftp)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CnConexaoSftp, e.DtDia, e.HrHorario }).IsUnique();
    }
}
