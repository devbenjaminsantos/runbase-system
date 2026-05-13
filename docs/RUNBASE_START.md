# RunBase - Inicio da Implementacao

## Decisao inicial

O RunBase deve comecar pelo backend em ASP.NET Core Web API.

Motivo: autenticacao, roles, regras de dominio, banco e contratos da API sao a base do produto. O frontend em Next.js deve nascer logo depois, ja consumindo endpoints reais, sem criar uma segunda camada de mocks.

## Recorte da primeira entrega

### Milestone 1A - Backend Foundation

Objetivo: criar uma API nova, limpa e verificavel, sem ainda implementar todos os modulos do produto.

Entregaveis:

- Solution `.NET` criada.
- Projetos base criados:
  - `RunBase.Api`
  - `RunBase.Application`
  - `RunBase.Domain`
  - `RunBase.Infrastructure`
  - `RunBase.Application.Tests`
- Referencias entre projetos configuradas.
- Scalar habilitado para documentacao interativa da API.
- Endpoint `/health` respondendo.
- Configuracao preparada para connection string via environment variable.
- Build local passando.

Criterio de pronto:

- `dotnet build` passa.
- API sobe localmente.
- Scalar abre em ambiente de desenvolvimento.
- `/health` retorna status da aplicacao.

## Estrutura inicial proposta

```text
RunBase/
  legacy/
    frontend-static/
    backend-node/
  backend/
    RunBase.slnx
    src/
      RunBase.Api/
      RunBase.Application/
      RunBase.Domain/
      RunBase.Infrastructure/
    tests/
      RunBase.Application.Tests/
  frontend/
    app/
    components/
    features/
    lib/
    types/
  docs/
    RUNBASE_ROADMAP.md
    RUNBASE_START.md
```

## Como lidar com o codigo atual

O codigo atual deve virar referencia de produto e UX, nao a base tecnica final.

Plano recomendado:

1. Criar a nova stack em pastas paralelas.
2. Validar a base ASP.NET Core.
3. Validar a base Next.js.
4. Depois mover o codigo antigo para `legacy/`, evitando misturar estruturas durante o nascimento da nova stack.

Essa ordem reduz risco porque o projeto atual continua acessivel enquanto a nova fundacao e criada.

## Primeiro modulo depois da fundacao

Depois da Milestone 1A, o proximo modulo deve ser Auth.

Escopo do Auth inicial:

- entidade `User`;
- enum `UserRole` com `Admin`, `Manager`, `Support`, `Viewer`;
- enum `UserStatus` com `Active`, `Inactive`;
- endpoint de login;
- JWT com claims de usuario e role;
- endpoint `/api/auth/me`;
- seed de usuario Admin inicial.

Refresh token e logout/revoke entram logo em seguida, ainda dentro da milestone de Auth, mas depois do login basico estar funcionando.

## Primeira ordem de execucao

1. Criar solution e projetos do backend.
2. Adicionar referencias entre camadas.
3. Configurar Scalar e health check.
4. Criar primeiro teste unitario simples em `RunBase.Application.Tests`.
5. Rodar build e testes.
6. Registrar no README como rodar o backend novo.

## Decisao de escopo

Nesta fase, nao entram:

- CRUD de Users;
- Clients;
- Plans;
- Orders;
- frontend Next.js;
- Azure SQL real;
- deploy.

Esses itens dependem da fundacao estar estavel.
