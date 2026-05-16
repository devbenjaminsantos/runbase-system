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

- [x] Dashboard visual com cards e grafico.
- [x] Tela de usuarios com CRUD local.
- [x] Tela de pedidos com tabela, filtros e exportacao CSV.
- [x] Tela de configuracoes com persistencia local.
- [x] Tema claro/escuro persistido no navegador.
- [x] Estrutura multi-pagina com HTML, CSS e JavaScript.

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

- [x] Backend inicial em Node.js/Express.
- [x] Rotas para autenticacao, usuarios, pedidos, configuracoes, analytics e notificacoes.
- [x] Login com JWT.
- [x] Persistencia de usuarios e pedidos.
- [x] Configuracoes globais persistidas no backend.
- [x] Dashboard alimentado por metricas reais.
- [x] Estados reais de pedidos.
- [x] Testes basicos no backend.
- [x] Suporte a sql.js e MySQL.

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

- [x] Codigo legado movido para `legacy/`.
- [x] Nova pasta `backend/` criada.
- [x] Solution `.NET` criada com `RunBase.slnx`.
- [x] Projetos criados:
  - [x] `RunBase.Api`
  - [x] `RunBase.Application`
  - [x] `RunBase.Domain`
  - [x] `RunBase.Infrastructure`
  - [x] `RunBase.Application.Tests`
- [x] Referencias entre camadas configuradas.
- [x] Scalar configurado para documentacao interativa da API.
- [x] Endpoint `/health` criado.
- [x] OpenAPI exposto em `/openapi/v1.json`.
- [x] Connection string preparada via `ConnectionStrings__DefaultConnection`.
- [x] Pipeline basico de build/test criado com GitHub Actions.
- [x] Primeiro teste unitario criado para o servico de health.
- [x] Build e testes validados localmente.
- [x] Entidade `User` criada.
- [x] Enums `UserRole` e `UserStatus` criados.
- [x] Login basico criado em `/api/auth/login`.
- [x] JWT emitido com claims de usuario e role.
- [x] Endpoint protegido `/api/auth/me` criado.
- [x] Seed de usuario Admin inicial criado.
- [x] Testes unitarios iniciais do Auth criados.
- [x] Refresh token criado.
- [x] Rotacao de refresh token implementada.
- [x] Logout/revoke de refresh token implementado.
- [x] Policies de autorizacao por role criadas.

### Validacoes feitas

- [x] `dotnet build RunBase.slnx` passou com 0 erros.
- [x] `dotnet test RunBase.slnx` passou com 80 testes aprovados.
- [x] `GET /health` retornou `Healthy`.
- [x] `/scalar/v1` retornou 200.
- [x] `/openapi/v1.json` retornou 200.
- [x] `POST /api/auth/login` retornou token JWT.
- [x] `POST /api/auth/refresh` retornou novo par de tokens.
- [x] Reutilizar refresh token antigo retornou 401.
- [x] `POST /api/auth/logout` revogou o refresh token e retornou 204.
- [x] Usar refresh token revogado apos logout retornou 401.
- [x] `GET /api/auth/me` retornou o usuario autenticado.

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

- [x] Login.
- [x] Refresh token.
- [x] Logout/revoke.
- [x] Usuario logado em `/api/auth/me`.
- [x] Roles no token.
- [x] Policies por role.

### Users

- [x] CRUD inicial de usuarios.
- [x] Controle de role escolhido explicitamente.
- [x] Status ativo/inativo.

### Clients

- [x] CRUD inicial de clientes.
- [x] Status `Active`, `Inactive` ou `Suspended`.
- [x] Relacao com plano atual.

### Plans

- [x] CRUD inicial de planos.
- [x] Estagios iniciais definidos: `Trial`, `Free`, `Plus` e `Premium`.
- [x] Datas de cobranca iniciadas no dominio para planos pagos.
- [x] Toggle ativo/inativo.
- [x] Preco e ciclo de cobranca.

### Orders

- [x] CRUD inicial de pedidos.
- [x] Status operacional.
- [x] Valor final preservado no pedido para manter historico financeiro.

### Dashboard

- Total de clientes.
- Pedidos por status.
- Planos ativos.
- Receita estimada.
- Ultimos pedidos.

### Settings

- Perfil do usuario logado.
- Configuracoes basicas da operacao.

## V4 - Security & Privacy Foundation

A V4 sera pautada em cyberseguranca aplicada ao RunBase.

Como o projeto gerencia clientes, planos, pedidos, usuarios e futuramente dados sensiveis, a prioridade passa a ser proteger informacoes mesmo dentro do painel administrativo.

O foco da V4 sera o CID:

- Confidencialidade: dados sensiveis mascarados por padrao, criptografia em repouso e permissao explicita para revelacao.
- Integridade: validacoes, auditoria e protecao de alteracoes criticas.
- Disponibilidade: protecoes contra abuso em endpoints sensiveis, como login e operacoes administrativas.

### Security & Privacy

- [x] Hasher temporario substituido por PBKDF2 com salt por senha.
- [x] Mascaramento padrao para email, telefone e documentos.
- [x] Criptografia de dados sensiveis em repouso.
- [x] Policy `SensitiveData.View`.
- [x] Audit log para tentativa de visualizacao de dados sensiveis.
- [x] Logs sem exposicao de dados sensiveis.
- [x] Persistencia preparada com EF Core/LINQ contra SQL Injection.
- [x] Validacoes de entrada para DTOs publicos.
- [x] Rate limiting para login e endpoints sensiveis.

### Dados Demo e Interacoes

- [x] Geracao de clientes e assinaturas com dados sinteticos.
- [x] Origem dos dados separada entre `Demo`, `Manual` e `Imported`.
- [x] Campanhas de notificacao para promocao, cobranca a vencer e cobranca em atraso.

### Migracao EF Core por Modulo

- [x] Plans migrado para EF Core com fallback em memoria.
- [x] Orders migrado para EF Core com fallback em memoria.
- [x] Notification Campaigns migrado para EF Core com fallback em memoria.
- [x] Clients migrado para EF Core preservando criptografia, lookup hash e fallback em memoria.
- [x] Audit log sensivel migrado para EF Core com fallback em memoria.

### 6/6 - Auth, Users & Refresh Tokens

- [x] 6.1 - Migrar `Users` para EF Core com fallback em memoria.
- [x] 6.2 - Validar login, `/me`, RBAC e usuarios inativos com usuario persistido.
- [x] 6.3 - Migrar `Refresh Tokens` para EF Core com rotacao, revoke, logout e fallback em memoria.
- [x] 6.4 - Validacao final do fluxo completo: login, refresh, logout, roles, usuario inativo e ultimo admin ativo.

## Proxima Etapa

A proxima etapa natural e iniciar a V4 com a fundacao de seguranca e privacidade.

Escopo recomendado:

1. Aplicar mascaramento sensivel nos demais contratos conforme novos dados entrarem.
2. Criar fluxo de bloqueio persistente para reincidencia de tentativa sensivel.
3. Continuar migracao EF Core por modulo.

Depois disso, entram dados sinteticos e interacoes de cobranca/promocao com seguranca desde a base.

## Documentos Relacionados

- [Roadmap tecnico](./RUNBASE_ROADMAP.md)
- [Regras de persistencia segura](./SECURITY_PERSISTENCE.md)
- [Inicio da implementacao](./RUNBASE_START.md)
- [README tecnico em ingles](../README.md)
