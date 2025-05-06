namespace MetalFlowScheduler.Api.Application.Exceptions
{
    /// <summary>
    /// Classe base abstrata para exceções de negócio customizadas.
    /// </summary>
    public abstract class BaseApplicationException : Exception
    {
        /// <summary>
        /// Código de erro específico para a exceção (opcional, para granularidade).
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Detalhes adicionais do erro, útil para validações (ex: erros de campos específicos).
        /// </summary>
        public Dictionary<string, string[]>? Details { get; set; }

        protected BaseApplicationException(string message, string? errorCode = null, Dictionary<string, string[]>? details = null, Exception? innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            Details = details;
        }
    }
}
