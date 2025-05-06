using Microsoft.AspNetCore.Authorization;

namespace MetalFlowScheduler.Api.Extensions
{
    public static class AuthorizationPolicies
    {
        public static void ConfigurePolicies(AuthorizationOptions options)
        {

            options.AddPolicy("LoggedInOnly", policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy("CanAssignRoles", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context =>
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

                           return isDeveloper || isOwner || isAdmin;
                       })
            );


            options.AddPolicy("CanRemoveRoles", policy =>
                 policy.RequireAuthenticatedUser()
                       .RequireAssertion(context =>
                       {
                           // Usuário que inicia a requisição
                           var initiatingUser = context.User;

                           // Lógica baseada nas roles do usuário que INICIA a requisição
                           bool isDeveloper = initiatingUser.IsInRole("Developer");
                           bool isOwner = initiatingUser.IsInRole("Owner");

                           return isDeveloper || isOwner;
                       })
            );


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


                          return isDeveloper || isOwner || isAdmin;
                      })
           );


            options.AddPolicy("CanDeleteItems", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Admin")
                      .RequireAssertion(context =>
                          context.User.IsInRole("Admin") &&
                          (context.User.IsInRole("Planejamento") || context.User.IsInRole("Owner") || context.User.IsInRole("Developer"))
                      )
            );

            // Adicione outras políticas aqui
        }
    }
}
