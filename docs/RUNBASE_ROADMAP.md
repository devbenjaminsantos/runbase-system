# RunBase Roadmap

## Visao

RunBase sera um sistema administrativo interno para gestao de clientes, planos e pedidos, com autenticacao segura, controle de acesso por roles e painel operacional baseado em dados reais.

O projeto atual funciona como um prototipo funcional do produto: possui frontend administrativo, backend inicial, autenticacao, usuarios, pedidos, configuracoes e metricas. A nova fase transforma essa base em um produto mais robusto com Next.js, ASP.NET Core Web API, Azure SQL, RBAC real e deploy em Azure.

O inicio da implementacao esta definido em [`RUNBASE_START.md`](./RUNBASE_START.md).

## Decisao de Direcao

### Estado atual

- Frontend estatico com HTML, CSS e JavaScript.
- Backend em Node.js/Express.
- Persistencia local com sql.js e suporte a MySQL.
- Modulos ja representados: auth, users, orders, settings, analytics e notifications.

### Estado alvo

- Frontend em Next.js, React e TypeScript.
- Backend em ASP.NET Core Web API e C#.
- Banco em Azure SQL.
- Auth com JWT, refresh token e RBAC.
- Deploy via Azure Static Web Apps e Azure App Service.
- CI/CD com GitHub Actions.

### Estrategia

O RunBase deve ser reconstruido de forma incremental, usando o produto atual como referencia de UX e regras operacionais. A prioridade nao e migrar cada arquivo existente, mas preservar o aprendizado do prototipo e criar uma base tecnica limpa para o produto final.

## Roles

| Role | Permissoes |
| --- | --- |
| Admin | Acesso total, incluindo usuarios, roles, planos, clientes, pedidos, dashboard e configuracoes. |
| Manager | Gerencia clientes, planos e pedidos. Visualiza dashboard. Nao gerencia usuarios Admin. |
| Support | Visualiza clientes e pedidos. Atualiza status de pedidos. |
| Viewer | Acesso somente leitura ao dashboard e listas permitidas. |

## Dominio Inicial

### Users

- `id`
- `name`
- `email`
- `passwordHash`
- `role`
- `status`
- `createdAt`
- `updatedAt`

Regras:

- Usuario pode estar `Active` ou `Inactive`.
- Usuario inativo nao pode autenticar.
- Admin nao deve conseguir desativar a propria conta quando for o ultimo Admin ativo.

### Clients

- `id`
- `name`
- `email`
- `phone`
- `companyName`
- `status`
- `currentPlanId`
- `createdAt`
- `updatedAt`

Regras:

- Cliente pode estar `Active`, `Inactive` ou `Suspended`.
- Cliente pode existir sem plano ativo.
- Cliente pode ter multiplos pedidos.

### Plans

- `id`
- `name`
- `description`
- `price`
- `billingCycle`
- `isActive`
- `createdAt`
- `updatedAt`

Regras:

- Plano inativo nao pode ser usado em novos pedidos.
- Pedidos antigos preservam o valor contratado mesmo se o plano mudar depois.

### Orders

- `id`
- `clientId`
- `planId`
- `status`
- `amount`
- `notes`
- `createdAt`
- `updatedAt`

Regras:

- Status inicial recomendado: `Pending`.
- Status suportados no MVP: `Pending`, `Processing`, `Completed`, `Cancelled`, `Refunded`.
- Pedido deve registrar `amount` proprio para manter historico financeiro.

## Roadmap por Milestones

### Milestone 0 - Repositorio e Direcao

Objetivo: deixar claro que o projeto entrou na fase RunBase.

- [x] Registrar roadmap RunBase.
- [x] Atualizar README com a nova direcao.
- [x] Definir se o codigo legado ficara como `legacy/` ou se a nova stack sera criada em pastas paralelas.
- [x] Definir estrutura inicial do monorepo.

Criterio de pronto:

- Qualquer pessoa abrindo o repo entende o que existe hoje, qual e o produto alvo e qual e a proxima etapa tecnica.

### Milestone 1 - Backend Foundation

Objetivo: criar a base ASP.NET Core Web API.

Documento de inicio: [`RUNBASE_START.md`](./RUNBASE_START.md).

- [x] Criar solution `.NET`.
- [x] Criar projeto `RunBase.Api`.
- [x] Criar projetos de dominio/aplicacao se necessario, mantendo Clean Architecture leve.
- [x] Configurar Scalar para documentacao interativa da API.
- [x] Configurar health check.
- [x] Configurar connection string via environment variables.
- [x] Criar pipeline basico de build.

Estrutura sugerida:

```text
backend/
  src/
    RunBase.Api/
    RunBase.Application/
    RunBase.Domain/
    RunBase.Infrastructure/
  tests/
    RunBase.Application.Tests/
```

Criterio de pronto:

- API sobe localmente.
- Scalar abre.
- `/health` responde.
- Build passa via CLI.

### Milestone 2 - Auth, JWT e RBAC

Objetivo: implementar a base de seguranca antes dos CRUDs.

- [x] Criar entidade `User`.
- [x] Criar roles `Admin`, `Manager`, `Support`, `Viewer`.
- [x] Criar login.
- [x] Emitir JWT com claims de usuario e role.
- [x] Criar refresh token.
- [x] Criar logout/revoke.
- [x] Criar endpoint `/api/auth/me`.
- [x] Criar policies de autorizacao por role.
- [x] Criar seed de usuario Admin inicial.

Criterio de pronto:

- Usuario consegue logar e receber token.
- Token protege endpoints privados.
- Refresh token renova sessao.
- Logout invalida refresh token.
- Roles bloqueiam acesso indevido.

### Milestone 3 - Modulos Operacionais

Objetivo: implementar os CRUDs centrais do produto.

- [x] Users CRUD.
- [x] Clients CRUD.
- [x] Plans CRUD.
- [x] Definir estagios iniciais de plano: `Trial`, `Free`, `Plus`, `Premium`.
- [x] Orders CRUD.
- [x] Toggle de plano ativo/inativo.
- [x] Atualizacao de status de pedido.
- [x] Validacoes de regras de dominio.
- [x] Testes unitarios iniciais nos Services.

Criterio de pronto:

- Admin gerencia usuarios.
- Manager gerencia clientes, planos e pedidos.
- Support consegue atualizar status de pedidos.
- Viewer nao consegue alterar dados.
- Testes cobrem regras principais.

### Milestone 4 - Security & Privacy Foundation

Objetivo: pautar a V4 em cyberseguranca aplicada, protegendo dados sensiveis e reforcando confidencialidade, integridade e disponibilidade.

- [x] Trocar hasher temporario por hash de senha adequado.
- [ ] Criar camada de mascaramento para email, telefone e documentos.
- [ ] Criar criptografia de dados sensiveis em repouso.
- [ ] Separar permissao administrativa de permissao para visualizar dados sensiveis.
- [ ] Criar policy `SensitiveData.View`.
- [ ] Criar audit log para revelacao e alteracao de dados sensiveis.
- [ ] Garantir que logs da aplicacao nao exponham dados sensiveis.
- [ ] Preparar persistencia com consultas parametrizadas para prevenir SQL Injection.
- [ ] Adicionar validacoes de entrada para DTOs publicos.
- [ ] Adicionar rate limiting para login e endpoints sensiveis.
- [ ] Criar gerador de dados sinteticos para clientes e assinaturas.
- [ ] Marcar origem dos dados como `Demo`, `Manual` ou `Imported`.
- [ ] Modelar campanhas de notificacao: promocao, cobranca a vencer e cobranca em atraso.

Criterio de pronto:

- Confidencialidade: Admin comum nao ve dados sensiveis completos por padrao.
- Integridade: alteracoes criticas ficam validadas e auditadas.
- Disponibilidade: endpoints sensiveis possuem protecoes basicas contra abuso.
- SQL Injection: persistencia futura segue uso de parametros, sem concatenacao de SQL.
- Ambiente demo tem clientes e assinaturas realistas sem dados reais.

### Milestone 5 - Frontend Foundation

Objetivo: criar o admin em Next.js consumindo a API real.

- [ ] Criar app Next.js com TypeScript.
- [ ] Configurar rotas protegidas.
- [ ] Criar tela de login.
- [ ] Criar layout administrativo.
- [ ] Criar sidebar com itens baseados na role.
- [ ] Criar client HTTP com tratamento de token e refresh.
- [ ] Criar estados de loading, erro, vazio e permissao negada.

Estrutura sugerida:

```text
frontend/
  app/
  components/
  features/
  lib/
  types/
```

Criterio de pronto:

- Frontend autentica contra API real.
- Usuario sem token e redirecionado para login.
- Menu muda de acordo com a role.
- Nenhuma tela principal depende de mock fixo.

### Milestone 6 - Telas Principais

Objetivo: entregar o fluxo operacional completo.

- [ ] Dashboard com metricas reais.
- [ ] Users list/create/edit/status.
- [ ] Clients list/create/edit/status.
- [ ] Plans list/create/edit/toggle active.
- [ ] Orders list/create/edit/status.
- [ ] Settings do usuario logado.
- [ ] Filtros e busca nas listas principais.

Criterio de pronto:

- Operacao basica pode ser feita de ponta a ponta pelo painel.
- Dashboard reflete dados reais.
- Erros da API aparecem de forma clara para o usuario.

### Milestone 7 - Azure SQL e Deploy

Objetivo: colocar o produto em ambiente cloud.

- [ ] Criar migrations.
- [ ] Configurar Azure SQL.
- [ ] Configurar Azure App Service para API.
- [ ] Configurar Azure Static Web Apps para frontend.
- [ ] Configurar variaveis de ambiente.
- [ ] Criar GitHub Actions para build, test e deploy.

Criterio de pronto:

- Push na branch principal executa pipeline.
- Backend e frontend fazem deploy automaticamente.
- Ambiente publicado usa Azure SQL.

### Milestone 8 - Qualidade e Produto

Objetivo: amadurecer o RunBase como produto demonstravel.

- [ ] Testes xUnit para Services.
- [ ] Testes Vitest para utilitarios do frontend.
- [ ] Testes Playwright para login e fluxo principal.
- [ ] Scalar revisado com documentacao dos endpoints.
- [ ] README atualizado com setup completo.
- [ ] Documentar roles, endpoints e variaveis de ambiente.
- [ ] Criar dados seed para demo.

Criterio de pronto:

- Projeto pode ser apresentado como produto real.
- Novo desenvolvedor consegue rodar localmente seguindo a documentacao.
- Fluxo principal esta coberto por testes automatizados.

## Ordem Recomendada de Execucao

1. Atualizar README e estrutura do repo para a fase RunBase.
2. Criar backend ASP.NET Core com health check e Scalar.
3. Implementar Auth e RBAC antes dos demais modulos.
4. Implementar Users, Clients, Plans e Orders.
5. Criar frontend Next.js com login e layout protegido.
6. Conectar telas reais aos endpoints.
7. Configurar testes e CI.
8. Publicar em Azure.

## Fora do Escopo Inicial

- Multiempresa/SaaS completo.
- Billing real com gateway de pagamento.
- Permissoes configuraveis por tela ou acao.
- Auditoria detalhada para todas as entidades.
- Notificacoes em tempo real.
- Relatorios avancados.

Esses itens fazem sentido depois que o produto principal estiver confiavel.
