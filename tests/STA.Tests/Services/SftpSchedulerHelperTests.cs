using STA.Core.Services;
using Xunit;

namespace STA.Tests.Services;

public class SftpSchedulerHelperTests
{
    [Fact]
    public void IsDiaHabilitado_DiaNoLista_RetornaTrue()
    {
        var segunda = new DateTime(2026, 7, 20, 10, 0, 0); // segunda-feira
        Assert.True(SftpSchedulerHelper.IsDiaHabilitado("seg,ter,qua", segunda));
    }

    [Fact]
    public void IsDiaHabilitado_DiaForaDaLista_RetornaFalse()
    {
        var domingo = new DateTime(2026, 7, 19, 10, 0, 0); // domingo
        Assert.False(SftpSchedulerHelper.IsDiaHabilitado("seg,ter,qua,qui,sex", domingo));
    }

    [Fact]
    public void IsDiaHabilitado_ListaVazia_RetornaTrue()
    {
        var qualquerDia = new DateTime(2026, 7, 20, 10, 0, 0);
        Assert.True(SftpSchedulerHelper.IsDiaHabilitado("", qualquerDia));
    }

    [Fact]
    public void GetHorarioAtivo_DentroTolerancia_RetornaHorario()
    {
        var agora = new DateTime(2026, 7, 20, 10, 3, 0); // 10:03
        var result = SftpSchedulerHelper.GetHorarioAtivo("08:00,10:00,15:00", agora, 10);
        Assert.Equal("10:00", result);
    }

    [Fact]
    public void GetHorarioAtivo_ForaTolerancia_RetornaNull()
    {
        var agora = new DateTime(2026, 7, 20, 10, 15, 0); // 10:15 (15min depois)
        var result = SftpSchedulerHelper.GetHorarioAtivo("10:00", agora, 10);
        Assert.Null(result);
    }

    [Fact]
    public void GetHorarioAtivo_AntesDaHora_RetornaNull()
    {
        var agora = new DateTime(2026, 7, 20, 9, 58, 0); // 09:58 (antes do 10:00)
        var result = SftpSchedulerHelper.GetHorarioAtivo("10:00", agora, 10);
        Assert.Null(result);
    }

    [Fact]
    public void IsUltimoHorarioDoDia_UltimoHorario_RetornaTrue()
    {
        Assert.True(SftpSchedulerHelper.IsUltimoHorarioDoDia("04:00,10:00,20:00", "20:00"));
    }

    [Fact]
    public void IsUltimoHorarioDoDia_NaoUltimo_RetornaFalse()
    {
        Assert.False(SftpSchedulerHelper.IsUltimoHorarioDoDia("04:00,10:00,20:00", "10:00"));
    }

    [Fact]
    public void IsUltimoHorarioDoDia_HorarioSemPadding_RetornaTrue()
    {
        Assert.True(SftpSchedulerHelper.IsUltimoHorarioDoDia("7:00,15:00", "15:00"));
    }

    [Fact]
    public void IsUltimoHorarioDoDia_PrimeiroHorarioSemPadding_RetornaFalse()
    {
        Assert.False(SftpSchedulerHelper.IsUltimoHorarioDoDia("7:00,15:00", "7:00"));
    }
}
