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
- [x] `dotnet test RunBase.slnx --no-build` passou com 29 testes aprovados.
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

- CRUD de planos.
- [x] Estagios iniciais definidos: `Trial`, `Free`, `Plus` e `Premium`.
- [x] Datas de cobranca iniciadas no dominio para planos pagos.
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

### Dados Demo, Privacidade e Interacoes

- [ ] Geracao de clientes e assinaturas com dados sinteticos.
- [ ] Origem dos dados separada entre `Demo`, `Manual` e `Imported`.
- [ ] Mascaramento padrao para email, telefone e documentos.
- [ ] Criptografia de dados sensiveis em repouso.
- [ ] Permissao sensivel separada do RBAC administrativo.
- [ ] Audit log para revelacao de dados sensiveis.
- [ ] Campanhas de notificacao para promocao, cobranca a vencer e cobranca em atraso.

## Proxima Etapa

A proxima etapa natural e consolidar os modulos operacionais da V3 na nova base .NET.

Escopo recomendado:

1. Trocar o hasher temporario por hash de senha adequado.
2. Persistir usuarios e tokens em banco.
3. Preparar CRUD inicial de usuarios com escolha explicita de role.
4. Aplicar policies nos CRUDs conforme cada modulo nascer.
5. Depois da fundacao backend, criar a aba de dados demo, privacidade e interacoes.

Depois disso, a aba de dados demo, privacidade e interacoes entra para deixar o produto demonstravel com seguranca.

## Documentos Relacionados

- [Roadmap tecnico](./RUNBASE_ROADMAP.md)
- [Inicio da implementacao](./RUNBASE_START.md)
- [README tecnico em ingles](../README.md)
