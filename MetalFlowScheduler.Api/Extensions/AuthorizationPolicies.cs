using Microsoft.AspNetCore.Authorization;

namespace MetalFlowScheduler.Api.Extensions
{
    public static class AuthorizationPolicies
    {
        public static void ConfigurePolicies(AuthorizationOptions options)
        {
            //options.AddPolicy("AdminGerenciaGeral", policy =>
            //    policy.RequireRole("Admin")
            //          .RequireClaim("Departamento", "Gerência Geral"));

            //options.AddPolicy("AdminOnly", policy =>
            //    policy.RequireRole("Admin"));

            options.AddPolicy("LoggedInOnly", policy =>
                policy.RequireAuthenticatedUser());

            // Política "CanAssignRoles" para permitir:
            // - Usuários autenticados E (
            //   - Tenham a role "Owner" OU "Developer"
            //   - OU Tenham a role "Admin" E a claim "Departamento" com valor "Planejamento"
            // )
            options.AddPolicy("CanAssignRoles", policy =>
                policy.RequireAuthenticatedUser() // Exige que o usuário esteja autenticado
                      .RequireAssertion(context => // Usa RequireAssertion para lógica OR complexa
                      {
                          // Condição 1: Tem a role "Owner" OU "Developer"
                          bool isOwnerOrDeveloper = context.User.IsInRole("Owner") || context.User.IsInRole("Developer");

                          // Condição 2: Tem a role "Admin" E a claim "Departamento" com valor "Planejamento"
                          bool isAdminAndPlanning = context.User.IsInRole("Admin") &&
                                                    context.User.HasClaim("Departamento", "Planejamento");

                          // Retorna true se qualquer uma das condições (ou ambas) for verdadeira
                          return isOwnerOrDeveloper || isAdminAndPlanning;
                      })
            );

            // ** Nova Política: CanDeleteUsers **
            // Permite:
            // - Developer (qualquer um)
            // - Owner (qualquer um, exceto Developer)
            // - Admin (qualquer um, exceto Developer e Owner)
            // NOTA: Esta política verifica as roles do USUÁRIO QUE INICIA A REQUISIÇÃO (context.User).
            // Para verificar as roles do USUÁRIO ALVO (quem está sendo deletado),
            // seria necessário um Authorization Handler customizado que receba o ID do usuário alvo como recurso.
            options.AddPolicy("CanDeleteUsers", policy =>
                 policy.RequireAuthenticatedUser()
                       .RequireAssertion(context =>
                       {
                           // Usuário que inicia a requisição
                           var initiatingUser = context.User;

                           // Lógica baseada nas roles do usuário que INICIA a requisição
                           bool isDeveloper = initiatingUser.IsInRole("Developer");
                           bool isOwner = initiatingUser.IsInRole("Owner");
                           bool isAdmin = initiatingUser.IsInRole("Admin");

                           // Implementação simplificada: Apenas verifica a role do usuário que inicia a requisição.
                           // ** ISTO NÃO IMPLEMENTA A LÓGICA "exceto Developer/Owner" baseada no USUÁRIO ALVO **
                           // Para isso, um Authorization Handler customizado é necessário.
                           return isDeveloper || isOwner || isAdmin;
                       })
            );

            // ** Nova Política: CanRemoveRoles **
            // Permite:
            // - Developer (qualquer um)
            // - Owner (qualquer um, exceto Developer)
            // NOTA: Esta política verifica as roles do USUÁRIO QUE INICIA A REQUISIÇÃO (context.User).
            // Para verificar as roles do USUÁRIO ALVO (de quem a role está sendo removida),
            // seria necessário um Authorization Handler customizado.
            options.AddPolicy("CanRemoveRoles", policy =>
                 policy.RequireAuthenticatedUser()
                       .RequireAssertion(context =>
                       {
                           // Usuário que inicia a requisição
                           var initiatingUser = context.User;

                           // Lógica baseada nas roles do usuário que INICIA a requisição
                           bool isDeveloper = initiatingUser.IsInRole("Developer");
                           bool isOwner = initiatingUser.IsInRole("Owner");

                           // Implementação simplificada: Apenas verifica a role do usuário que inicia a requisição.
                           // ** ISTO NÃO IMPLEMENTA A LÓGICA "exceto Developer" baseada no USUÁRIO ALVO **
                           // Para isso, um Authorization Handler customizado é necessário.
                           return isDeveloper || isOwner;
                       })
            );

            // ** Nova Política: CanRemoveClaims **
            // Permite:
            // - Developer (qualquer um)
            // - Owner (qualquer um, exceto Developer)
            // - Admin (qualquer um, exceto Developer e Owner)
            // NOTA: Esta política verifica as roles do USUÁRIO QUE INICIA A REQUISIÇÃO (context.User).
            // Para verificar as roles do USUÁRIO ALVO (de quem a claim está sendo removida),
            // seria necessário um Authorization Handler customizado.
            options.AddPolicy("CanRemoveClaims", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context =>
                      {
                          // Usuário que inicia a requisição
                          var initiatingUser = context.User;

                          // Lógica baseada nas roles do usuário que INICIA a requisição
                          bool isDeveloper = initiatingUser.IsInRole("Developer");
                          bool isOwner = initiatingUser.IsInRole("Owner");
                          bool isAdmin = initiatingUser.IsInRole("Admin");

                          // Implementação simplificada: Apenas verifica a role do usuário que inicia a requisição.
                          // ** ISTO NÃO IMPLEMENTA A LÓGICA "exceto Developer/Owner" baseada no USUÁRIO ALVO **
                          // Para isso, um Authorization Handler customizado é necessário.
                          return isDeveloper || isOwner || isAdmin;
                      })
           );


            // Exemplo de política para deletar itens (combinando Role e potencialmente Claims/Areas)
            // Mantenha ou adapte esta política conforme suas regras de negócio
            options.AddPolicy("CanDeleteItems", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Admin") // Exige a role "Admin"
                                            // E exige que o usuário Admin seja de uma dessas áreas/departamentos (se modelado como Claim)
                                            // .RequireClaim("Area", "Planejamento", "Owner", "Developer")
                                            // OU, se "Planejamento", "Owner", "Developer" forem Roles separadas e qualquer uma delas com "Admin" puder deletar:
                      .RequireAssertion(context =>
                          context.User.IsInRole("Admin") &&
                          (context.User.IsInRole("Planejamento") || context.User.IsInRole("Owner") || context.User.IsInRole("Developer"))
                      )
            // Se a regra for simplesmente "Admin" OU "Planejamento" OU "Owner" OU "Developer"
            // .RequireRole("Admin", "Planejamento", "Owner", "Developer") // Use esta se for OU
            );

            // Adicione outras políticas aqui
        }
    }
}
