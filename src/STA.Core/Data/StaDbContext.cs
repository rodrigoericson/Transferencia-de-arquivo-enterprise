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

        builder.Property(d => d.FlAtivo)
            .HasColumnName("fl_ativo")
            .HasDefaultValue(true);

        builder.HasOne(d => d.Rota)
            .WithMany(r => r.Destinos)
            .HasForeignKey(d => d.CnRota)
            .OnDelete(DeleteBehavior.Cascade);
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
}
