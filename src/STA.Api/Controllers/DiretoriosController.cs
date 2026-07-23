using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STA.Api.Common;

namespace STA.Api.Controllers;

[Authorize(Roles = "Admin,Operator")]
[ApiController]
[Route("api/v1/diretorios")]
public class DiretoriosController : ControllerBase
{
    [HttpPost("validar")]
    public ActionResult<ApiResponse<ValidacaoDiretorioResult>> Validar([FromBody] ValidarDiretorioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest(new ApiResponse<ValidacaoDiretorioResult>(false, null, "Caminho é obrigatório."));

        var path = request.Path.Trim();

        // Apenas verifica se existe (não cria)
        if (Directory.Exists(path))
        {
            return Ok(new ApiResponse<ValidacaoDiretorioResult>(true, new ValidacaoDiretorioResult("existe", "Diretório encontrado.", true)));
        }

        // Verifica se o parent existe (para saber se é criável)
        try
        {
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
            {
                return Ok(new ApiResponse<ValidacaoDiretorioResult>(true, new ValidacaoDiretorioResult("nao_existe", "Diretório não existe, mas pode ser criado.", false)));
            }
            return Ok(new ApiResponse<ValidacaoDiretorioResult>(true, new ValidacaoDiretorioResult("inacessivel", "Caminho inacessível ou diretório pai não existe.", false)));
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<ValidacaoDiretorioResult>(true, new ValidacaoDiretorioResult("erro", "Erro ao processar diretório.", false)));
        }
    }

    [HttpPost("criar")]
    public ActionResult<ApiResponse<ValidacaoDiretorioResult>> Criar([FromBody] ValidarDiretorioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest(new ApiResponse<ValidacaoDiretorioResult>(false, null, "Caminho é obrigatório."));

        var path = request.Path.Trim();

        if (Directory.Exists(path))
            return Ok(new ApiResponse<ValidacaoDiretorioResult>(true, new ValidacaoDiretorioResult("existe", "Diretório já existe.", true)));

        try
        {
            Directory.CreateDirectory(path);
            return Ok(new ApiResponse<ValidacaoDiretorioResult>(true, new ValidacaoDiretorioResult("criado", "Diretório criado com sucesso.", true)));
        }
        catch (UnauthorizedAccessException)
        {
            return Ok(new ApiResponse<ValidacaoDiretorioResult>(true, new ValidacaoDiretorioResult("sem_permissao", "Sem permissão para criar.", false)));
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<ValidacaoDiretorioResult>(true, new ValidacaoDiretorioResult("erro", "Erro ao processar diretório.", false)));
        }
    }
}

public record ValidarDiretorioRequest(string Path);
public record ValidacaoDiretorioResult(string Status, string Mensagem, bool Ok);
