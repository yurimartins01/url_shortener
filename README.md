# 🔗 URL Shortener
`.NET 10` `PostgreSQL` `Docker`

Uma URL longa pode ser um problema em ambientes com limite de caracteres e pode ser quebrada por clientes de e-mail tornando o link inacessível.

O encurtador resolve isso gerando um link curto e estável que redireciona para o endereço original.

## ✨ Funcionalidades Fase 1

### ⚙️ Backend
- Receber uma URL longa e devolver um código curto.
- Acessar o código curto e ser redirecionado para a URL original -> 302 Found.
- Persistir os links em PostgreSQL.
- Aceitar apenas URLs com esquema `http` ou `https`.
- Falhar de forma previsível: URL inválida -> 400 Bad Request; código inexistente -> 404 Not Found.
- Aplicar as migrations automaticamente na inicialização da aplicação.
- Ler toda a configuração sensível a partir de variáveis de ambiente.
- Não deduplicar URLs: cada solicitação gera um novo código.

### 🎨 Frontend
- Interface de página única, sem framework e sem etapa de build.
- Campo de entrada para o usuário inserir a URL longa.
- Botão para iniciar o processo de conversão de URL.
- Área de resultado da URL curta e botão de copiar.
- Layout de coluna única, legível em telas a partir de 360px de largura.
- Script cliente responsável por consumir a API via `fetch`.

### 🐳 Conteinerização
- Container para API.
- Container para PostgreSQL.
- `healthcheck` no PostgreSQL e `depends_on: condition: service_healthy` na API.
- Arquivo `.env.example` versionado, `.env` ignorado pelo Git.

### 🧪 Testes
- Testes unitários do gerador de código curto.
- Um teste de integração do fluxo criar → redirecionar.

## 🚧 Fora do escopo na Fase 1
- Contagem de cliques e estatísticas.
- Alias customizado (código escolhido pelo usuário).
- Expiração de links.
- Rate limiting.
- Redis para cache.
- Logging estruturado (Serilog) + correlation ID.
- Cobertura ampla de testes e testes de integração com Testcontainers.
- CI com GitHub Actions.
- Versionamento de API.
- Observabilidade.
- Autenticação.
- Bloquear redirecionamento para endereços privados, loopback e hosts sem TLD.

## 🏛️ Decisões técnicas

### ✏️ Geração do código curto
 **Decisão:** Utilizar 7 caracteres aleatórios em Base62, gerados com `RandomNumberGenerator`.

**Justificativa:** os links podem apontar para conteúdos sensíveis, a aleatoriedade garante a imprevisibilidade da URL gerada mantendo a aplicação segura. Utilizar a alternativa de ID sequencial torna os códigos enumeráveis, permitindo a coleta de todos os links do sistema.

`RandomNumberGenerator` é criptograficamente seguro; `Random` é determinístico a partir da semente e tornaria os códigos previsíveis por análise, anulando a única vantagem dessa estratégia.

Base62 (`a-zA-Z0-9`) foi escolhido em vez de Base64 porque a última inclui `+` e `/`,
que exigem URL-encoding. Um encurtador que produz URLs escapadas é uma contradição.

**Trade-off aceito:** esta estratégia tem um custo de escrita maior que a sequencial, porque exige tratar colisão. O custo medido é desprezível (ver decisão de unicidade), e a alternativa sequencial só alcançaria imprevisibilidade introduzindo uma chave secreta, o que faria a segurança de todos os códigos já emitidos depender dessa chave, sem possibilidade de rotação.

### ✏️ Unicidade do código gerado
 **Decisão:** unicidade garantida via constraint `UNIQUE` no banco, com retentativa da geração em caso de violação.

**Justificativa:** verificar com `SELECT` antes de inserir não garante unicidade. Duas requisições concorrentes podem consultar ao mesmo tempo, ambas não encontrarem o código e ambas inserirem, um caso de check-then-act, que não é uma operação atômica. O banco é o único ponto serializado do sistema, portanto é ele que deve ser a autoridade sobre a unicidade.

A colisão em si é muito improvável: com 7 caracteres e um milhão de links cadastrados, a chance por inserção é de aproximadamente 0,000028%, ou seja, uma retentativa a cada ~3,5 milhões de criações. Mesmo assim, a constraint inviabiliza que uma duplicata seja persistida no banco.

Ela também se torna necessária na Fase 2, quando o alias customizado permitir que dois usuários escolham o mesmo código.

### ✏️ Não deduplicação de URLs

**Decisão:** cada cadastro de URL gera um novo código, mesmo que a URL de destino já exista no banco.

**Justificativa:** ao implementar a contagem de cliques na Fase 2 será possível determinar qual campanha tem um desempenho melhor (Instagram, Facebook, Twitter, etc.).

**Trade-off aceito:** Links duplicados ocupam espaço sem limite, compensado pelo volume baixo.
 
### ✏️ Comprimento fixo de 7 caracteres

**Decisão:** todos os códigos têm exatamente 7 caracteres.

**Justificativa:** Um comprimento variável (4 a 7 caracteres) aumentaria o espaço de chaves em apenas 1,64%. Em Base62 cada caractere adicional multiplica o espaço por 62, então o maior comprimento domina a soma e todos os menores juntos valem cerca de 1/61 do total.

Utilizando 7 caracteres e gerando 1.000 links por segundo ininterruptos, o espaço levaria 111 anos para se esgotar.

Um comprimento variável faz a resistência do sistema quanto à enumeração ser determinada pelo menor código já emitido. Um código de 4 caracteres tem ~14,7 milhões de combinações, varríveis em poucas horas. A segurança não é determinada pela média das credenciais já emitidas, mas sim pela mais fraca. 

### ✏️ Desambiguação de rota
**Decisão:** a rota de redirect é restrita por expressão regular (`^[a-zA-Z0-9]{7}$`).

**Justificativa:** a rota `GET /{code}` é um curinga na raiz do domínio e disputa espaço com `/`, `/style.css` e `/app.js`. A proteção é feita por meio da restrição: a regex faz a rota casar apenas com strings que têm forma de código, eliminando a ambiguidade por construção em vez de por ordenação. 

**Por que não por ordenação:** o hosting mínimo insere o roteamento no início do pipeline, antes de qualquer middleware registrado. Quando o roteamento seleciona um endpoint, o middleware de arquivos estáticos se cala: ele verifica se há endpoint ativo e delega adiante sem servir. Como a rota vence arquivos estáticos, a proteção não pode vir por ordenação. Então, a regex fica com a responsabilidade de impedir a seleção.

**Dependência:** esta restrição só é possível porque o comprimento é fixo. Quando a Fase 2 migrar para geração sequencial + Feistel, o encoding precisará usar padding para manter o comprimento constante.

### ✏️ Validação da URL de destino

**Decisão:** aceitar apenas URLs absolutas com esquema `http` ou `https`.

**Justificativa:** sem essa restrição o serviço permite redirecionar para esquemas perigosos ou inadequados: `javascript:`, `data:` e `file:`, tornando o encurtador um mecanismo que possibilita ataques por meio de links maliciosos.

**Limitação:** endereços privados, loopback e hosts sem TLD passam na validação. O risco não é o servidor buscar essas URLs (SSRF), e sim alguém disfarçar um link para o painel do roteador de quem clicar.

### ✏️ Status do redirecionamento

**Decisão:** retornar status code 302 Found no endpoint de redirecionamento.

**Justificativa:** o status code 301 é cacheado pelo navegador de forma agressiva, ignorando a API para links acessados mais de uma vez na mesma máquina, o que resulta na perda da capacidade de alterar ou desativar o link para quem já o visitou. Além disso, o funcionamento do contador de cliques também será prejudicado após sua implementação. O status code 307 também foi considerado, mas como o endpoint de redirect é acessado exclusivamente via GET, sua preservação torna-se irrelevante.

### ✏️ Ferramenta para servir interface

**Decisão:** servir uma página estática pelo `wwwroot` do ASP.NET, sem framework de frontend.

**Justificativa:** o uso de um framework seria desproporcional ao problema. Não por ser ruim, mas por resolver problemas que esta tela não tem.

Servir a página pela própria API traz três benefícios:

- CORS deixa de existir, porque frontend e API compartilham a mesma origem.
- A conteinerização cai para dois containers, sem estágio de build de Node, sem `nginx.conf` e sem configuração de fallback de SPA.
- Os arquivos entram na imagem automaticamente via `dotnet publish`.

**Nota:** quando a Fase 2 introduzir o painel de estatísticas de cliques, a interface passará de um formulário para uma tela com listagem, filtro e estado. Nesse momento o custo de manipular DOM manualmente será maior que o custo do framework, e a migração para Angular passa a ser justificada.

### ✏️ Conteinerização

**Decisão:** API e PostgreSQL em containers, orquestrados por `docker compose`.

**Justificativa:** o objetivo é que qualquer pessoa execute o projeto com dois comandos sem precisar ter .NET SDK ou PostgreSQL instalado em sua máquina. Os containers entregam um ambiente já montado, isolado e com as versões corretas, eliminando problemas relacionados a versionamento.

```bash
cp .env.example .env
docker compose up
```

### ✏️ Aplicação de migrations no startup

**Decisão:** as migrations do EF Core são aplicadas automaticamente na inicialização da aplicação.

**Justificativa:** quem clona o repositório não deve rodar `dotnet ef database update` manualmente. Isso quebraria a proposta de execução em apenas dois comandos.

**Limitação:** em produção com múltiplas instâncias, aplicar migration no startup gera concorrência entre réplicas tentando migrar simultaneamente. Abordagem adequada nesse cenário é um passo de migração separado do processo de deploy (por exemplo, em um pipeline de CI/CD). Para o escopo deste projeto, a simplicidade compensa.


