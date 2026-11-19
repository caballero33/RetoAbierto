# 🚀 RetoAbierto - Plataforma SaaS Multi-empresa (B2B)

Plataforma transaccional bajo el modelo SaaS (Software as a Service) Multi-empresa diseñada para conectar a empresas que tienen retos reales con estudiantes universitarios que postulan soluciones en equipo.

---

## 🏛️ Arquitectura del Sistema (Fase 2)

El backend está desarrollado en **C# y .NET** con base de datos **PostgreSQL**, respetando:
* **Clean Architecture en 4 capas:** `Domain`, `Application`, `Infrastructure`, `API`.
* **Vertical Slicing:** Organización por funcionalidades de negocio (*Features*) en la capa de Aplicación.
* **Aislamiento Multi-empresa:** Implementación de **Global Query Filters (`HasQueryFilter`)** en Entity Framework Core.
* **Seguridad IAM:** Autenticación mediante **Tokens JWT** con claims de contexto (`sub`, `name`, `rol`, `empresaId`, `universidadId`, `estudianteId`) y contraseñas hasheadas con **BCrypt**.
* **Validación Semántica:** Reglas declarativas con **FluentValidation** interceptadas por un **Middleware global de excepciones**.
* **Motor de Matching Explicable:** Algoritmo ponderado $100 \times (0.5H + 0.3D + 0.2E)$ para recomendación de perfiles compatibles.

---

## 📁 Estructura del Repositorio

```plaintext
├── backend/
│   ├── RetoAbierto.sln
│   ├── RetoAbierto.Domain/               # 23 entidades 3FN, enums y contratos base
│   ├── RetoAbierto.Application/          # DTOs, FluentValidation, servicios y matching
│   ├── RetoAbierto.Infrastructure/       # EF Core DbContext, HasQueryFilter, JWT y BCrypt
│   └── RetoAbierto.API/                  # Controladores REST, Middleware y Scalar OpenAPI
├── infra/
│   ├── docker-compose.yml                # Contenedor de PostgreSQL 16
│   └── .env.example
└── docs_presentacion/                    # Documentación técnica y guía de pruebas
```

---

## 🚀 Puesta en Marcha Local

### 1. Clonar el repositorio
```bash
git clone https://github.com/caballero33/RetoAbierto.git
cd RetoAbierto
```

### 2. Levantar la base de datos con Docker
```bash
cd infra
cp .env.example .env
docker-compose up -d
```

### 3. Configurar y arrancar el Backend
```bash
cd ../backend/RetoAbierto.API
cp .env.example .env
dotnet run
```

### 4. Documentación interactiva
Abre en tu navegador:
```
http://localhost:5000/scalar/v1
```

## Pruebas de seguridad multiempresa

El proyecto `backend/RetoAbierto.IntegrationTests` comprueba el aislamiento entre empresas y los accesos autorizados de estudiantes y universidades, además del inicio de sesión y el cambio de empresa. Usa datos ficticios en SQLite en memoria; no necesita Docker ni modifica la base PostgreSQL del equipo.

Desde la raíz del repositorio, ejecuta:

```bash
dotnet test backend/RetoAbierto.sln
```

Se requiere el SDK de .NET 8 o compatible y acceso a NuGet para restaurar los paquetes la primera vez. Estas pruebas no sustituyen la verificación HTTP de la API ni la ejecución contra PostgreSQL.
