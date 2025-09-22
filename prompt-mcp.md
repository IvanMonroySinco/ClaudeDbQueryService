Eres un desarrollador senior experto en IA. Tu tarea es modificar este microservicio para que act�e como intermediario entre una interfaz de usuario y un MCP (Model Context Protocol) existente.

Detalles del proyecto:
- El microservicio ya existe y sigue la estructura est�ndar del equipo (Clean Architecture, DDD, CQRS)
- El MCP ya est� implementado y se conecta a una base de datos
- El MCP posee varios tools que permiten realizar consultas a la base de datos para generar informes
- El MCP ya ha sido probado con Claude Desktop, demostrando precisi�n en sus respuestas
- Es fundamental que el microservicio no dependa de la interfaz de Claude Desktop
- El c�digo fuente del MCP est� ubicado en: C:\FuentesNET3.0\Azure\MaquinariaEquipos.MCP
- El microservicio debe soportar comunicaci�n con MCP mediante stdio transport en lugar de HTTP

Requisitos del microservicio:
1. Ser el backend de una interfaz propia
2. Recibir solicitudes de usuario a trav�s del endpoint espec�fico (/query)
3. Procesar estas solicitudes de forma independiente (sin usar Claude Desktop)
4. Posibilidad de agregar contexto adicional o transformar la solicitud a un lenguaje natural m�s limpio
5. Retornar respuestas estructuradas basadas en las herramientas del MCP
6. Implementar soporte para conexi�n con MCP mediante stdio transport
7. Utilizar las tools del MCP al procesar solicitudes en el endpoint /query
8. Permitir configuraci�n de la ruta al ejecutable del MCP que utiliza stdio (ej: C:\FuentesNET3.0\Azure\MaquinariaEquipos.MCP\bin\Debug\net8.0\MaquinariaEquipos.MCP.exe)

Tecnolog�a y estructura:
- El microservicio ya existe y se basa en una estructura est�ndar utilizada por el equipo
- Debe mantener la implementaci�n de Clean Architecture, DDD y CQRS
- **El c�digo base actual contiene ejemplos de implementaci�n que deben ser utilizados como referencia para:**
  - Estructuraci�n de Entities
  - Implementaci�n de Servicios
  - Organizaci�n de capas seg�n los patrones establecidos
- Implementar un adaptador para comunicaci�n con MCP mediante stdio

Objetivo final:
Modificar el microservicio existente para que permita a los usuarios obtener informaci�n de la base de datos a trav�s de una interfaz propia, utilizando el MCP existente como capa de acceso a datos mediante stdio transport, manteniendo coherencia con las convenciones de arquitectura del equipo.