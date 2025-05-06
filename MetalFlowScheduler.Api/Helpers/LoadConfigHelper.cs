using MetalFlowScheduler.Api.Configuration;
using Newtonsoft.Json;

namespace MetalFlowScheduler.Api.Helpers
{
    public static class LoadConfigHelper
    {
        public static T LoadConfigFromSecret<T>(IConfiguration configuration, string secretName)
        {
            var secretJson = configuration[secretName] ?? throw new InvalidOperationException($"Segredo '{secretName}' não encontrado ou vazio na configuração.");

            try
            {
                var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                return JsonConvert.DeserializeObject<T>(secretJson, settings)
                       ?? throw new InvalidOperationException($"Falha ao desserializar o JSON do segredo '{secretName}' em {typeof(T).Name}. O JSON desserializado resultou em null.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Erro ao desserializar o JSON do segredo '{secretName}'. Verifique o formato.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ocorreu um erro inesperado ao carregar o segredo '{secretName}'.", ex);
            }
        }

        // Novo método para selecionar a Connection String pelo ambiente
        public static ConnectionStringConfig GetConnectionStringForEnvironment(ConnectionStringsConfig config, IHostEnvironment environment)
        {
            if (config?.ConnectionStrings == null || !config.ConnectionStrings.Any())
            {
                throw new InvalidOperationException("A configuração de ConnectionStrings está vazia ou nula.");
            }

            // Tenta encontrar a string de conexão para o ambiente atual
            var connectionString = config.ConnectionStrings
                                         .FirstOrDefault(cs => cs.Environment.Equals(environment.EnvironmentName, StringComparison.OrdinalIgnoreCase));

            if (connectionString == null)
            {
                // Se não encontrar uma para o ambiente específico, tenta encontrar uma marcada como 'Default' (opcional)
                connectionString = config.ConnectionStrings
                                         .FirstOrDefault(cs => cs.Environment.Equals("Default", StringComparison.OrdinalIgnoreCase));

                if (connectionString == null)
                {
                    throw new InvalidOperationException($"Connection string para o ambiente '{environment.EnvironmentName}' não encontrada na configuração de secrets e nenhuma connection string 'Default' foi fornecida.");
                }
                // Logar um aviso se estiver usando o padrão
                // Para logar dentro de um helper, você precisaria injetar ILogger aqui,
                // ou retornar a string e logar no Program.cs após receber.
                // Para manter o helper mais puro, vamos lançar a exceção clara.
            }

            // Validação básica da string de conexão encontrada
            if (string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new InvalidOperationException($"A connection string encontrada para o ambiente '{connectionString.Environment}' está vazia ou nula.");
            }

            return connectionString;
        }
    }
}
