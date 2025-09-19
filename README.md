# MCP Server - Claude API Integration

Este es un servidor MCP (Model Context Protocol) que integra con la API de Claude de Anthropic para procesar consultas de lenguaje natural y proporcionar respuestas estructuradas.

## Características

- **Protocolo MCP 2025-06-18**: Implementación completa de la especificación MCP más reciente
- **Integración Claude API**: Comunicación directa con la API de Anthropic Claude
- **Herramientas MCP**: database-tool, general-assistant, data-analysis
- **API RESTful**: Endpoints HTTP para interacción con el microservicio DatamartMYE
- **Swagger Documentation**: Documentación automática de la API
- **Logging estructurado**: Registro detallado con Serilog
- **Health Checks**: Monitoreo del estado del servidor y Claude API

## Arquitectura

```
[DatamartMYE:5189] → [MCPServer:8080] → [Claude API]
```

## Instalación y Configuración

### 1. Configurar API Key de Claude

Edita `appsettings.json`:

```json
{
  "Claude": {
    "ApiUrl": "https://api.anthropic.com/v1/messages",
    "ApiKey": "YOUR_CLAUDE_API_KEY_HERE",
    "Model": "claude-3-5-sonnet-20241022",
    "MaxTokens": 4096,
    "TimeoutSeconds": 60
  }
}
```

### 2. Ejecutar el servidor

```bash
cd C:/FuentesNET3.0/Azure/MCPServer
dotnet run
```

El servidor se ejecutará en: `http://localhost:8080`

## Endpoints API

### Health Check
```
GET /health
```
Verifica el estado del servidor y la conectividad con Claude API.

**Respuesta exitosa:**
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "server": "DatamartMYE-MCP-Server",
  "timestamp": "2025-01-18T10:30:00Z"
}
```

### Herramientas Disponibles
```
GET /tools
```
Obtiene la lista de herramientas MCP disponibles.

**Respuesta:**
```json
{
  "tools": [
    {
      "name": "database-tool",
      "description": "Execute database queries and retrieve data from SQL databases",
      "enabled": true,
      "inputSchema": { ... },
      "outputSchema": { ... }
    },
    {
      "name": "general-assistant",
      "description": "General purpose AI assistant for answering questions",
      "enabled": true,
      "inputSchema": { ... },
      "outputSchema": { ... }
    },
    {
      "name": "data-analysis",
      "description": "Analyze data patterns and generate insights",
      "enabled": true,
      "inputSchema": { ... },
      "outputSchema": { ... }
    }
  ],
  "server": "DatamartMYE-MCP-Server",
  "version": "1.0.0"
}
```

### Procesar Consulta
```
POST /query
```
Procesa una consulta usando Claude AI y las herramientas MCP.

**Cuerpo de la petición:**
```json
{
  "query": "¿Cuántos usuarios activos tenemos este mes?",
  "tool": "database-tool",  // opcional, se selecciona automáticamente
  "parameters": { ... },    // opcional
  "context": { ... },       // opcional
  "userId": "user123",      // opcional
  "timeout": 30            // opcional, en segundos
}
```

**Respuesta exitosa:**
```json
{
  "success": true,
  "result": {
    "sql_query": "SELECT COUNT(*) FROM users WHERE status='active' AND created_date >= '2025-01-01'",
    "results": [{"count": 1250}],
    "row_count": 1,
    "execution_time_ms": 45
  },
  "tool_used": "database-tool",
  "execution_time_ms": 1250,
  "tokens_used": {
    "input_tokens": 150,
    "output_tokens": 75,
    "total_tokens": 225
  },
  "timestamp": "2025-01-18T10:30:00Z"
}
```

### Estado de Herramienta
```
GET /tools/{toolName}/status
```
Verifica si una herramienta específica está disponible.

### Información del Servidor
```
GET /info
```
Obtiene información detallada sobre el servidor y sus capacidades.

## Herramientas MCP

### 1. database-tool
- **Propósito**: Ejecutar consultas SQL y recuperar datos
- **Entrada**: Consulta en lenguaje natural
- **Salida**: SQL generado y resultados estructurados

### 2. general-assistant
- **Propósito**: Asistente de IA de propósito general
- **Entrada**: Pregunta o solicitud
- **Salida**: Respuesta con nivel de confianza

### 3. data-analysis
- **Propósito**: Análisis de patrones y generación de insights
- **Entrada**: Datos para analizar
- **Salida**: Insights, resúmenes y recomendaciones

## Integración con DatamartMYE

El microservicio DatamartMYE está configurado para usar este servidor MCP:

1. **Configuración**: DatamartMYE apunta a `http://localhost:8080`
2. **Flujo de datos**:
   - Usuario envía consulta a DatamartMYE
   - DatamartMYE envía consulta a MCPServer
   - MCPServer procesa con Claude API
   - Respuesta se devuelve a DatamartMYE
   - DatamartMYE guarda historial y devuelve resultado

## Ejemplos de Uso

### Consulta de Base de Datos
```bash
curl -X POST "http://localhost:8080/query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Obtén todos los usuarios registrados en los últimos 7 días",
    "userId": "analyst001"
  }'
```

### Análisis de Datos
```bash
curl -X POST "http://localhost:8080/query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Analiza los patrones de uso de los últimos 3 meses",
    "tool": "data-analysis",
    "userId": "data-scientist001"
  }'
```

### Pregunta General
```bash
curl -X POST "http://localhost:8080/query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "¿Cuáles son las mejores prácticas para optimizar consultas SQL?",
    "tool": "general-assistant"
  }'
```

## Swagger UI

Accede a la documentación interactiva en: `http://localhost:8080/swagger`

## Logs y Monitoreo

El servidor usa Serilog para logging estructurado. Los logs incluyen:
- Peticiones HTTP recibidas
- Comunicación con Claude API
- Tiempo de ejecución de consultas
- Errores y excepciones
- Uso de tokens de Claude

## Manejo de Errores

### Errores Comunes

1. **API Key inválida**: Configura correctamente `Claude.ApiKey`
2. **Timeout**: Ajusta `Claude.TimeoutSeconds` según necesidad
3. **Rate limiting**: Claude API tiene límites de velocidad
4. **Query inválida**: Valida formato de consultas antes de enviar

### Códigos de Error

- `400`: Error de validación (query vacía, parámetros inválidos)
- `404`: Herramienta no encontrada
- `408`: Timeout en procesamiento
- `500`: Error interno del servidor
- `503`: Claude API no disponible

## Desarrollo y Testing

### Compilar
```bash
dotnet build
```

### Ejecutar
```bash
dotnet run
```

### Tests (Configuración básica incluida)
Los tests requieren configuración adicional de xUnit project separado.

## Configuración Avanzada

### Variables de Entorno
```bash
export CLAUDE_API_KEY="your-api-key-here"
export MCP_SERVER_PORT="8080"
export MCP_LOG_LEVEL="Information"
```

### Docker (Opcional)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY . .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MCPServer.dll"]
```

## Requisitos del Sistema

- **.NET 8.0** Runtime
- **Conexión a Internet** para Claude API
- **Puerto 8080** disponible
- **API Key de Anthropic Claude** válida

## Soporte y Contribución

Para reportar issues o contribuir mejoras, contacta al equipo de desarrollo de DatamartMYE.