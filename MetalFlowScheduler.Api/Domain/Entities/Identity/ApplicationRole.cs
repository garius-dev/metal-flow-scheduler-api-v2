using Microsoft.AspNetCore.Identity;

namespace MetalFlowScheduler.Api.Domain.Entities.Identity
{
    // Sua classe de role customizada, herdando de IdentityRole
    // IdentityRole já inclui propriedades como Name
    public class ApplicationRole : IdentityRole<int> // Usamos int para o tipo da chave primária
    {
        // Adicione propriedades customizadas para roles aqui, se necessário.
        // Exemplo:
        // public string Description { get; set; }

        // Construtor padrão necessário para Identity e EF Core
        public ApplicationRole() : base() { }

        // Construtor com nome da role
        public ApplicationRole(string roleName) : base(roleName) { }
    }
}
