using System; // Necessário para DateTime, se adicionado no futuro

namespace MetalFlowScheduler.Api.Application.Dtos.Auth
{
    /// <summary>
    /// DTO para a resposta de login bem-sucedida.
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// O token JWT gerado.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// O ID do usuário autenticado.
        /// </summary>
        public int UserId { get; set; } // Usando int pois ApplicationUser<int>

        /// <summary>
        /// O nome de usuário (ou email) do usuário autenticado.
        /// </summary>
        public string Username { get; set; }

        // Opcional: Adicionar outras informações úteis, como roles ou tempo de expiração do token
        // public List<string> Roles { get; set; } = new List<string>();
        // public DateTime Expiration { get; set; }
    }
}
