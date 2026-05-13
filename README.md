# RunBase

Painel administrativo interno para gestao de clientes, planos, pedidos e acessos, com foco em operacoes recorrentes e controle por roles.

O projeto nasceu como uma base visual de painel administrativo chamada Olympus Admin e agora entra em uma nova fase como RunBase: um produto mais robusto com frontend em `Next.js`, backend em `ASP.NET Core Web API`, banco em `Azure SQL`, autenticacao com `JWT + Refresh Token` e RBAC real.

O roadmap completo da nova fase esta em [`docs/RUNBASE_ROADMAP.md`](./docs/RUNBASE_ROADMAP.md).

## Visao do Produto

O objetivo do RunBase e se tornar um painel de gestao operacional para pequenas e medias operacoes, centralizando tarefas administrativas em uma interface simples e objetiva.

Na pratica, a proposta do produto e permitir:

- acompanhamento de usuarios e perfis de acesso
- controle de pedidos e estados operacionais
- leitura rapida de indicadores do painel
- configuracoes globais da operacao
- futura trilha de auditoria e notificacoes em tempo real

## Status Atual

Atualmente, o projeto possui:

- frontend multi-pagina com dashboard, usuarios, pedidos e configuracoes
- interacoes locais no navegador para demonstracao da experiencia
- backend inicial estruturado com rotas para autenticacao, usuarios, pedidos, notificacoes e analytics

Hoje ele ja funciona muito bem como base de interface e arquitetura inicial, mas a evolucao principal da `V2` sera conectar o frontend ao backend para transformar a experiencia em uma operacao real.

## Preview

![Demo](./media/demo.gif)
![Tela inicial](./media/inicio.png)
![Tela de usuarios](./media/usuario.png)
![Tela de pedidos](./media/pedido.png)
![Configuracoes](./media/configuracao.png)

## Escopo da V1

A `V1` representa a fundacao do produto.

- dashboard com cards e grafico visual
- tela de usuarios com CRUD local
- tela de pedidos com filtros, tabela e exportacao CSV
- tela de configuracoes com persistencia local
- tema claro/escuro persistido no navegador
- estrutura inicial de backend com API REST

## Tecnologias

### Frontend

- HTML5
- CSS3
- JavaScript
- Bootstrap
- jQuery
- Chart.js
- DataTables

### Backend

- Node.js
- Express
- sql.js
- JSON Web Token
- bcryptjs
- Socket.io

### Stack atual e direcao tecnica

O backend atual em `Node.js/Express` sera tratado como prototipo funcional e referencia de produto. A nova fase tecnica do RunBase sera construida com `ASP.NET Core Web API`, `Next.js` e `Azure SQL`, seguindo o roadmap em `docs/RUNBASE_ROADMAP.md`.

Quando fizer sentido adicionar uma camada complementar para relatorios, automacoes ou modulos mais analiticos, ela deve ser avaliada depois que a base principal estiver publicada e testada.

## Como rodar localmente

### Frontend

1. Clone o repositorio
2. Abra o projeto no editor
3. Rode o frontend com `Live Server` ou abra `index.html`

### Backend

1. Acesse a pasta `backend`
2. Instale as dependencias com `npm install`
3. Crie o arquivo `.env` com base em `.env.example`
4. Inicie a API com `npm run dev`
5. Rode os testes basicos com `npm test`

### MySQL local com Docker

1. Suba o banco com `docker compose up -d`
2. Acesse `backend/.env` e altere `DATABASE_DRIVER=mysql`
3. Inicialize o schema com `npm run db:mysql:init`
4. Inicie a API com `npm run dev`

### Migrando dados do SQL.js para MySQL

1. Garanta que o MySQL esteja rodando
2. Na pasta `backend`, rode `npm run db:migrate:mysql`
3. Confirme no `.env` que `DATABASE_DRIVER=mysql`
4. Inicie a API com `npm run dev`

## Checklist de QA da V2

Use esta validacao no fluxo local com `sqljs` antes de migrar para MySQL.

### Preparacao

1. Na pasta `backend`, confirme `DATABASE_DRIVER=sqljs`
2. Rode a API com `npm run dev`
3. Abra o frontend com `Live Server` ou navegador local
4. Acesse `login.html`

### Fluxo principal

1. Faça login com `admin@olympus.local` e `Admin123!`
2. Confirme se o dashboard carrega metricas sem erro visual
3. Abra `Users` e crie um novo usuario
4. Edite esse usuario e confirme a atualizacao
5. Exclua ou inative um usuario de teste
6. Abra `Orders` e crie um pedido para um usuario ativo
7. Edite o pedido criado
8. Exclua o pedido criado
9. Abra `Settings` e altere o nome da empresa
10. Confirme se o branding atualiza sem reload

### Verificacoes de UX

1. Confirme se toasts aparecem com mensagens coerentes
2. Confirme se estados de loading, erro e retry aparecem nas telas principais
3. Confirme se listas vazias mostram mensagens claras
4. Confirme se tema claro/escuro continua funcionando
5. Confirme se o logout redireciona para `login.html`

### Resultado esperado

- login funcional
- CRUD de usuarios funcional
- CRUD de pedidos funcional
- configuracoes persistidas no backend
- dashboard refletindo dados reais
- feedback visual consistente na interface

## Roadmap do Produto

### V1 - Base visual e arquitetura inicial

- [x] Estrutura multi-pagina do painel administrativo
- [x] Dashboard com cards e grafico visual
- [x] Tela de usuarios com CRUD local
- [x] Tela de pedidos com tabela, filtros e exportacao CSV
- [x] Tela de configuracoes com persistencia local
- [x] Tema claro/escuro persistido no navegador
- [x] Backend inicial com rotas de autenticacao, usuarios, pedidos, notificacoes e analytics

### V2 - Painel de gestao operacional funcional

- [x] Integrar frontend com a API real
- [x] Criar fluxo de login com autenticacao JWT
- [x] Persistir usuarios e pedidos no banco de dados
- [x] Persistir configuracoes globais no backend
- [x] Alimentar dashboard com metricas reais
- [x] Implementar estados reais de pedidos
- [x] Corrigir inconsistencias principais entre frontend e backend
- [x] Atualizar a experiencia para refletir dados reais da operacao
- [ ] Validar a operacao completa em ambiente MySQL
- [ ] Finalizar a revisao funcional de ponta a ponta no navegador

### V3 - Operacao, monitoramento e controle

- [ ] Adicionar trilha de auditoria para acoes administrativas
- [ ] Implementar notificacoes em tempo real com Socket.io
- [ ] Criar filtros por periodo e relatorios operacionais
- [ ] Adicionar permissoes por perfil de acesso
- [ ] Criar tela de atividades recentes
- [ ] Preparar deploy completo de frontend e backend

### V4 - Expansao de produto

- [ ] Adicionar dashboard executivo com KPIs avancados
- [ ] Exportar relatorios em multiplos formatos
- [ ] Criar modulo de clientes e relacionamento
- [ ] Implementar busca global no painel
- [ ] Adicionar testes automatizados e observabilidade
- [ ] Estruturar versao SaaS ou multiempresa

## Autor

Feito por **Benjamin Montenegro** - [LinkedIn](https://linkedin.com/in/benjaminmontenegro) | [Portfolio](https://devbenjaminsantos.github.io/benjamin-portfolio/)
