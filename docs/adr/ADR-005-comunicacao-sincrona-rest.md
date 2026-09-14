# ADR-005 — Comunicação síncrona por HTTP/REST, sem mensageria

- **Status:** aceito
- **Data:** 2026-09-13

## Contexto

O sistema tem três pontos de entrada: a API REST (consumida por Postman, Swagger e integrações),
o Web administrativo em Razor e os links de aprovação de OS enviados por e-mail. Os fluxos de
negócio — abrir OS, diagnosticar, orçar, aprovar, executar, finalizar e entregar — são curtos,
iniciados por uma pessoa, e cada passo precisa de resposta imediata ("a OS foi aprovada?").

O único efeito colateral externo é o envio de e-mail pelo SMTP do Gmail, em cada mudança de
status e no pedido de aprovação.

## Decisão

1. **Consumidores da API falam com o sistema por HTTP/REST com JSON**, pelo API Gateway
   (HTTPS), que encaminha ao ALB interno e aos pods da API. O Web administrativo é acessado
   pelo navegador direto na porta 8090; colocá-lo atrás do gateway é uma decisão em aberto.
2. **A API e o Web compartilham as camadas de aplicação e domínio no mesmo processo.** O Web
   não chama a API por HTTP: seus controllers usam os mesmos `AppServices`, registrados pelo
   `MechanicLtda.Bootstrap`. É um monólito modular com dois hosts.
3. **Não há broker de mensagens.** O e-mail é enviado de forma síncrona dentro da requisição,
   como *best-effort*: uma falha no SMTP é registrada (log e evento `FalhaIntegracao`) e não
   desfaz a transição de status.
4. **Autenticação fica fora do cluster**, no gateway e na Lambda, também por chamada síncrona.

## Alternativas

| Alternativa | Por que não agora |
|---|---|
| Microsserviços com chamadas HTTP entre si | Um único contexto de negócio e um único banco: dividir traria latência e falhas de rede sem ganho de autonomia de deploy |
| Eventos com fila (SQS/SNS) para notificações | Resolveria o acoplamento do e-mail à requisição e daria retry, mas adiciona fila, worker ou Lambda consumidora e tratamento de mensagens duplicadas para um volume de poucos e-mails por dia |
| gRPC entre componentes | Não há comunicação interna entre serviços que justifique contrato binário |

## Consequências

- Rastreio simples: uma requisição tem um único `X-Correlation-Id` do gateway até o log da API.
- A latência do SMTP entra no tempo de resposta das transições de status que enviam e-mail.
- Sem retry: um e-mail que falha não é reenviado. A falha aparece no painel de integrações e
  dispara alerta.
- **Evolução prevista.** O desafio cita soluções serverless também para notificações. O caminho
  natural é publicar a mudança de status numa fila SQS e enviar o e-mail por uma Lambda com SES,
  com retry e fila de mensagens mortas — sem mudar o contrato REST da API.
