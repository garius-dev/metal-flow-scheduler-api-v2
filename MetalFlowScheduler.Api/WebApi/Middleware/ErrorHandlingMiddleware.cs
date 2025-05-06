﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using MetalFlowScheduler.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using MetalFlowScheduler.Api.Application.Exceptions;

namespace MetalFlowScheduler.Api.WebApi.Middleware
{
    /// <summary>
    /// Middleware para tratamento global de exceções e retorno de respostas HTTP padronizadas.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu uma exceção não tratada durante a requisição: {Message}", ex.Message);
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json"; // Padrão Problem Details
            var statusCode = HttpStatusCode.InternalServerError; // Padrão para erros não tratados
            var title = "Ocorreu um erro interno no servidor.";
            var detail = exception.Message;
            string? errorCode = null;
            object? errors = null; // Para detalhes de validação

            // Mapeia exceções customizadas para códigos HTTP específicos
            switch (exception)
            {
                case NotFoundException notFoundException:
                    statusCode = HttpStatusCode.NotFound; // 404
                    title = "Recurso não encontrado.";
                    detail = notFoundException.Message;
                    errorCode = notFoundException.ErrorCode;
                    break;
                case ValidationException validationException:
                    statusCode = HttpStatusCode.BadRequest; // 400
                    title = "Falha na validação da requisição.";
                    detail = validationException.Message;
                    errorCode = validationException.ErrorCode;
                    errors = validationException.Details; // Inclui detalhes da validação
                    break;
                case ConflictException conflictException:
                    statusCode = HttpStatusCode.Conflict; // 409
                    title = "Conflito ao processar a requisição.";
                    detail = conflictException.Message;
                    errorCode = conflictException.ErrorCode;
                    break;
                case PermissionDeniedException permissionDeniedException:
                    statusCode = HttpStatusCode.Forbidden; // 403
                    title = "Permissão negada.";
                    detail = permissionDeniedException.Message;
                    errorCode = permissionDeniedException.ErrorCode;
                    break;
                // Adicione outros tipos de exceção customizadas aqui
                // case CustomBusinessException businessException:
                //     statusCode = HttpStatusCode.BadRequest; // ou outro código apropriado
                //     title = "Erro de negócio.";
                //     detail = businessException.Message;
                //     errorCode = businessException.ErrorCode;
                //     break;
                default:
                    // Para exceções não customizadas (erros inesperados)
                    // Em produção, evite expor detalhes internos da exceção padrão.
                    // Apenas o título genérico e o status 500 são suficientes.
                    // detail = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment() ? exception.ToString() : "Ocorreu um erro inesperado.";
                    detail = "Ocorreu um erro inesperado."; // Mantendo simples por enquanto
                    break;
            }

            // Cria um objeto ProblemDetails para a resposta
            var problemDetails = new ProblemDetails
            {
                Status = (int)statusCode,
                Title = title,
                Detail = detail,
                Type = $"https://httpstatuses.com/{(int)statusCode}", // Link opcional para descrição do status
                Instance = context.Request.Path, // Opcional: caminho da requisição
            };

            // Adiciona detalhes específicos para ValidationException
            if (errors != null)
            {
                // ProblemDetails suporta extensões, podemos adicionar um campo 'errors'
                problemDetails.Extensions.Add("errors", errors);
            }

            // Adiciona código de erro customizado se existir
            if (!string.IsNullOrEmpty(errorCode))
            {
                problemDetails.Extensions.Add("errorCode", errorCode);
            }


            var result = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(result);
        }
    }

    /// <summary>
    /// Extensão para facilitar a adição do middleware.
    /// </summary>
    public static class ErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
