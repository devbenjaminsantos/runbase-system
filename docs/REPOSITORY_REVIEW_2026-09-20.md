**RunBase — revisão do repositório em 20/09/2026**

Referência: commit `8dae1ea` (`initializing multi-tenancy architecture`). O Git estava limpo no início. Esta revisão acrescenta somente este relatório; não corrige nem publica o produto.

**Conclusão**

O RunBase possui uma fundação organizada para um painel administrativo de uma operação: camadas separadas, autenticação real, RBAC no servidor, criptografia de e-mails de clientes, migrations, frontend funcional e automação de testes. Entretanto, há falhas de autorização, integridade, sessões e experiência de uso que merecem correção antes de ampliar o uso. A implementação atual ainda não oferece isolamento entre organizações.

O maior problema de planejamento é considerar uma funcionalidade concluída apenas porque há implementação e testes do caminho feliz. Por exemplo: há refresh token, mas a rotação não é atômica; há proteção do último administrador, mas a verificação permite corrida; há dashboard, mas consultas recusadas ou indisponíveis se tornam números zero; há planos inativos, mas pedidos não consultam essa restrição.

**Escopo e limites**

Foram revisados os documentos de produto e arquitetura, configuração de CI/deploy, API, contratos, serviços, domínio, repositories EF/em memória, schema/migrations, criptografia, autenticação, páginas e cliente HTTP do frontend e cobertura das suítes existentes. Foram consultados avisos atuais dos mantenedores para dependências e documentação oficial para recomendações de segurança.

As reproduções usam dados sintéticos locais. Não foram feitas alterações nem testes de ataque em Render, Vercel ou Neon. Configurações reais, deploy em execução, branch protection, backups e migration aplicada em produção não foram verificados. Uma busca por padrões comuns de segredos nos arquivos rastreados não encontrou esses padrões; isso não equivale a varredura completa do histórico Git ou a prova de ausência de credenciais.

Legenda: **reproduzido** significa execução local; **confirmado no código** significa caminho identificável na implementação; **condicional** depende de concorrência, configuração ou ambiente ainda não exercitado. Alta prioridade não significa necessariamente exploração anônima pela internet.

**Validação executada**

| Verificação | Resultado |
| --- | --- |
| Git inicial e `git diff --check` | Limpo / aprovado |
| Vitest | 18 testes aprovados em 4 arquivos |
| Build Next.js de produção | Aprovado, incluindo TypeScript |
| Build e testes .NET | Aprovados: 127 testes, nenhuma falha ou teste ignorado |
| Playwright existente | 5 cenários aprovados; execução local com Opera/Chromium, conforme a configuração do projeto |
| `npm audit --json` | 5 pacotes sinalizados: 1 crítico, 1 alto e 3 moderados |
| `dotnet list ... package --vulnerable --include-transitive --no-restore --format json` | Nenhum pacote vulnerável listado pela consulta ao NuGet |
| PostgreSQL/migrations reais | Não executado; daemon Docker local indisponível |
| Probes adicionais de sessão e API | 11 observações HTTP e 4 observações do cliente HTTP/datas, detalhadas ao final |

O build Next.js utilizou a versão resolvida `16.3.0`, React `19.2.6`, Vitest `4.1.10` e Playwright `1.62.1`. A versão efetiva vem do lockfile, não apenas do intervalo declarado no `package.json`. A consulta NuGet não avalia vulnerabilidades da imagem Docker, sistema operacional ou runtime publicado.

A primeira tentativa de Playwright expirou aguardando o servidor dentro do sandbox. A API local também recebeu erro de permissão ao abrir socket nesse ambiente. Após executar fora dessa restrição, os cinco cenários passaram em 5,2 minutos. Isso foi uma limitação de execução local, não uma falha funcional demonstrada pelo E2E. O backend compilou e concluiu seus 127 testes; a duração reportada pela suíte foi de aproximadamente cinco minutos.

**Achados prioritários**

**01 — Dependências com avisos críticos/altos. Prioridade alta; dependências confirmadas, exploração condicional.**

O lockfile contém Next.js `16.3.0` e sharp `0.35.3`. O audit aponta dois avisos críticos em Next.js: execução remota em hospedagem Windows e execução remota no processamento de AVIF. O segundo está ligado à cadeia sharp/libheif. As primeiras versões corrigidas informadas nos avisos são Next.js `16.3.3` e sharp `0.35.4`.

Não encontrei upload de imagens ou `remotePatterns` no projeto; as imagens utilizadas são PNG locais. Isso limita a evidência de exposição ao cenário AVIF. O cenário Windows exige servidor Windows e não deve ser apresentado como exploração comprovada do deploy planejado em Vercel.

Também foram sinalizados `vitest` e `@vitest/mocker` `4.1.10`, corrigidos em `4.1.11`, e `baseline-browser-mapping` `2.10.31`, corrigido em `2.11.0`. São cinco pacotes afetados, não cinco explorações independentes demonstradas.

Incremento: atualizar dependências e lockfile em mudança isolada, repetir audit, build e testes e confirmar qual versão foi efetivamente publicada. Adicionar verificação de dependências ao CI. Fontes: [Next.js/Windows](https://github.com/vercel/next.js/security/advisories/GHSA-p293-qw3h-jr36), [Next.js/AVIF](https://github.com/vercel/next.js/security/advisories/GHSA-2xp9-vwfh-vxw4), [sharp](https://github.com/lovell/sharp/security/advisories/GHSA-rgj7-g3m4-5g8c), [Vitest](https://github.com/vitest-dev/vitest/security/advisories/GHSA-82fw-gwwq-j7x9).

**02 — Support pode criar, editar valores e excluir pedidos. Prioridade alta; reproduzido por HTTP local.**

Em `backend/src/RunBase.Application/Auth/AuthPolicies.cs`, `ManageOrders` inclui Support. Em `backend/src/RunBase.Api/Program.cs:654`, a policy protege o grupo inteiro, incluindo POST, PUT e DELETE. Não há restrição adicional por operação. Isso excede a matriz documentada, que prevê consulta e alteração de status para Support.

Incremento: separar leitura, criação/edição, alteração de status e exclusão. Cobrir cada verbo HTTP por role; ocultar botões na UI é complementar, não corrige a autorização da API.

**03 — O produto ainda não é multi-tenant. Bloqueador para hospedar organizações independentes; confirmado no código.**

Existem `Organization`, mapeamento e migration, mas não Membership, contexto de organização, `OrganizationId` nas entidades operacionais, filtros de tenant ou chaves estrangeiras compostas. As roles ainda pertencem ao User global. Consultas e índices de clientes/planos são globais. `Organization.CanOperate` não participa da autenticação ou dos CRUDs.

Isso é uma etapa explicitamente planejada, não uma implementação de isolamento que possa ser considerada pronta. A mesma instância não deve ser tratada como ambiente separado para empresas distintas. O guia `docs/MULTI_TENANCY.md` define uma direção adequada; falta executar a transição, inclusive atribuição dos dados antigos a uma organização inicial.

Incremento: Membership e Owner; contexto autenticado; migração/backfill dos dados; isolamento por módulo; constraints compostas; testes cruzados entre duas organizações. Preservar compatibilidade com `User.Role` até existir um caminho seguro de migração.

**04 — Rotação de refresh token não é atômica. Prioridade alta; corrida identificada no código, não validada contra PostgreSQL.**

`AuthService.cs:91` lê o token e verifica se está ativo; depois busca usuário, revoga e salva outro token. `EfRefreshTokenRepository` faz essas gravações separadamente. Não existe atualização condicional que consuma o token apenas se ainda estiver ativo, transação envolvendo a troca, versão de concorrência ou família de sessão.

Duas requisições podem ler o mesmo token ativo e gerar dois sucessores. Uma falha entre revogação e emissão também pode perder a sessão. A rejeição de reutilização sequencial, coberta nos testes, não resolve a corrida. Reutilização detectada não invalida a cadeia sucessora.

Incremento: token armazenado como hash, consumo atômico, sucessor na mesma transação, família de sessão e política de reutilização. Testar simultaneidade com DbContexts/conexões separados e falha entre etapas.

**05 — Proteção do último Admin permite corrida. Prioridade alta; identificada no código.**

`UsersService.cs:78` e `:114` consultam a existência de outro Admin ativo e só depois persistem a mudança/exclusão. Duas operações sobre os dois últimos administradores podem aprovar a mesma condição e deixar a operação sem Admin. O bootstrap serializável existente protege apenas a criação inicial.

Incremento: operação transacional com serialização/lock apropriado para preservar a invariável. Reutilizar o aprendizado na proteção do último Owner. Testar rebaixamento, desativação e exclusão simultâneos. Não pressupor que um lock em memória protege múltiplas instâncias.

**06 — Exclusão de cliente permite pedidos órfãos. Prioridade alta; reproduzido por HTTP local.**

`ClientsService.DeleteAsync` remove diretamente o cliente. `RunBaseDbContext.cs:123` define índice em `Order.ClientId`, mas não FK; a migration inicial também não cria essa relação. Pedidos continuam apontando para um cliente inexistente. `RefreshToken.UserId` tampouco possui FK ou limpeza associada ao usuário removido.

Incremento: definir retenção e regra de exclusão, criar FK e impedir exclusão destrutiva de cliente com histórico ou adotar arquivamento. Tratar tokens removidos/órfãos conforme a política de sessões. Acrescentar testes no PostgreSQL.

**07 — Pedido concluído mantém valor e cliente editáveis e pode ser apagado. Prioridade alta; alteração de valor e exclusão reproduzidas por HTTP local.**

`Domain/Orders/Order.cs:43` permite `Update` se o status informado continuar igual ao status terminal. Assim, um pedido Completed pode ter cliente, plano e valor alterados. `OrdersService.DeleteAsync` não restringe estado. A receita do dashboard pode ser reescrita ou eliminada sem trilha operacional.

Incremento: definir campos imutáveis após conclusão; implementar correção/estorno como evento separado com motivo e auditoria. A V12 já prevê essa direção, mas o risco afeta os CRUDs existentes.

**08 — Campos omitidos podem escolher Admin ativo implicitamente. Prioridade alta; reproduzido por HTTP local.**

`CreateUserRequest` recebe enums não anuláveis sem obrigatoriedade de presença JSON. `UserRole.Admin` e `UserStatus.Active` são zero. A configuração atual de JSON não exige parâmetros de construtor ausentes. O endpoint anuncia role explícita, mas o contrato permite defaults na desserialização. O mesmo padrão pode alterar role/status em PUTs incompletos.

O endpoint já exige Admin: não é cadastro público nem escalada anônima. É uma falha de integridade do contrato que pode gerar privilégio por omissão de cliente/integrador.

Incremento: exigir presença e validade dos campos, sem usar Admin como fallback. Testar JSON parcial real, pois construir DTOs diretamente em C# não exercita essa falha.

**09 — Tokens persistidos em localStorage e refresh tokens brutos no banco. Prioridade alta de endurecimento; confirmado.**

`frontend/lib/session.ts` salva a sessão completa. `RunBaseDbContext.cs:70` usa o valor bruto do refresh token como chave primária. Um XSS no frontend exporia a sessão; leitura indevida do banco poderia fornecer refresh tokens utilizáveis. Não foi encontrado um XSS explorável no código revisado; esse é o impacto da forma de armazenamento caso ocorra comprometimento.

O README reconhece a dívida, mas ela não aparece com destaque suficiente nos próximos incrementos. O frontend também não define CSP; a CSP da API não protege o documento HTML do Next.js.

Incremento: antecipar o armazenamento seguro de refresh tokens e a migração da sessão. Cookies HttpOnly/Secure exigem desenho correto para os domínios separados, SameSite e proteção CSRF; um BFF de mesma origem é uma opção a avaliar. Access token curto em memória e CSP compatível com Next.js completam a proteção.

**10 — Logout pode falhar ou ser desfeito por refresh pendente. Prioridade alta/média conforme cenário; confirmado no código.**

Em `frontend/lib/api.ts:72`, uma rejeição de rede impede `clearSession`. Respostas HTTP malsucedidas são ignoradas: um access token expirado pode produzir 401, a UI apaga a sessão, mas o refresh token permanece válido no servidor. Em `:150`, um refresh em andamento pode gravar uma nova sessão depois do logout. `activeRefresh` só coordena chamadas no mesmo contexto JavaScript, não abas distintas.

No backend, logout revoga apenas o refresh token: o access token continua válido enquanto usuário/role/status e expiração permitirem. A configuração é de 60 minutos, mais tolerância temporal padrão do validador; não é encerramento imediato da sessão. Também não há vínculo explícito do refresh token enviado com a identidade que chama logout.

Incremento: limpeza local em `finally`, tratamento explícito da revogação, invalidar respostas de refresh de uma sessão encerrada, coordenação entre abas e definição de revogação no servidor. Incluir testes de rede offline, token expirado e refresh/logout concorrentes.

**11 — Desativação bloqueia o acesso, mas não encerra permanentemente as sessões antigas. Prioridade média; confirmado no código.**

O evento JWT verifica status e role a cada request, o que é positivo. Porém, os tokens não são revogados ao desativar; ao reativar o usuário antes da expiração, credenciais antigas podem voltar a funcionar. Alterar role e depois restaurá-la tem efeito semelhante sobre JWTs antigos. Falta uma versão de segurança/sessão.

Incremento: revogação explícita e security stamp/session version para mudanças relevantes. O teste deve incluir desativar, reativar e tentar reutilizar os tokens anteriores.

**12 — Unicidade de e-mail não coincide com a identidade usada pelo login. Prioridade alta para consistência; condicional à concorrência/collation.**

O repository consulta `user.Email.ToUpper()`, mas o índice único é sobre `Email` original; `User` não normaliza na escrita. Em PostgreSQL com comparação textual sensível a maiúsculas, solicitações concorrentes com variações de caixa podem passar na consulta prévia e ocupar linhas distintas. E-mails com espaços também não seguem uma normalização uniforme. O índice original não garante a invariável documentada de identidade global normalizada.

Incremento: coluna normalizada persistida e índice único consistente, ou estratégia equivalente explícita de banco. Preparar detecção de colisões nos dados existentes antes da migration. Traduzir conflitos de unicidade para 409.

**13 — Chave criptográfica efêmera pode ser usada com persistência real em Development. Prioridade alta para perda de dados; cenário condicional.**

`AesGcmSensitiveDataProtector.GetKey` gera fallback por processo quando a chave está ausente. A DI escolhe EF pela presença da connection string, independentemente dessa chave. `ValidateProductionConfiguration` pula validações em Development. Portanto, Development + PostgreSQL + chave ausente pode gravar dados que deixam de ser descriptografáveis após reinício.

Além disso, a configuração de produção só verifica presença da chave no startup; Base64/tamanho são validados quando o serviço é instanciado. Não há identificador de chave nem procedimento de rotação/recifragem; a mesma chave também gera o lookup HMAC.

Incremento: exigir chave persistente sempre que houver banco persistente, validar formato no startup e planejar rotação versionada com recuperação testada. Separar chaves de cifragem/lookup por derivação ou gestão explícita. Não rotacionar descartando a chave antiga sem recifrar dados.

**14 — Guia de conexão ao banco não verifica a identidade do servidor. Prioridade alta de configuração; produção não inspecionada.**

`docs/CLOUD_DEPLOYMENT.md:25` recomenda `SSL Mode=Require;Trust Server Certificate=true`. No Npgsql atual, Require exige criptografia, mas não valida certificado/hostname como VerifyFull. O guia pode induzir uma conexão sem a autenticação esperada do servidor. Não foi conferida a string real do ambiente publicado.

Incremento: documentar e validar `SSL Mode=VerifyFull` com cadeia confiável. Confirmar a configuração efetiva sem exibir credenciais. Fonte: [segurança do Npgsql](https://www.npgsql.org/doc/security).

**Falhas funcionais e de produto**

**15 — Tela de pedidos quebra para Support. Prioridade alta funcional; confirmado no código.**

`frontend/app/orders/page.tsx:93` usa `Promise.all` para pedidos e clientes. A API permite pedidos a Support, mas nega `/api/clients` pela policy ManageClients. O 403 derruba todo o carregamento. É uma inconsistência separada do excesso de poderes do achado 02.

Incremento: leitura de clientes permitida conforme a matriz ou resposta de pedidos com apenas os dados associados autorizados. Validar o fluxo inteiro de Support, incluindo renderização e alteração de status permitida.

**16 — Dashboard apresenta ausência de autorização ou indisponibilidade como zero. Prioridade alta funcional; confirmado no código.**

`frontend/app/dashboard/page.tsx:76` consulta quatro listas independentemente da role. `getValue` converte qualquer rejeição em `[]`; em seguida a tela entra em ready. Viewer recebe 403 nas quatro listas e vê um painel vazio apresentado como operacional. Falhas de rede/500 também podem produzir indicadores falsos. A policy ViewDashboard não protege nenhum endpoint de métricas.

Incremento: endpoint de agregados autorizado para dashboard, estados distintos para sem dados, sem permissão e indisponibilidade. Manter valores anteriores apenas com indicação clara de desatualização. Não resolver liberando listas sensíveis completas para Viewer.

**17 — Edição de clientes e usuários exige conhecer e redigitar e-mail. Prioridade média/alta funcional; confirmado.**

Ao editar cliente, a UI define `email: ""`, torna o campo obrigatório e exige reentrada. A API exige e-mail mesmo para mudar somente status/nome. O operador que não conhece o endereço completo não consegue executar a alteração; isso conflita com a própria política de mascaramento. A tela Users repete a exigência, embora já receba o e-mail completo da API.

Incremento: preservar o contato atual quando não for alterado; comando específico e auditado para mudança de contato. Não retornar o e-mail real de clientes só para contornar o formulário.

**18 — Planos inativos ou inexistentes não impedem novos pedidos. Prioridade média/alta; plano inativo reproduzido por HTTP local.**

`OrdersService` depende apenas de pedidos e clientes. Ele verifica a existência do cliente, mas recebe `PlanStage` sem consultar catálogo ou `IsActive`. Clientes também armazenam stage diretamente. Desativar ou excluir um plano não impede que novos registros usem aquela classificação. A regra contradiz `RUNBASE_ROADMAP.md`.

Incremento: validar vínculo com plano ativo na contratação e preservar snapshot do contrato. A restrição única em Stage limita o catálogo a quatro planos globais; isso precisa mudar antes de catálogo flexível por organização.

**19 — Datas de cobrança podem aparecer no dia anterior e discordar entre telas/campanhas. Prioridade média; confirmado no código.**

Os formulários de clientes e planos convertem data para `T00:00:00Z`. `formatDate` mostra no fuso do navegador; em São Paulo, 20/09 à meia-noite UTC vira 19/09. O dashboard compara início do dia local, enquanto campanhas comparam instante UTC atual e ainda restringem cobranças a Plus/Premium. A mesma cobrança pode cair em segmentos diferentes.

Incremento: definir se vencimento é data civil ou instante. Para data civil, usar DateOnly/`YYYY-MM-DD` e fuso da organização; para instante, preservar semântica explicitamente. Unificar as regras de vencido/a vencer e testar fronteiras de dia/fuso.

**20 — Valores monetários têm validação de intervalo, mas não de escala. Prioridade média; identificado no contrato/schema.**

Price e FinalAmount aceitam decimal sem limite de casas, enquanto o banco usa `numeric(18,2)`. Um integrador pode enviar três ou mais casas; a resposta usa o objeto em memória, mas persistência/leitura podem refletir arredondamento. O modo em memória não reproduz essa diferença.

Incremento: rejeitar ou arredondar explicitamente segundo regra única, registrar moeda e testar round-trip PostgreSQL. Hoje USD é fixo no frontend e a moeda não faz parte do contrato comercial.

**21 — Tratamento de erros esconde a causa e pode orientar o usuário incorretamente. Prioridade média; confirmado.**

Muitas respostas de domínio usam 400/409 sem código ou mensagem. `apiFetch` ignora o corpo de erro. Na tela de clientes, qualquer 400 é descrito como falta de data de cobrança; validação de nome/e-mail também causa 400. Login trata 429 e falha de rede como credenciais inválidas. Se `/auth/setup` falhar, a tela permanece em loading com erro e sem ação de tentar novamente. `ProtectedPage` manda indisponibilidade do serviço para login.

Incremento: ProblemDetails com códigos estáveis, erros de campo, distinção entre autenticação e indisponibilidade e recuperação explícita. Middleware global deve tratar falhas inesperadas com ID de correlação, sem payloads sensíveis. Conflitos de banco hoje podem escapar como 500.

**22 — Mascaramento de usuários é somente visual. Prioridade média de clareza/privacidade; confirmado.**

UsersService retorna e-mail completo e a tela aplica `maskEmail` chamando a coluna de “Protected email”. Admin consegue ler o dado pela resposta HTTP/estado da página. Isso não é acesso anônimo: a rota exige Admin. Contudo, máscara de UI não oferece a mesma propriedade de privacidade dos clientes, cujo DTO é mascarado pelo servidor.

Incremento: definir a política específica de identidade dos usuários internos. Se Admin pode ver o endereço, não apresentar a máscara como controle de sigilo. Se não pode, mascarar no DTO e separar os fluxos administrativos.

**23 — Assinaturas e comunicação ainda são fundações, não fluxos completos. Observação de planejamento.**

Não existe entidade Subscription. Data/estágio no cliente e no plano não oferecem histórico de contratos, pausa, inadimplência, renovação ou snapshots completos. Campanhas têm cadastro, estimativa e status Draft/Scheduled, mas não worker, entrega, provedor, mensagem, aprovação, cancelamento ou tela no frontend. Marcar Scheduled não envia nada. O público é recalculado na consulta, não um snapshot de destinatários.

`DemoDataGenerator` gera DTOs e é testado, mas não está conectado a um fluxo de UI/API que persista um ambiente demo. `DataSource.Demo` pode coexistir com dados manuais, e dashboard/campanhas não os excluem. Settings é consulta do perfil/sessão, não configuração persistida da operação.

Incremento: manter a V12 antes da entrega de campanhas; explicitar “disponível”, “protótipo” e “planejado”. Definir isolamento de dados demo antes de usá-los em métricas reais ou envios.

**Disponibilidade, operação e qualidade**

**24 — Consultas sem paginação e descriptografia em massa. Prioridade média; confirmado no código.**

Repositories carregam todas as linhas com `ToListAsync`; serviços ordenam e filtram depois. Dashboard baixa listas inteiras e ainda consulta usuários sem usar essa coleção nas métricas renderizadas. `EfClientRepository.ListAsync` descriptografa todos os e-mails até quando campanhas precisam só contar clientes por estado/estágio/data. Não há paginação, limites de resultado ou agregados dedicados.

Incremento: paginação/filtro/ordenação no banco, DTOs de projeção e métricas agregadas. Evitar descriptografia quando o dado completo não é necessário. Adicionar índices de acordo com consultas reais e testes de carga modestos após corrigir os contratos.

**25 — Rate limit depende do IP de conexão, sem forwarded headers configurados. Prioridade média; efeito em produção condicional.**

`Program.cs:821` usa RemoteIpAddress. O pipeline não configura proxies confiáveis/ForwardedHeaders. Atrás de proxy, o bucket pode representar o proxy e bloquear usuários distintos juntos. O limite é local à instância; mais réplicas mudam a proteção. Refresh e consulta pública de setup não possuem limite explícito. O 429 não inclui Retry-After.

Incremento: validar o comportamento na hospedagem real, configurar proxies confiáveis e combinar limites por origem/conta conforme o endpoint. Não confiar indiscriminadamente em X-Forwarded-For. Fonte: [ASP.NET Core e proxies](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0).

**26 — Hash de senha e ciclo de vida de conta precisam de evolução. Prioridade média; confirmado.**

PBKDF2 usa SHA-256 com 210.000 iterações. A referência OWASP para PBKDF2-HMAC-SHA256 indica 600.000; calibrar custo e migrar por rehash no login. Há salt individual e comparação em tempo constante, que devem ser preservados. O parser também não limita iterações/tamanhos nem trata todos os formatos corrompidos, relevante para robustez de dados internos.

Contas posteriores aceitam 8 caracteres, enquanto setup exige 12; não há MFA, recuperação/troca de senha, convites ou confirmação recente para operações críticas. Parte disso já está na V11. O login também evita executar hash para usuário inexistente, abrindo uma diferença de tempo que merece mitigação, sem alegar enumeração remotamente comprovada.

Incremento: política consistente de credenciais, rehash versionado, recuperação segura/revogação e MFA para funções privilegiadas. Fonte: [OWASP Password Storage](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html).

**27 — Auditoria cobre tentativas de revelar e-mail, não o ciclo operacional. Prioridade média/alta; confirmado.**

O único audit log modelado é SensitiveDataAuditEntry. Não há trilha de criação/alteração/exclusão de usuários, roles, pedidos, clientes e planos, nem de login/refresh/logout. Requests registram método/path/status, sem autor, ação de negócio ou resultado detalhado. O logger em `finally` pode registrar status padrão antes de o servidor transformar uma exceção em 500.

O auditor sensível faz Count+Insert, sem índice composto para a consulta, sem atomicidade do contador e sem janela/retencão. “Blocked” é um resultado persistido, não um bloqueio global da conta. `ClaimTypes.Email` é lido para auditoria, mas não é emitido no JWT, então esse campo não identifica o autor por e-mail; UserId continua disponível.

Incremento: eventos de auditoria com ator, organização, alvo, motivo, correlação e resultado, sem contatos/tokens/senhas. Definir retenção e acesso ao log. Manter auditoria crítica consistente com a transação de negócio.

**28 — Testes não exercitam o mecanismo de persistência de produção. Prioridade alta de validação; confirmado.**

Os testes de integração usam Development e repositories em memória por padrão. Não há suíte PostgreSQL para migrations, FKs, índices, collation, serialização, precisão decimal ou concorrência. Os testes atuais não garantem os comportamentos dos repositories EF. O teste de SQL injection procura nomes de APIs Raw no código; é uma regra útil, não um teste abrangente de resistência a injeção.

A suíte Playwright contém cinco cenários centrados em setup/login/navegação/Viewer/refresh/logout. Não testa CRUD de clientes/planos/pedidos, Support, valores reais do dashboard, edição com contato mascarado, datas ou responsividade. Vitest cobre utilitários, não os formulários principais. Não há limiar de cobertura no CI.

Incremento: primeiro testes de regressão dos achados, depois PostgreSQL efêmero e matriz de autorização por operação, e então E2E dos percursos de cada role. A aprovação da suíte atual não elimina os problemas descritos.

**29 — Testes HTTP não forçam isolamento de configuração externa. Prioridade alta condicional.**

As factories em `ApiIntegrationTests.cs:455` e `ApiSecurityIntegrationTests.cs:234` somente definem Development. Elas não substituem explicitamente `ConnectionStrings:DefaultConnection`. Se o processo de teste herdar uma connection string real, a DI pode selecionar EF e os testes podem escrever no banco correspondente, contrariando `docs/TESTING.md`.

Nesta execução, foi verificada apenas a presença das variáveis relevantes, sem valores: não havia connection string de ambiente. O Playwright já passa connection string vazia, sendo mais explícito nesse ponto.

Incremento: substituir serviços/configuração nas factories e falhar se uma conexão não aprovada for selecionada. Testes PostgreSQL devem usar apenas a instância criada para a suíte.

**30 — Health check não verifica prontidão. Prioridade média; confirmado.**

`HealthStatusService` sempre retorna Healthy com timestamp. Render usa `/health`; banco inacessível, schema incompatível ou chave de dados inválida podem coexistir com health 200. O endpoint raiz também anuncia `/scalar/v1`, que só é mapeado em Development.

Incremento: separar liveness de readiness; readiness testa uma consulta mínima, disponibilidade das dependências críticas e compatibilidade de schema sem expor detalhes. Manter tempos limite curtos. Não usar um 200 desse endpoint como prova de persistência.

**31 — Pipeline verifica código, mas não comprova deploy concluído e saudável. Prioridade média; confirmado no workflow.**

CI já faz build/test backend, Vitest/build frontend, container e Playwright antes dos hooks. Porém, o hook só aceita o disparo; não há espera por deploy bem-sucedido, conferência do commit publicado, smoke test ou rollback. Secrets ausentes resultam em sucesso com deploy pulado, comportamento documentado.

Não há etapas de audit de dependências, secret scanning, lint ou testes PostgreSQL. Não há configuração de concurrency para impedir execuções antigas sobrepostas, permissions explícitas ou pin dos actions por SHA. Branch protection e auto-deploy nativo dos provedores são externos e não foram inspecionados; portanto não é possível afirmar que os checks sejam obrigatórios ou que todo deploy passe por eles.

Incremento: scans e PostgreSQL no CI; execução por commit/ambiente com observação do resultado; smoke test e rollback. Confirmar regras remotas e integração Git dos provedores antes de declarar o gate de publicação garantido.

**32 — Operação de banco, recuperação e chaves não está documentada suficientemente. Prioridade alta antes de depender de dados reais.**

Há duas migrations, mas o repositório não apresenta procedimento completo de execução controlada, compatibilidade entre versões, backfill, verificação pós-migração e rollback. Não há teste de restauração, RPO/RTO, calendário de retenção ou procedimento de recuperação da chave que descriptografa contatos. A presença da migration Organization no Git não comprova aplicação no Neon.

Incremento: runbook de migration com conta separada da aplicação, backup e restauração ensaiados em destino descartável, recuperação das chaves e critérios de rollback. Os privilégios reais do usuário do banco não foram inspecionados.

**33 — Endurecimento e reprodução de ambiente podem melhorar. Prioridade média/baixa.**

O Dockerfile é multi-stage, mas não seleciona usuário não root explicitamente. Não há `global.json` para fixar SDK nem lock de pacotes NuGet; patches de Microsoft.* variam entre projetos. A imagem é referenciada por tag móvel. O README sugere `docker run` sem configuração, embora a aplicação fora de Development exija secrets e banco. O frontend cai silenciosamente para localhost quando `NEXT_PUBLIC_API_BASE_URL` falta.

Incremento: usuário runtime explícito, política de SDK/dependências/imagens, fail-fast para URL pública em build de produção, exemplos de execução consistentes e validação dos parâmetros JWT/chave/CORS no startup. O teste atual usa SDK instalado, não comprova equivalência à imagem publicada.

**34 — UX, localização e manutenção. Prioridade baixa/média.**

O HTML declara `pt-BR`, mas a interface é inglesa e os formatadores usam en-US/USD. Isso afeta leitura assistiva e expectativa de moeda. Há botões de exclusão permanente imediata sem confirmação ou undo e sem bloqueio por operação pendente; é especialmente relevante para usuários/pedidos. Erros e carregamentos não oferecem consistentemente retry e anúncios acessíveis. A avaliação visual/acessibilidade completa não foi realizada nesta revisão.

`Program.cs` concentra 913 linhas. Páginas de CRUD repetem tipos, formulários, serialização de data e tratamento de erros. O contrato frontend/backend é manual. `formatDate` também lança para data inválida em vez de apresentar fallback. Configuração local do Playwright presume Opera em qualquer macOS fora do CI.

Incremento: padronizar idioma/moeda/fuso do produto, confirmar operações destrutivas, impedir duplicidade de cliques e revisar acessibilidade com teclado. Depois das correções comportamentais, extrair grupos de endpoints, tipos/validações compartilhados ou gerados por OpenAPI e componentes pequenos de tabela/formulário. Evitar uma refatoração grande misturada com mudanças de segurança.

**35 — Documentação contém afirmações inconsistentes com o estado atual. Prioridade média de planejamento.**

- O estado consolidado do roadmap diz que V10–V14 não começaram, mas Organization já está implementada e marcada como concluída na V10.
- `PROJETO_E_PLANEJAMENTO.md` encerra o estado atual na V8; o roadmap já tem V9 e início de V10.
- O domínio descrito contém PlanId/CurrentPlanId, telefone, empresa, notas e Refunded; o código usa PlanStage e não implementa esses campos/estado.
- “Alterações críticas auditadas” é mais amplo do que o audit log sensível existente.
- Suporte a campanhas e geração demo precisa distinguir código de fundação de fluxos executáveis pelo usuário.
- Exemplos e contagens de testes históricas não devem ser lidos como resultado atual ou prova de produção.

Incremento: uma tabela canônica de capacidades com status implementado, testado localmente, verificado em produção e planejado. Atualizar o roadmap apenas com evidência da etapa, sem apagar o valor do histórico.

**Aspectos que merecem ser preservados**

- Separação de domínio/aplicação/infraestrutura e DI; repositories em memória facilitam testes rápidos.
- Autorização real no backend; a proteção não depende apenas do menu frontend.
- Validação de assinatura, emissor, audiência e expiração do JWT e revalidação de usuário/status/role em cada request autenticado.
- Setup exige chave separada e não aceita role pública; implementação EF utiliza isolamento serializável para a criação inicial.
- PBKDF2 com salt aleatório, comparação em tempo constante e refresh token aleatório forte.
- AES-GCM com nonce aleatório, autenticação do ciphertext e lookup HMAC; DTO de clientes mascarado no backend.
- Ausência de composição SQL manual nas rotas/repositories inspecionados; uso de LINQ/EF.
- CORS por origens explícitas, documentação interativa somente em Development e validações de secrets fora de Development.
- TypeScript estrito, organização simples do frontend, estados de UI básicos, configuração de responsividade e suporte a reduced motion.
- CI com múltiplas camadas de testes e relatório Playwright; o guia multi-tenant já descreve bons critérios de aceitação.

**Ordem sugerida de incrementos**

Cada linha abaixo deve ser uma mudança pequena e revisável, com teste de aceitação próprio. Esta ordem ajusta prioridades sem exigir implementar toda a V10 ou toda a V11 de uma vez.

| Ordem | Incremento | Critério de aceite |
| --- | --- | --- |
| 1 | Atualizar Next/sharp e dependências sinalizadas | Lockfile corrigido; audit/build/testes; versão publicada conferida quando houver deploy |
| 2 | Corrigir RBAC de pedidos e leitura de Support | Matriz HTTP por verbo/role; Support usa a tela e só executa ações autorizadas |
| 3 | Exigir campos explícitos de role/status | JSON incompleto não cria/promove Admin |
| 4 | Preservar contato em edições comuns | Alterar status/nome sem conhecer e-mail completo; troca de contato separada |
| 5 | Corrigir dashboard | Viewer tem agregados autorizados; 403/500/offline nunca viram zero silencioso |
| 6 | Corrigir logout/refresh do navegador | Offline limpa estado; refresh pendente não restaura sessão; abas coerentes |
| 7 | PostgreSQL efêmero no CI e isolamento das factories | Migrations/constraints/round-trip executados sem chance de usar banco externo |
| 8 | Refresh atômico e tokens em hash | Apenas um consumo concorrente; rollback seguro; detecção de reutilização |
| 9 | Integridade de Admin, e-mail e relacionamentos | Sem perda do último Admin, identidade duplicada ou pedidos órfãos |
| 10 | Imutabilidade de pedidos e validação do catálogo | Pedido concluído preservado; plano inativo rejeitado na contratação |
| 11 | Datas/moeda e erros de API | Vencimento consistente entre fuso/telas; erro específico e recuperável |
| 12 | Persistência/infra seguras | Chave persistente validada, TLS verificado, readiness, restore e migration ensaiados |
| 13 | Membership/Owner e migração do tenant inicial | Dados existentes associados; usuários preservados; último Owner protegido |
| 14 | Isolamento tenant por módulo | Testes cruzados com duas organizações, incluindo relações e agregados |
| 15 | Sessões seguras, recuperação e auditoria de contas | Revogação definitiva, convites/reset seguros, autenticação recente e MFA conforme prioridade |
| 16 | Subscription e histórico comercial | Regras de contrato/snapshot/transições antes de campanhas ou indicadores recorrentes |
| 17 | Comunicação assíncrona e operação | Aprovação, outbox/idempotência, entrega, consentimento e observabilidade |

Antes de disponibilizar o produto como SaaS para empresas independentes, concluir e comprovar o isolamento da V10. Antes de ampliar indicadores e automações de cobrança, concluir a consistência do modelo de assinatura. Microservices, broker externo ou banco por tenant não são pré-requisitos para corrigir os problemas atuais.

**Reproduções adicionais — registro final**

API iniciada em Development, connection string explicitamente vazia, porta local 5197, usando o código compilado nesta revisão. Os registros são descartáveis e não pertencem ao ambiente publicado.

| Cenário HTTP | Resultado observado |
| --- | --- |
| Support cria pedido usando stage de plano inativo | 201 Created |
| Support altera valor de pedido Completed de 100 para 1, mantendo status | 200 OK; resposta com valor 1 |
| Support consulta clientes necessários à tela de pedidos | 403 Forbidden |
| Admin envia criação de usuário sem role e sem status | 201 Created; usuário Admin/Active |
| Alteração de nome do cliente sem redigitar e-mail | 400 Bad Request |
| Excluir cliente com pedido existente | 204 No Content |
| Consultar o pedido após excluir o cliente | 200 OK; referência ao cliente removido preservada |
| Support exclui pedido Completed | 204 No Content |
| Logout com sessão válida | 204 No Content |
| Reutilizar access token após logout | 200 OK em rota protegida |
| Reutilizar refresh token após logout | 401 Unauthorized |

O código real de `frontend/lib/api.ts` foi transpilado em harness isolado, com storage/fetch simulados, para exercitar cenários difíceis de rede e ordenação. Isso é reprodução do cliente HTTP, não validação desses cenários no navegador ou em produção:

| Cenário do cliente HTTP/data | Resultado observado |
| --- | --- |
| Fetch do logout rejeita por rede indisponível | Sessão local permanece |
| Logout recebe HTTP 401 | Sessão local é apagada sem erro de revogação |
| Refresh pendente retorna depois de logout concluído | Sessão é gravada novamente |
| Formatar `2026-09-20T00:00:00Z` em America/Sao_Paulo | Mostra 19/09/2026 |

Os probes não exploraram avisos de RCE nem acessaram banco de produção. As corridas de refresh/último Admin no PostgreSQL continuam como achados estáticos que precisam de testes reais de banco.
