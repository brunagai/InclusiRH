namespace InclusiRH.Api.Services;

public sealed class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string message)
        : base(message)
    {
    }
}
