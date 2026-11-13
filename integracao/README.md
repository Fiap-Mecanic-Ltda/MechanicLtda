# Homologação local: MechanicLtda + OS Service + Execução

Este compose sobe o sistema da Fase 4 ligado de ponta a ponta, sem AWS:

| Container | O que é | Porta no host |
|---|---|---|
| `web` | MechanicLtda.Web, com as chaves `Integracoes:*` ligadas | 8090 |
| `api` | MechanicLtda.API (cliente, veículo, login) | 8080 |
| `os-service` | OrdemServico-service, orquestrador da Saga | 8081 |
| `execucao` | ExecucaoProducao-service (estoque, catálogo, fila da oficina) | 8082 |
| `rabbitmq` | Broker único da Saga | 5672, painel em 15672 |
| `sqlserver` | `MechanicLtdaDb` e `OsServiceDb` na mesma instância | 1433 |
| `dynamodb` | DynamoDB Local da Execução | 8000 |
| `carga-execucao` | Copia estoque e serviços do monólito para a Execução e sai | - |

O **Billing ainda é simulado** pelo OS Service (`Simuladores__Billing=true`): o
OrcamentoPagamento-service não consome os comandos da Saga pelo RabbitMQ. O "cliente" paga
sozinho quando a OS chega em Aguardando Aprovação. A Execução é a real.

## Subir

Os três repositórios lado a lado:

```text
./MechanicLtda
./OrdemServico-service      (branch desenvolvimento)
./ExecucaoProducao-service  (branch desenvolvimento)
```

```bash
cd MechanicLtda/integracao
cp .env.example .env
docker compose up --build
```

Login no Web: `admin@mechanic.com` / `Admin@123`.

## Roteiro de teste

1. **Serviços** > Novo serviço (ex.: "Troca de correia"). Ele é gravado na Execução.
2. **Ordens** > Nova ordem (cliente 1, veículo 1). A OS é aberta no OS Service, que confere
   cliente e veículo na API do monólito.
3. Na OS, **Diagnóstico**. A Saga manda `IniciarDiagnostico` para a Execução.
4. **Oficina** (aparece no menu com a Execução ligada): a OS está em `AguardandoDiagnostico`.
   Abra, **Assumir o diagnóstico**, escolha uma peça (com valor unitário) e o serviço do
   catálogo e **Concluir diagnóstico**. Em **Estoque**, a peça aparece reservada.
5. A Saga segue sozinha: orçamento gerado, pagamento confirmado (simulado), OS em
   **Execução** e a oficina recebe a OS em `NaFila`. O estoque reservado é baixado.
6. Na oficina, **Iniciar o reparo** e **Finalizar o reparo**. A OS fica **Finalizada**.
7. Na OS, **Entregar**. O histórico mostra cada passo e se veio do usuário ou da Saga.

Cenários de falha do Billing simulado: coloque `[recusar]` ou `[falha-pagamento]` na
descrição de um item do diagnóstico (campo Complemento).

## Chaves

No `.env`:

| Variável | Efeito |
|---|---|
| `WEB_USA_OS_SERVICE=false` | O Web volta a usar as OS deste repositório |
| `WEB_USA_EXECUCAO=false` | O Web volta a usar estoque e serviços deste repositório |
| `SAGA_HABILITADA=false` | OS e Execução funcionam sozinhos; a oficina ganha os botões manuais |
| `MODO_AUTOMATICO=true` | A Execução avança diagnóstico e reparo sozinha |

Fora do compose, a mesma configuração vale para o Web em qualquer ambiente:

```text
Integracoes__OsService__Habilitado=true
Integracoes__OsService__BaseUrl=http://<OS Service ou API Gateway>
Integracoes__ExecucaoService__Habilitado=true
Integracoes__ExecucaoService__BaseUrl=http://<Execução ou API Gateway>
```

Desligadas (o padrão), nada muda no comportamento do Web.
