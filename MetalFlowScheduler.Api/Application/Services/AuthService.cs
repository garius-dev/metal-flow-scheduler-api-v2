﻿using AutoMapper;
using MetalFlowScheduler.Api.Application.Dtos.Auth;
using MetalFlowScheduler.Api.Application.Interfaces;
using MetalFlowScheduler.Api.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using MetalFlowScheduler.Api.Configuration;

namespace MetalFlowScheduler.Api.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<AuthService> _logger;
        private readonly JwtSecretConfig _jwtSettings;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<AuthService> logger,
            IOptions<JwtSecretConfig> jwtSettings)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> RegisterAsync(RegisterRequestDto request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
            };

            var result = await _userManager.CreateAsync(user, request.Password);


            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    await _roleManager.CreateAsync(new ApplicationRole("User"));
                }
                await _userManager.AddToRoleAsync(user, "User");
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<LoginResponseDto?> AuthenticateAsync(string username, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Falha na autenticação para o usuário: {Username}. Resultado: {SignInResult}", username, result);
                return null;
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                _logger.LogError("Usuário {Username} autenticado com sucesso, mas não encontrado no UserManager. Isso não deveria acontecer.", username);
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            var expirationTime = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

            // Obter claims do usuário, incluindo roles e claims customizadas do banco de dados
            var claims = (await _userManager.GetClaimsAsync(user)).ToList();
            claims.AddRange(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            });

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expirationTime,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new LoginResponseDto
            {
                Token = tokenString,
                UserId = user.Id,
                Username = user.UserName
            };
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> AssignRoleAsync(int userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "UserNotFound", Description = $"Usuário com ID {userId} não encontrado." }
                };
                _logger.LogWarning("Tentativa de atribuir role a usuário não encontrado: ID {UserId}", userId);
                return IdentityResult.Failed(errors.ToArray());
            }

            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "RoleNotFound", Description = $"Role '{roleName}' não encontrada." }
                };
                _logger.LogWarning("Tentativa de atribuir role inexistente '{RoleName}' ao usuário ID {UserId}", roleName, userId);
                return IdentityResult.Failed(errors.ToArray());
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                _logger.LogInformation("Role '{RoleName}' atribuída ao usuário ID {UserId}.", roleName, userId);
            }
            else
            {
                _logger.LogWarning("Falha ao atribuir role '{RoleName}' ao usuário ID {UserId}. Erros: {Errors}", roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> AddClaimAsync(int userId, string claimType, string claimValue)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "UserNotFound", Description = $"Usuário com ID {userId} não encontrado." }
                };
                _logger.LogWarning("Tentativa de adicionar claim a usuário não encontrado: ID {UserId}", userId);
                return IdentityResult.Failed(errors.ToArray());
            }

            var claim = new Claim(claimType, claimValue);

            var result = await _userManager.AddClaimAsync(user, claim);

            if (result.Succeeded)
            {
                _logger.LogInformation("Claim '{ClaimType}:{ClaimValue}' adicionada ao usuário ID {UserId}.", claimType, claimValue, userId);
            }
            else
            {
                _logger.LogWarning("Falha ao adicionar claim '{ClaimType}:{ClaimValue}' ao usuário ID {UserId}. Erros: {Errors}", claimType, claimValue, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> UpdateUserPermissionsAsync(UpdateUserPermissionsDto request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());

            if (user == null)
            {
                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "UserNotFound", Description = $"Usuário com ID {request.UserId} não encontrado." }
                };
                _logger.LogWarning("Tentativa de atualizar permissões para usuário não encontrado: ID {UserId}", request.UserId);
                return IdentityResult.Failed(errors.ToArray());
            }

            var errorsList = new List<IdentityError>();

            // --- Gerenciar Roles ---
            if (request.Roles != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var desiredRoles = request.Roles.Distinct().ToList();

                // Roles para remover: as que o usuário tem mas não estão na lista desejada
                var rolesToRemove = currentRoles.Except(desiredRoles).ToList();
                foreach (var roleToRemove in rolesToRemove)
                {
                    var removeResult = await _userManager.RemoveFromRoleAsync(user, roleToRemove);
                    if (!removeResult.Succeeded)
                    {
                        errorsList.AddRange(removeResult.Errors);
                        _logger.LogWarning("Falha ao remover role '{RoleName}' do usuário ID {UserId}. Erros: {Errors}", roleToRemove, request.UserId, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    }
                }

                // Roles para adicionar: as que estão na lista desejada mas o usuário não tem
                var rolesToAdd = desiredRoles.Except(currentRoles).ToList();
                foreach (var roleToAdd in rolesToAdd)
                {
                    // Verificar se a role a ser adicionada existe no sistema antes de tentar adicionar
                    if (!await _roleManager.RoleExistsAsync(roleToAdd))
                    {
                        errorsList.Add(new IdentityError { Code = "RoleNotFound", Description = $"Role '{roleToAdd}' não encontrada." });
                        _logger.LogWarning("Tentativa de adicionar role inexistente '{RoleName}' ao usuário ID {UserId} durante atualização de permissões.", roleToAdd, request.UserId);
                        continue;
                    }

                    var addResult = await _userManager.AddToRoleAsync(user, roleToAdd);
                    if (!addResult.Succeeded)
                    {
                        errorsList.AddRange(addResult.Errors);
                        _logger.LogWarning("Falha ao adicionar role '{RoleName}' ao usuário ID {UserId}. Erros: {Errors}", roleToAdd, request.UserId, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            // --- Gerenciar Claims ---
            if (request.Claims != null)
            {
                var currentClaims = await _userManager.GetClaimsAsync(user);
                var desiredClaims = request.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();


                // Remove todas as claims existentes para os tipos presentes na lista desejada.
                var claimTypesToManage = desiredClaims.Select(c => c.Type).Distinct().ToList();
                var claimsToRemoveExisting = currentClaims.Where(c => claimTypesToManage.Contains(c.Type)).ToList();

                foreach (var claimToRemove in claimsToRemoveExisting)
                {
                    var removeResult = await _userManager.RemoveClaimAsync(user, claimToRemove);
                    if (!removeResult.Succeeded)
                    {
                        errorsList.AddRange(removeResult.Errors);
                        _logger.LogWarning("Falha ao remover claim existente '{ClaimType}:{ClaimValue}' do usuário ID {UserId} durante atualização de permissões. Erros: {Errors}", claimToRemove.Type, claimToRemove.Value, request.UserId, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    }
                }

                // Adiciona todas as claims da lista desejada
                foreach (var claimToAdd in desiredClaims)
                {
                    var addResult = await _userManager.AddClaimAsync(user, claimToAdd);
                    if (!addResult.Succeeded)
                    {
                        errorsList.AddRange(addResult.Errors);
                        _logger.LogWarning("Falha ao adicionar claim '{ClaimType}:{ClaimValue}' ao usuário ID {UserId} durante atualização de permissões. Erros: {Errors}", claimToAdd.Type, claimToAdd.Value, request.UserId, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            if (errorsList.Any())
            {
                return IdentityResult.Failed(errorsList.ToArray());
            }

            _logger.LogInformation("Permissões (roles e claims) atualizadas com sucesso para o usuário ID {UserId}.", request.UserId);
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> RemoveRoleAsync(int userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "UserNotFound", Description = $"Usuário com ID {userId} não encontrado." }
                };
                _logger.LogWarning("Tentativa de remover role de usuário não encontrado: ID {UserId}", userId);
                return IdentityResult.Failed(errors.ToArray());
            }

            // --- Lógica de Restrição de Remoção Baseada na Role do Usuário Alvo ---
            var targetUserRoles = await _userManager.GetRolesAsync(user);


            if (roleName.Equals("Developer", StringComparison.OrdinalIgnoreCase) && (await _userManager.IsInRoleAsync(user, "Developer")))
            {

                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "PermissionDenied", Description = $"Não é possível remover a role '{roleName}' de um usuário." }
                };
                _logger.LogWarning("Tentativa de remover role protegida '{RoleName}' do usuário ID {UserId}.", roleName, userId);
                return IdentityResult.Failed(errors.ToArray());
            }


            var result = await _userManager.RemoveFromRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                _logger.LogInformation("Role '{RoleName}' removida do usuário ID {UserId}.", roleName, userId);
            }
            else
            {
                _logger.LogWarning("Falha ao remover role '{RoleName}' do usuário ID {UserId}. Erros: {Errors}", roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> RemoveClaimAsync(int userId, string claimType, string claimValue)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "UserNotFound", Description = $"Usuário com ID {userId} não encontrado." }
                };
                _logger.LogWarning("Tentativa de remover claim de usuário não encontrado: ID {UserId}", userId);
                return IdentityResult.Failed(errors.ToArray());
            }

            var claimToRemove = (await _userManager.GetClaimsAsync(user))
                                .FirstOrDefault(c => c.Type == claimType && c.Value == claimValue);

            if (claimToRemove == null)
            {
                // A claim não foi encontrada para este usuário
                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "ClaimNotFound", Description = $"Claim '{claimType}:{claimValue}' não encontrada para o usuário com ID {userId}." }
                };
                _logger.LogWarning("Tentativa de remover claim inexistente '{ClaimType}:{ClaimValue}' do usuário ID {UserId}.", claimType, claimValue, userId);
                return IdentityResult.Failed(errors.ToArray());
            }

            // --- Lógica de Restrição de Remoção Baseada na Role do Usuário Alvo ---
            var targetUserRoles = await _userManager.GetRolesAsync(user);


            if ((targetUserRoles.Contains("Developer") || targetUserRoles.Contains("Owner")))
            {

                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "PermissionDenied", Description = $"Não é possível remover claims de usuários com as roles Developer ou Owner." }
                };
                _logger.LogWarning("Tentativa de remover claim protegida '{ClaimType}:{ClaimValue}' do usuário ID {UserId} com roles protegidas.", claimType, claimValue, userId);
                return IdentityResult.Failed(errors.ToArray());
            }


            var result = await _userManager.RemoveClaimAsync(user, claimToRemove);

            if (result.Succeeded)
            {
                _logger.LogInformation("Claim '{ClaimType}:{ClaimValue}' removida do usuário ID {UserId}.", claimType, claimValue, userId);
            }
            else
            {
                _logger.LogWarning("Falha ao remover claim '{ClaimType}:{ClaimValue}' do usuário ID {UserId}. Erros: {Errors}", claimType, claimValue, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                _logger.LogWarning("Tentativa de obter roles de usuário não encontrado: ID {UserId}", userId);
                return Enumerable.Empty<string>();
            }

            return await _userManager.GetRolesAsync(user);
        }

        /// <inheritdoc/>
        public async Task<IList<Claim>> GetUserClaimsAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                _logger.LogWarning("Tentativa de obter claims de usuário não encontrado: ID {UserId}", userId);
                return new List<Claim>();
            }

            return await _userManager.GetClaimsAsync(user);
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> DeleteUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "UserNotFound", Description = $"Usuário com ID {userId} não encontrado." }
                };
                _logger.LogWarning("Tentativa de remover usuário não encontrado: ID {UserId}", userId);
                return IdentityResult.Failed(errors.ToArray());
            }

            // --- Lógica de Restrição de Remoção Baseada na Role do Usuário Alvo ---
            var targetUserRoles = await _userManager.GetRolesAsync(user);


            if (targetUserRoles.Contains("Developer"))
            {

                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "PermissionDenied", Description = "Não é possível remover usuários com a role Developer." }
                };
                _logger.LogWarning("Tentativa de remover usuário com role protegida 'Developer'. Usuário alvo ID: {UserId}", userId);
                return IdentityResult.Failed(errors.ToArray());
            }


            if ((targetUserRoles.Contains("Developer") || targetUserRoles.Contains("Owner")))
            {

                var errors = new List<IdentityError>
                {
                    new IdentityError { Code = "PermissionDenied", Description = "Não é possível remover usuários com as roles Developer ou Owner." }
                };
                _logger.LogWarning("Tentativa de remover usuário com role protegida 'Developer' ou 'Owner'. Usuário alvo ID: {UserId}", userId);
                return IdentityResult.Failed(errors.ToArray());
            }


            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuário com ID {UserId} removido com sucesso.", userId);
            }
            else
            {
                _logger.LogWarning("Falha ao remover usuário com ID {UserId}. Erros: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<ApplicationUser?> GetUserByIdAsync(int userId)
        {
            // UserManager.FindByIdAsync retorna null se o usuário não for encontrado
            return await _userManager.FindByIdAsync(userId.ToString());
        }

        /// <inheritdoc/>
        public Task LogoutAsync()
        {

            _logger.LogInformation("Logout solicitado (operação no servidor não implementada para JWT stateless).");
            return Task.CompletedTask;
        }
    }
}
