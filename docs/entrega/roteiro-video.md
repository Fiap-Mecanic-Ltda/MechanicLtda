# Roteiro do vídeo — Fase 3

Vídeo de até **15 minutos** mostrando a Fase 3 funcionando na AWS.

O que ele precisa cobrir:

- autenticação por CPF;
- execução do pipeline de CI/CD;
- deploy automatizado;
- consumo das APIs protegidas;
- painel de monitoramento com análise ao vivo;
- logs e traces em execução.

O roteiro soma **14 min 30 s** e deixa 30 s de folga. Os tempos são de fala. A pipeline leva
alguns minutos para rodar, por isso é disparada no bloco 2, fica rodando enquanto os blocos 3 e 4
são apresentados e é retomada no bloco 5.

| # | Bloco | Início | Duração | Requisito coberto |
|---|---|---|---|---|
| 1 | Abertura e arquitetura | 00:00 | 1 min 30 s | Visão da solução |
| 2 | Pull request e disparo da pipeline | 01:30 | 2 min | CI/CD |
| 3 | Autenticação por CPF | 03:30 | 2 min | Autenticação por CPF |
| 4 | APIs protegidas e fluxo da OS | 05:30 | 3 min | Consumo das APIs protegidas |
| 5 | Resultado da pipeline e deploy | 08:30 | 1 min 30 s | Deploy automatizado |
| 6 | Painel e análise ao vivo | 10:00 | 2 min 30 s | Monitoramento |
| 7 | Logs e traces | 12:30 | 1 min 30 s | Logs e traces |
| 8 | Encerramento | 14:00 | 30 s | Documentação |

---

## Antes de gravar

### Ambiente

- [ ] Os PRs de todos os repositórios estão mesclados na `main` e as pipelines de `main` terminaram
      verdes.
- [ ] InfraKubernete aplicado com `expose_nodeport_publicly = false` e
      `app_base_url_aprovacao` igual à URL do gateway.
- [ ] Smoke test da Lambda verde no último deploy.
- [ ] New Relic recebendo dados há pelo menos 15 minutos. Rode a coleção Postman três ou quatro
      vezes pelo Runner antes de gravar, para o painel não aparecer vazio.
- [ ] Proteção de branches aplicada com `scripts/proteger-branches.sh --convidar-soat`.
- [ ] Uma alteração pequena e segura pronta numa branch `feature/demo-video` do repositório
      MechanicLtda. Exemplo: a mensagem de log de abertura de OS. O PR para `desenvolvimento`
      já está aberto e com a check verde.

### Telas abertas, nesta ordem

1. Diagrama de componentes: `docs/arquitetura/01-diagrama-de-componentes.md` no GitHub.
2. GitHub, repositório MechanicLtda, com o PR da demonstração.
3. GitHub Actions de MechanicLtda, InfraKubernete e Lambda, cada um numa aba.
4. Postman com a coleção e o ambiente selecionados:
   - `emailDemonstracao` preenchido;
   - console aberto em **View → Show Postman Console**.
5. Caixa de e-mail do `emailDemonstracao`.
6. New Relic nas seguintes telas:
   - painel **MechanicLtda - mechanicltda-prod**;
   - **APM → mechanicltda-api**;
   - **Logs**;
   - **Alerts**.
7. AWS Console, CloudWatch → Logs Insights, com o grupo `/aws/apigateway/mechanicltda-prod-auth`
   selecionado.
8. Terminal com `BASE_URL` e `EC2_IP` exportados.

### Cuidados com dados sensíveis

- Não abrir o SSM Parameter Store nem os secrets do GitHub.
- No Postman, a senha fica na variável `senhaAdmin` do tipo *secret*.
- Não colar tokens em sites de terceiros. Os testes da coleção já decodificam e conferem as claims.
- Zoom de 125% no navegador e fonte grande no terminal.

---

## 1. Abertura e arquitetura — 00:00 a 01:30

**Tela:** diagrama de componentes no GitHub.

**Fala:**

> Este é o MechanicLtda na Fase 3. Toda requisição externa entra pelo AWS API Gateway.
>
> - As rotas de autenticação vão para uma Lambda.
> - As rotas da aplicação passam por um Lambda authorizer e seguem, por VPC Link e ALB interno,
>   até a API .NET no cluster k3s.
> - O banco é SQL Server no RDS, em sub-rede privada.
>
> O projeto está dividido em quatro repositórios:
>
> - aplicação;
> - Lambda e gateway;
> - infraestrutura do cluster;
> - banco.
>
> Cada repositório tem a própria pipeline. As decisões estão registradas em quatro RFCs e onze ADRs.

**Mostrar rapidamente:** a organização `Fiap-Mecanic-Ltda` com os quatro repositórios.

## 2. Pull request e disparo da pipeline — 01:30 a 03:30

**Tela:** PR `feature/demo-video → desenvolvimento`.

1. Mostrar a proteção de branch: merge bloqueado até a check **Build e testes** passar.
2. Mesclar o PR em `desenvolvimento`. Mostrar que essa branch roda só build e testes.
3. Mesclar os PRs `desenvolvimento → homologacao` e `homologacao → main`, abertos antes da
   gravação e com a check já verde.
4. Abrir **Actions → Esteira de CI/CD - MechanicLtda (aplicacao)** e mostrar os jobs começando:
   **Build e testes**, depois **Build e push das imagens no ECR**.

**Fala:**

> Ninguém faz commit direto na `main`. A feature sobe por PR para `desenvolvimento`, depois
> `homologacao` e por fim `main`, e só a `main` faz deploy. Optamos por não manter um ambiente de homologação na
> AWS, e o motivo está na ADR-009.
>
> Com o merge, a pipeline roda os testes, publica as imagens no ECR com a tag do commit e dispara
> o deploy no repositório do cluster. Enquanto isso roda, vamos usar a aplicação.

## 3. Autenticação por CPF — 03:30 a 05:30

**Tela:** Postman, pasta **01 · Autenticação do cliente por CPF**.

Executar uma requisição por vez, mostrando o corpo da resposta e a aba **Test Results**:

| Requisição | Mostrar |
|---|---|
| CPF com dígito verificador inválido | 400 `CPF invalido.`, sem consulta ao banco |
| Cliente inativo | 401 com a mesma mensagem do CPF não cadastrado |
| CPF válido não cadastrado | 401, idêntico ao anterior |
| CPF válido e cliente ativo | 200 com `token`, `expiracao` e `cliente`; nos testes, role `Cliente` e `clienteId` no token |
| CPF de outro cliente | 200; guarda o segundo `clienteId` para o teste de posse |

**Fala:**

> A Lambda valida os dígitos do CPF. Em seguida procura o cliente por um hash HMAC do CPF, porque
> o CPF fica cifrado no banco com IV aleatório e não pode ser pesquisado direto. Por fim, confere
> se o cliente está ativo.
>
> Cliente inativo e CPF inexistente recebem exatamente a mesma resposta, para ninguém descobrir
> quem é cliente da oficina testando CPFs. O token vale 30 minutos e carrega o `clienteId`.

**Opcional, 20 s:** no terminal, rodar o comando de rajada do `docs/postman/README.md` e mostrar os
`429` do throttling.

## 4. APIs protegidas e fluxo da OS — 05:30 a 08:30

**Tela:** Postman, pasta **03 · Rotas protegidas**.

| Requisição | Resposta | Quem decide |
|---|---|---|
| Sem token | 401 `Unauthorized` | Gateway |
| Token inválido | 403 `Forbidden` | Lambda authorizer |
| Cliente consulta as próprias OS | 200 | Passa pelos dois |
| Cliente tenta consultar OS de outro cliente | 403 com `errors` | API: verificação de posse |
| Cliente tenta acessar rota administrativa | 403 `Forbidden` | Lambda authorizer |

Mostrar o header `X-Correlation-Id` nas respostas que chegaram à API. Ele não aparece nas que
pararam no gateway.

**Terminal, 15 s:** acesso direto ao cluster bloqueado.

```bash
curl -m 5 "http://$EC2_IP:8080/health"
```

A saída esperada é `Connection timed out`, porque a porta 8080 só aceita o ALB.

**Tela:** Postman, pasta **04 · Fluxo completo da ordem de serviço**.

1. Executar de **Ler cadastro do cliente** até **Enviar para aprovação do cliente**.
2. Mostrar o status mudando a cada passo e o PDF do orçamento.
3. Abrir a caixa de e-mail e mostrar o e-mail de aprovação. O link aponta para a URL do gateway.
4. Clicar em **Aprovar** no e-mail e mostrar a resposta `Ordem de Serviço aprovada com sucesso.`
5. Executar **Cliente acompanha a OS** para mostrar a OS em *Execução*.
6. Executar **Finalizar** e **Entregar**.

**Fala:**

> A autorização tem duas camadas. O gateway autentica e filtra a role pela rota. A API confere
> se o cliente está pedindo os próprios dados. O link do e-mail é uma rota pública do gateway,
> protegida por um token de uso único.

## 5. Resultado da pipeline e deploy — 08:30 a 10:00

**Tela:** GitHub Actions.

1. MechanicLtda: a esteira verde, com o passo **Dispara o deploy no cluster**.
2. InfraKubernete → **Deploy no Kubernetes (k3s)**: a execução disparada por `repository_dispatch`
   com a tag do commit. No passo final, mostrar `deployment "mechanicltda-api" successfully rolled out`.
3. Lambda → **CI/CD - Lambda de Autenticacao**: a última execução da `main`. Mostrar o
   **Terraform apply** e o **Smoke test do gateway**, que repete pelo gateway os testes de 400,
   401 e 403.

**Fala:**

> O merge na `main` publicou as imagens e disparou o deploy sem nenhum passo manual. O manifesto
> é aplicado no cluster pelo SSM, e o job só termina quando o rollout conclui. O repositório da
> Lambda faz o mesmo com o Terraform e testa o gateway depois de cada apply.

**Opcional:** chamar `GET /health` no Postman para mostrar que a API continuou respondendo durante
o rollout.

## 6. Painel e análise ao vivo — 10:00 a 12:30

**Antes do bloco:** no Postman, iniciar o **Runner** com a pasta 03 em 20 iterações, para gerar
tráfego enquanto o painel é apresentado.

**Tela:** New Relic, painel **MechanicLtda - mechanicltda-prod**. Ajustar o período para
*Last 30 minutes*.

| Página | Mostrar |
|---|---|
| Ordens de servico | A OS criada no bloco 4 em *OS abertas hoje*, e o tempo em cada etapa |
| APIs | Latência p50, p95 e p99 subindo com o Runner, throughput e taxa de erro, e disponibilidade do `/health` pelo monitor sintético |
| Integracoes | Falhas de e-mail (esperado: zero) e tempo no SQL Server por requisição |
| Kubernetes | CPU e memória por pod, réplicas do HPA e reinícios |

**Análise ao vivo:** abrir **Query your data** e rodar:

```sql
SELECT count(*), percentile(duration * 1000, 95) AS 'p95 ms'
FROM Transaction WHERE appName = 'mechanicltda-api'
FACET name SINCE 10 minutes ago
```

```sql
SELECT tipo, statusAnteriorDescricao, statusNovoDescricao, minutosNoStatusAnterior
FROM OrdemServicoEvento SINCE 30 minutes ago
```

A primeira consulta mostra a rota do cliente no topo, por causa do Runner. A segunda mostra a
sequência de transições da OS do bloco 4, com o tempo gasto em cada status.

**Tela:** **Alerts**, com as 8 condições da política e o monitor sintético.

**Fala:**

> O agente APM está nos pods da API e do Web, e a integração Kubernetes coleta CPU, memória e
> réplicas. Cada transição de OS vira um evento de negócio, e é assim que o painel mede o tempo
> em cada etapa. Os alertas cobrem latência, taxa de erro, falha de OS, falha de e-mail, recursos
> dos containers e a disponibilidade medida de fora, pelo gateway.

## 7. Logs e traces — 12:30 a 14:00

**Tela:** New Relic, **APM → mechanicltda-api → Distributed tracing**.

1. Abrir o trace de uma transição de OS do bloco 4 (`PATCH .../aguardar-aprovacao`) e mostrar os
   spans da API e das consultas SQL. O envio por SMTP não gera span, mas o tempo dele aparece na
   duração total da transação.
2. Na mesma transação, abrir **Logs** (*logs in context*). Mostrar a linha JSON com `level`,
   `message` e `context.CorrelationId`.
3. Copiar o `CorrelationId`.

**Tela:** CloudWatch → Logs Insights, grupo `/aws/apigateway/mechanicltda-prod-auth`.

```text
fields @timestamp, rota, status, latencia, latenciaIntegracao, erroAuthorizer
| filter requestId = "<CorrelationId copiado>"
```

**Fala:**

> O gateway gera um `requestId` para cada chamada e o repassa à API no header
> `X-Correlation-Id`. O mesmo ID está no access log do gateway, no log JSON da API e no trace do
> New Relic. Com ele, dá para seguir uma requisição da borda até o banco.

**Opcional:** no grupo `/aws/lambda/mechanicltda-prod-auth-authorizer`, mostrar a decisão do
authorizer para o 403 do bloco 4.

## 8. Encerramento — 14:00 a 14:30

**Tela:** `docs/arquitetura/README.md` no GitHub.

**Fala:**

> A documentação está no repositório da aplicação:
>
> - diagramas de componentes e de sequência;
> - justificativa do banco com o diagrama ER;
> - quatro RFCs e onze ADRs;
> - a coleção Postman usada neste vídeo.
>
> Os links dos quatro repositórios e deste vídeo estão no PDF de entrega.

---

## Plano B

| Problema na gravação | Alternativa |
|---|---|
| E-mail de aprovação não chega | Executar **Aprovar pela oficina** na pasta 04 e citar o link público na fala |
| Pipeline ainda rodando no bloco 5 | Mostrar a execução anterior da `main`, já verde, e voltar à atual no fim do bloco 7 |
| Painel sem dados recentes | Aumentar o período para *Last 3 hours*; o Runner do bloco 6 repõe os dados em 1 ou 2 minutos |
| Cold start da Lambda na primeira chamada | Executar a pasta 01 uma vez antes de começar a gravar |
