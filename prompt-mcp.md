Eres un desarrollador senior experto en IA. Tu tarea es modificar este microservicio para que actúe como intermediario entre una interfaz de usuario y un MCP (Model Context Protocol) existente.

Detalles del proyecto:
- El microservicio ya existe y sigue la estructura estándar del equipo (Clean Architecture, DDD, CQRS)
- El MCP ya está implementado y se conecta a una base de datos
- El MCP posee varios tools que permiten realizar consultas a la base de datos para generar informes
- El MCP ya ha sido probado con Claude Desktop, demostrando precisión en sus respuestas
- Es fundamental que el microservicio no dependa de la interfaz de Claude Desktop
- El código fuente del MCP está ubicado en: C:\FuentesNET3.0\Azure\MaquinariaEquipos.MCP
- El microservicio debe soportar comunicación con MCP mediante stdio transport en lugar de HTTP

Requisitos del microservicio:
1. Ser el backend de una interfaz propia
2. Recibir solicitudes de usuario a través del endpoint específico (/query)
3. Procesar estas solicitudes de forma independiente (sin usar Claude Desktop)
4. Posibilidad de agregar contexto adicional o transformar la solicitud a un lenguaje natural más limpio
5. Retornar respuestas estructuradas basadas en las herramientas del MCP
6. Implementar soporte para conexión con MCP mediante stdio transport
7. Utilizar las tools del MCP al procesar solicitudes en el endpoint /query
8. Permitir configuración de la ruta al ejecutable del MCP que utiliza stdio (ej: C:\FuentesNET3.0\Azure\MaquinariaEquipos.MCP\bin\Debug\net8.0\MaquinariaEquipos.MCP.exe)

Tecnología y estructura:
- El microservicio ya existe y se basa en una estructura estándar utilizada por el equipo
- Debe mantener la implementación de Clean Architecture, DDD y CQRS
- **El código base actual contiene ejemplos de implementación que deben ser utilizados como referencia para:**
  - Estructuración de Entities
  - Implementación de Servicios
  - Organización de capas según los patrones establecidos
- Implementar un adaptador para comunicación con MCP mediante stdio

Objetivo final:
Modificar el microservicio existente para que permita a los usuarios obtener información de la base de datos a través de una interfaz propia, utilizando el MCP existente como capa de acceso a datos mediante stdio transport, manteniendo coherencia con las convenciones de arquitectura del equipo.