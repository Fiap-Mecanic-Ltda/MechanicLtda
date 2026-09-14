# Entrega da Fase 3

| Arquivo | Conteúdo |
|---|---|
| [roteiro-video.md](roteiro-video.md) | Roteiro do vídeo de até 15 minutos, com tempos, falas, telas e plano B |
| [MechanicLtda-Fase3-Entrega.pdf](MechanicLtda-Fase3-Entrega.pdf) | PDF de entrega, gerado a partir dos dois arquivos abaixo |
| [dados-entrega.json](dados-entrega.json) | Dados que só o time tem: grupo, integrantes, link do vídeo, URL do gateway e confirmação do `soat-architecture` |
| [modelo-entrega.html](modelo-entrega.html) | Layout e texto do PDF |
| [gerar-pdf.mjs](gerar-pdf.mjs) | Junta dados e modelo e imprime o PDF com o Chrome ou o Edge em modo headless |
| [diagrama-componentes.png](diagrama-componentes.png) | Primeiro diagrama de [01 — Diagrama de componentes](../arquitetura/01-diagrama-de-componentes.md), renderizado para o PDF |

## Gerar o PDF final

1. Preencha `dados-entrega.json`:

   ```json
   {
     "grupo": "Grupo 12",
     "integrantes": [{ "nome": "Nome completo", "rm": "RM000000" }],
     "videoUrl": "https://www.youtube.com/watch?v=...",
     "gatewayUrl": "https://<api-id>.execute-api.us-east-1.amazonaws.com",
     "soatArchitecture": {
       "MechanicLtda": { "confirmado": true, "data": "15/09/2026" }
     }
   }
   ```

   Marque `confirmado: true` só depois de conferir, em **Settings → Collaborators** de cada
   repositório, que o `soat-architecture` aparece na lista. O convite pode ser enviado com
   `bash scripts/proteger-branches.sh --convidar-soat`.

2. Gere o PDF na raiz do repositório:

   ```bash
   node docs/entrega/gerar-pdf.mjs
   ```

Enquanto faltar algum dado, o PDF sai com a faixa **Rascunho** na primeira página e o comando
lista o que falta. Links precisam começar com `https://`.

O Chrome e o Edge são encontrados nos caminhos padrão de Windows, macOS e Linux; para outro
navegador baseado em Chromium, defina `CHROME_PATH`.

## Atualizar o diagrama

Se o diagrama de componentes mudar, renderize de novo o primeiro bloco Mermaid do documento
com fonte de 18px:

```bash
npx -p @mermaid-js/mermaid-cli mmdc -i componentes.mmd -o docs/entrega/diagrama-componentes.png \
  -b white -s 2 -w 1600 -c mermaid-config.json
```

`mermaid-config.json`: `{"theme":"default","themeVariables":{"fontSize":"18px"}}`.
