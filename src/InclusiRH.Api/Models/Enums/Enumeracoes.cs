namespace InclusiRH.Api.Models.Enums;

public enum PapelColaborador
{
    Colaborador = 1,
    Gestor = 2,
    RecursosHumanos = 3,
    ComiteDei = 4,
    Administrador = 5
}

public enum StatusDenuncia
{
    Pendente = 1,
    EmAnalise = 2,
    Tratado = 3,
    Contestado = 4,
    Escalonado = 5,
    Encerrado = 6
}

public enum StatusValidacao
{
    Confirmado = 1,
    Contestado = 2
}

/// <summary>
/// Categorias de cor ou raça alinhadas ao IBGE, com opção de não informar.
/// </summary>
public enum Etnia
{
    Branca = 1,
    Preta = 2,
    Parda = 3,
    Amarela = 4,
    Indigena = 5,
    PrefiroNaoInformar = 6
}

public enum Genero
{
    Mulher = 1,
    Homem = 2,
    MulherTrans = 3,
    HomemTrans = 4,
    NaoBinario = 5,
    Outro = 6,
    PrefiroNaoInformar = 7
}

public enum OrientacaoSexual
{
    Heterossexual = 1,
    Homossexual = 2,
    Bissexual = 3,
    Assexual = 4,
    Pansexual = 5,
    Outro = 6,
    PrefiroNaoInformar = 7
}

public enum SituacaoPcd
{
    Nao = 1,
    Sim = 2,
    PrefiroNaoInformar = 3
}

public enum ContextoSocioeconomico
{
    PrefiroNaoInformar = 1,
    AteDoisSalariosMinimos = 2,
    DeDoisACincoSalariosMinimos = 3,
    DeCincoADezSalariosMinimos = 4,
    AcimaDeDezSalariosMinimos = 5
}

public enum TipoOcorrencia
{
    AssedioMoral = 1,
    AssedioSexual = 2,
    DiscriminacaoEtnicoRacial = 3,
    DiscriminacaoDeGenero = 4,
    DiscriminacaoPorOrientacaoSexual = 5,
    Capacitismo = 6,
    Retaliacao = 7,
    Outro = 8
}

/// <summary>
/// Momento do fluxo em que a evidência foi anexada.
/// </summary>
public enum OrigemEvidencia
{
    Abertura = 1,
    Contestacao = 2
}

public enum CategoriaTreinamento
{
    LetramentoEtnicoRacial = 1,
    EquidadeDeGenero = 2,
    InclusaoDePessoasComDeficiencia = 3,
    DiversidadeLgbtqiapn = 4,
    PrevencaoDeAssedio = 5,
    LgpdEPrivacidade = 6,
    LiderancaInclusiva = 7
}

public enum StatusConclusaoTreinamento
{
    NaoIniciado = 1,
    EmAndamento = 2,
    Concluido = 3
}

public static class DescricoesCategoriaTreinamento
{
    public static string Obter(CategoriaTreinamento categoria) => categoria switch
    {
        CategoriaTreinamento.LetramentoEtnicoRacial =>
            "Letramento Étnico-Racial - Leis 10.639/03 e 11.645/08",
        CategoriaTreinamento.EquidadeDeGenero => "Equidade de gênero",
        CategoriaTreinamento.InclusaoDePessoasComDeficiencia => "Inclusão de pessoas com deficiência",
        CategoriaTreinamento.DiversidadeLgbtqiapn => "Diversidade LGBTQIAPN+",
        CategoriaTreinamento.PrevencaoDeAssedio => "Prevenção ao assédio e conduta ética",
        CategoriaTreinamento.LgpdEPrivacidade => "LGPD e privacidade de dados",
        CategoriaTreinamento.LiderancaInclusiva => "Liderança inclusiva",
        _ => throw new ArgumentOutOfRangeException(nameof(categoria), categoria, "Categoria de treinamento não mapeada.")
    };
}
