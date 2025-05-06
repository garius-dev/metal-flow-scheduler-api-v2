namespace MetalFlowScheduler.Api.Application.Exceptions
{
    /// <summary>
    /// Exceção lançada quando um usuário não tem permissão para realizar uma ação específica.
    /// Corresponde tipicamente a um status HTTP 403 Forbidden.
    /// </summary>
    public class PermissionDeniedException : BaseApplicationException
    {
        public PermissionDeniedException(string message = "Você não tem permissão para realizar esta ação.", string? errorCode = null, Exception? innerException = null)
            : base(message, errorCode, null, innerException)
        {
        }
    }
}
