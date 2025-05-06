namespace MetalFlowScheduler.Api.Application.Exceptions
{
    /// <summary>
    /// Exceção lançada quando uma requisição resulta em um conflito com o estado atual do recurso.
    /// Ex: tentar criar um recurso com um nome que já existe.
    /// Corresponde tipicamente a um status HTTP 409 Conflict.
    /// </summary>
    public class ConflictException : BaseApplicationException
    {
        public ConflictException(string message, string? errorCode = null, Exception? innerException = null)
            : base(message, errorCode, null, innerException)
        {
        }
    }
}
