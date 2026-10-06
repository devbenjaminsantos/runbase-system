# RunBase - Estrategia de Testes

Este documento registra como validar o RunBase localmente e no CI. Os testes usam apenas configuracoes de desenvolvimento e nao devem acessar dados ou credenciais de producao.

## Camadas de Teste

### Backend - xUnit

Localizacao: `backend/tests/RunBase.Application.Tests`.

Cobre:

- regras dos Services e do dominio;
- autenticacao, JWT, refresh token e logout;
- matriz RBAC;
- mascaramento, criptografia e auditoria de dados sensiveis;
- protecao contra uso de Raw SQL;
- integracao HTTP da API;
- CRUDs, validacao, headers de seguranca e payloads suspeitos.
- migrations, schema, constraints, precisao decimal e transacoes em PostgreSQL descartavel.

Os testes HTTP forcam explicitamente repositories em memoria, mesmo quando o processo possui uma
`ConnectionStrings__DefaultConnection` herdada. A suite de persistencia cria seu proprio container
`postgres:17.6-alpine` com Testcontainers, usa porta aleatoria e remove o container ao final. Ela nao
aceita uma connection string externa e nunca deve apontar para Neon ou outro banco compartilhado.

Executar toda a suite:

```bash
cd backend
dotnet test RunBase.slnx
```

Executar somente os testes de seguranca da API:

```bash
cd backend
dotnet test RunBase.slnx --filter FullyQualifiedName~ApiSecurityIntegrationTests
```

Executar somente os testes PostgreSQL (requer Docker ativo):

```bash
cd backend
dotnet test RunBase.slnx --filter FullyQualifiedName~PostgreSqlPersistenceTests
```

Gerar cobertura do backend:

```bash
cd backend
dotnet test RunBase.slnx --collect:"XPlat Code Coverage"
```

## Frontend - Vitest

Localizacao: arquivos `*.test.ts` dentro de `frontend/lib`.

Cobre:

- leitura, escrita e limpeza da sessao;
- API client e tratamento de erros;
- envio do access token;
- rotacao unica do refresh token em requests concorrentes;
- matriz de navegacao por role;
- formatacao compartilhada de datas e valores;
- respostas sem conteudo.

Executar uma vez:

```bash
cd frontend
npm test
```

Executar em modo watch:

```bash
cd frontend
npm run test:watch
```

Executar apenas o API client:

```bash
cd frontend
npm test -- lib/api.test.ts
```

## Fluxos Reais - Playwright

Localizacao: `frontend/e2e`.

O Playwright inicia automaticamente:

- API ASP.NET Core em `http://localhost:5140`;
- frontend Next.js em `http://localhost:3000`;
- repositorios em memoria do ambiente `Development`.

As portas `3000` e `5140` devem estar livres. A configuracao nao reutiliza servidores existentes para impedir que os testes escrevam em uma API ou banco iniciados manualmente.

Executar os fluxos E2E:

```bash
cd frontend
npm run test:e2e
```

Executar um cenario especifico:

```bash
cd frontend
npm run test:e2e -- --grep "refreshes"
```

Abrir a interface do Playwright:

```bash
cd frontend
npm run test:e2e:ui
```

Em sistemas suportados, instalar o Chromium antes da primeira execucao:

```bash
cd frontend
npx playwright install chromium
```

No macOS 12 usado no desenvolvimento do RunBase, o Playwright atual nao distribui mais Chromium ou FFmpeg. A configuracao usa o Opera local como navegador Chromium e desativa somente a gravacao de video local. No GitHub Actions, Chromium, traces, screenshots e video em falhas permanecem habilitados.

## Validacao Completa Local

Antes de enviar uma alteracao relevante:

```bash
cd backend
dotnet test RunBase.slnx

cd ../frontend
npm test
npm run build
npm run test:e2e
npm audit
```

## GitHub Actions

O workflow executa:

1. Restore, build e xUnit do backend, incluindo PostgreSQL descartavel via Docker.
2. Instalacao, Vitest e build do frontend.
3. Build da imagem Docker da API.
4. API e frontend locais com Playwright no Chromium.
5. Deploy hooks somente quando todas as validacoes necessarias passam em `main`.

O relatorio HTML do Playwright e enviado como artifact por sete dias. Nenhum teste do CI usa connection string, senha ou token de producao.

## Regras para Novos Testes

- Testar comportamento observavel, nao detalhes privados da implementacao.
- Criar dados unicos por teste quando houver persistencia compartilhada.
- Nunca inserir secrets reais em fixtures, logs ou snapshots.
- Manter testes de role explicitos para que mudancas de permissao sejam revisaveis.
- Adicionar um teste de regressao sempre que uma falha real for corrigida.
- Nao apontar Playwright ou testes de integracao para Render, Vercel ou Neon.
