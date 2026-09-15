// Gera o PDF de entrega da Fase 3 a partir de dados-entrega.json e modelo-entrega.html.
//
//   node docs/entrega/gerar-pdf.mjs [dados.json] [saida.pdf]
//
// Sem argumentos, lê dados-entrega.json e grava MechanicLtda-Fase3-Entrega.pdf nesta pasta.
// Usa o Chrome ou o Edge instalados, em modo headless. Para outro navegador baseado
// em Chromium, informe o executável em CHROME_PATH.
import { execFileSync } from "node:child_process";
import { existsSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const pasta = dirname(fileURLToPath(import.meta.url));
const arquivoDados = resolve(process.argv[2] ?? join(pasta, "dados-entrega.json"));
const saida = resolve(process.argv[3] ?? join(pasta, "MechanicLtda-Fase3-Entrega.pdf"));
const dados = JSON.parse(readFileSync(arquivoDados, "utf8"));
const REPOSITORIOS = ["MechanicLtda", "Lambda", "InfraKubernete", "InfraSGBD"];

const escapar = (texto) =>
  String(texto ?? "").replace(/[&<>"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" })[c]);

const pendencias = [];
const aPreencher = (campo) => {
  pendencias.push(campo);
  return '<span class="pendente">a preencher</span>';
};
const link = (url, campo) => {
  const valor = String(url ?? "").trim();
  if (!valor) return aPreencher(campo);
  if (!/^https:\/\//.test(valor)) throw new Error(`${campo} precisa começar com https://`);
  return `<a href="${escapar(valor)}">${escapar(valor)}</a>`;
};

// A ordem abaixo segue a do documento, para a lista de pendências sair na mesma ordem.
const grupo = dados.grupo?.trim() ? escapar(dados.grupo) : aPreencher("grupo");

const integrantes = (dados.integrantes ?? []).filter((i) => i.nome?.trim() || i.rm?.trim());
const linhasIntegrantes = integrantes.length
  ? integrantes.map((i, n) =>
      `<tr><td>${i.nome?.trim() ? escapar(i.nome) : aPreencher(`integrantes[${n}].nome`)}</td>` +
      `<td>${i.rm?.trim() ? escapar(i.rm) : aPreencher(`integrantes[${n}].rm`)}</td></tr>`).join("")
  : `<tr><td>${aPreencher("integrantes")}</td><td></td></tr>`;

const video = link(dados.videoUrl, "videoUrl");

const linhasSoat = REPOSITORIOS.map((repo) => {
  const item = dados.soatArchitecture?.[repo] ?? {};
  if (!item.confirmado) pendencias.push(`soatArchitecture.${repo}`);
  const situacao = item.confirmado
    ? '<span class="selo ok">Adicionado</span>'
    : '<span class="selo nao">Não confirmado</span>';
  const data = item.data?.trim() ? escapar(item.data) : item.confirmado ? aPreencher(`soatArchitecture.${repo}.data`) : "—";
  return `<tr><td class="nome">${repo}</td><td>${situacao}</td><td>${data}</td></tr>`;
}).join("");

const valores = {
  GRUPO: grupo,
  DATA_GERACAO: new Date().toLocaleDateString("pt-BR", { day: "2-digit", month: "long", year: "numeric" }),
  INTEGRANTES: linhasIntegrantes,
  VIDEO: video,
  SOAT_ROWS: linhasSoat,
  GATEWAY: link(dados.gatewayUrl, "gatewayUrl"),
  DIAGRAMA: `data:image/png;base64,${readFileSync(join(pasta, "diagrama-componentes.png")).toString("base64")}`,
};

// O aviso é montado por último, quando todas as pendências já foram coletadas.
valores.AVISO_RASCUNHO = pendencias.length
  ? `<div class="rascunho"><b>Rascunho: faltam dados antes da entrega</b><ul>${pendencias.map((p) => `<li><code>${escapar(p)}</code> em dados-entrega.json</li>`).join("")}</ul></div>`
  : "";

const html = readFileSync(join(pasta, "modelo-entrega.html"), "utf8").replace(/%%([A-Z_]+)%%/g, (marcador, chave) => {
  if (!(chave in valores)) throw new Error(`Marcador sem valor no modelo: ${marcador}`);
  return valores[chave];
});

function localizarNavegador() {
  const candidatos = [
    process.env.CHROME_PATH,
    "C:/Program Files/Google/Chrome/Application/chrome.exe",
    "C:/Program Files (x86)/Google/Chrome/Application/chrome.exe",
    "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe",
    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
    "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
    "/usr/bin/google-chrome",
    "/usr/bin/chromium",
    "/usr/bin/chromium-browser",
    "/usr/bin/microsoft-edge",
  ].filter(Boolean);
  const encontrado = candidatos.find((c) => existsSync(c));
  if (!encontrado) throw new Error("Chrome ou Edge não encontrado. Informe o executável em CHROME_PATH.");
  return encontrado;
}

const temporaria = mkdtempSync(join(tmpdir(), "entrega-fase3-"));
try {
  const arquivoHtml = join(temporaria, "entrega.html");
  writeFileSync(arquivoHtml, html, "utf8");
  execFileSync(localizarNavegador(), [
    "--headless=new",
    "--disable-gpu",
    "--no-pdf-header-footer",
    `--user-data-dir=${join(temporaria, "perfil")}`,
    `--print-to-pdf=${saida}`,
    pathToFileURL(arquivoHtml).href,
  ], { stdio: "ignore", timeout: 120_000 });
} finally {
  rmSync(temporaria, { recursive: true, force: true });
}

console.log(`PDF gerado: ${saida}`);
if (pendencias.length) {
  console.log(`\nRascunho — preencha em dados-entrega.json e gere de novo:`);
  pendencias.forEach((p) => console.log(`  - ${p}`));
}
