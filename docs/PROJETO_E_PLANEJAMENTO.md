# RunBase - Projeto e Planejamento

Este documento registra como o projeto foi pensado, o que ja foi construido e qual direcao foi definida para transformar a base inicial em um produto mais significativo.

O RunBase nasceu a partir do Olympus Admin, um painel administrativo visual, e passou a ser reposicionado como um sistema interno para gestao de clientes, planos, pedidos e acessos, com controle por roles e uma base tecnica mais robusta.

## Ideia Central

RunBase e um painel administrativo para empresas que precisam organizar operacoes recorrentes.

A proposta do produto e centralizar:

- clientes;
- planos;
- pedidos;
- usuarios internos;
- permissoes por perfil;
- indicadores operacionais;
- configuracoes da operacao.

A leitura principal do produto e:

> RunBase organiza a operacao interna de um negocio por assinatura: quem pode fazer o que, quais clientes estao ativos, quais planos existem, quais pedidos estao em andamento e como a receita esta evoluindo.

## V1 - Base Visual e Experiencia Inicial

A primeira versao foi construida como uma base visual de painel administrativo.

Ela serviu para validar a experiencia de uso, o layout geral e os principais fluxos de navegacao antes de transformar o projeto em uma aplicacao conectada a dados reais.

### O que foi feito

- Dashboard visual com cards e grafico.
- Tela de usuarios com CRUD local.
- Tela de pedidos com tabela, filtros e exportacao CSV.
- Tela de configuracoes com persistencia local.
- Tema claro/escuro persistido no navegador.
- Estrutura multi-pagina com HTML, CSS e JavaScript.

### Stack usada

- HTML
- CSS
- JavaScript
- Bootstrap
- jQuery
- Chart.js
- DataTables

### Resultado da V1

A V1 provou que o projeto tinha uma experiencia administrativa clara e uma interface suficiente para evoluir. Ainda nao era um produto completo, mas ja funcionava como prototipo navegavel e apresentavel.

## V2 - Prototipo Funcional com Backend

A segunda versao conectou o painel a uma API e adicionou persistencia, autenticacao e dados reais.

Nesta fase, o objetivo foi sair de uma demonstracao visual e transformar a experiencia em uma operacao funcional.

### O que foi feito

- Backend inicial em Node.js/Express.
- Rotas para autenticacao, usuarios, pedidos, configuracoes, analytics e notificacoes.
- Login com JWT.
- Persistencia de usuarios e pedidos.
- Configuracoes globais persistidas no backend.
- Dashboard alimentado por metricas reais.
- Estados reais de pedidos.
- Testes basicos no backend.
- Suporte a sql.js e MySQL.

### Stack usada

- Node.js
- Express
- sql.js
- MySQL
- JSON Web Token
- bcryptjs
- Socket.io

### Resultado da V2

A V2 transformou o painel em um prototipo funcional. Ela mostrou que a ideia tinha comportamento real, mas tambem deixou claro que a proxima etapa deveria ter uma arquitetura mais forte, mais escalavel e mais alinhada com o objetivo final do produto.

Por isso, essa versao foi preservada em `legacy/` como referencia de produto, UX e aprendizado tecnico.

## V3 - RunBase Foundation

A terceira versao marca a virada do projeto para RunBase.

Aqui, o projeto deixa de ser apenas uma evolucao do Olympus Admin e passa a ter uma nova fundacao tecnica, pensada para um produto mais serio: backend em ASP.NET Core, frontend futuro em Next.js, banco em Azure SQL, RBAC real e deploy em Azure.

### Decisao de arquitetura

A decisao foi reconstruir a base de forma incremental, mantendo o legado como referencia.

O backend vem primeiro porque autenticacao, roles, regras de dominio, banco e contratos da API sao a base do produto. O frontend em Next.js deve nascer depois, ja consumindo endpoints reais.

### Stack planejada

| Camada | Tecnologia |
| --- | --- |
| Frontend | Next.js, React, TypeScript |
| Backend | ASP.NET Core Web API, C# |
| Banco | Azure SQL |
| Auth | JWT, Refresh Token, RBAC |
| API Docs | Scalar |
| Deploy | Azure Static Web Apps, Azure App Service |
| CI/CD | GitHub Actions |
| Testes | xUnit, Vitest, Playwright |

### O que ja foi feito na V3

- Codigo legado movido para `legacy/`.
- Nova pasta `backend/` criada.
- Solution `.NET` criada com `RunBase.slnx`.
- Projetos criados:
  - `RunBase.Api`
  - `RunBase.Application`
  - `RunBase.Domain`
  - `RunBase.Infrastructure`
  - `RunBase.Application.Tests`
- Referencias entre camadas configuradas.
- Scalar configurado para documentacao interativa da API.
- Endpoint `/health` criado.
- OpenAPI exposto em `/openapi/v1.json`.
- Connection string preparada via `ConnectionStrings__DefaultConnection`.
- Primeiro teste unitario criado para o servico de health.
- Build e testes validados localmente.

### Validacoes feitas

- `dotnet build RunBase.slnx` passou com 0 erros.
- `dotnet test RunBase.slnx --no-build` passou com 1 teste aprovado.
- `GET /health` retornou `Healthy`.
- `/scalar/v1` retornou 200.
- `/openapi/v1.json` retornou 200.

## Roles Planejadas

O RBAC do RunBase foi pensado com 4 roles reais:

| Role | Permissoes |
| --- | --- |
| Admin | Acesso total, incluindo usuarios, roles, planos, clientes, pedidos, dashboard e configuracoes. |
| Manager | Gerencia clientes, planos e pedidos. Visualiza dashboard. Nao gerencia usuarios Admin. |
| Support | Visualiza clientes e pedidos. Atualiza status de pedidos. |
| Viewer | Acesso somente leitura ao dashboard e listas permitidas. |

## Modulos Planejados

### Auth

- Login.
- Refresh token.
- Logout/revoke.
- Usuario logado em `/api/auth/me`.
- Roles no token.

### Users

- CRUD de usuarios.
- Controle de role.
- Status ativo/inativo.

### Clients

- CRUD de clientes.
- Status `Active`, `Inactive` ou `Suspended`.
- Relacao com plano atual.

### Plans

- CRUD de planos.
- Toggle ativo/inativo.
- Preco e ciclo de cobranca.

### Orders

- CRUD de pedidos.
- Status operacional.
- Valor final preservado no pedido para manter historico financeiro.

### Dashboard

- Total de clientes.
- Pedidos por status.
- Planos ativos.
- Receita estimada.
- Ultimos pedidos.

### Settings

- Perfil do usuario logado.
- Configuracoes basicas da operacao.

## Proxima Etapa

A proxima etapa natural e iniciar o modulo de Auth na nova base .NET.

Escopo recomendado:

1. Criar entidade `User`.
2. Criar enum `UserRole` com `Admin`, `Manager`, `Support`, `Viewer`.
3. Criar enum `UserStatus` com `Active`, `Inactive`.
4. Criar endpoint de login.
5. Gerar JWT com claims de usuario e role.
6. Criar endpoint `/api/auth/me`.
7. Criar seed de usuario Admin inicial.

Depois disso, entram refresh token, logout/revoke e protecao dos primeiros endpoints por role.

## Documentos Relacionados

- [Roadmap tecnico](./RUNBASE_ROADMAP.md)
- [Inicio da implementacao](./RUNBASE_START.md)
- [README tecnico em ingles](../README.md)
