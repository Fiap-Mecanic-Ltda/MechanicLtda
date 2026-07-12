# Manifestos Kubernetes — MechanicLtda

Manifestos para deploy da stack (`sqlserver`, `api`, `web`) em um cluster Kubernetes,
espelhando os mesmos três serviços do `docker-compose.yml` usado em desenvolvimento local.

## Estrutura

| Arquivo | Recurso | Descrição |
|---|---|---|
| `00-namespace.yaml` | Namespace | Isola todos os recursos em `mechanicltda` |
| `01-configmap.yaml` | ConfigMap | Configuração não sensível (ambiente, JWT issuer/audience, etc.) |
| `02-secret.yaml` | Secret | Segredos — connection string, `JWT_SECRET_KEY`, `ENCRYPTION_KEY`, `SA_PASSWORD` |
| `10-12` | Deployment/Service/PVC | SQL Server (instância única, com armazenamento persistente) |
| `20-22` | Deployment/Service/HPA | API (`.NET`, porta `8080`) |
| `30-32` | Deployment/Service/HPA | Web (`.NET MVC`, porta `8080`) |
| `kustomization.yaml` | — | Aplica todos os recursos acima em ordem, com um único comando |

## Pré-requisitos do cluster

- **metrics-server** instalado — os HPAs dependem dele para ler utilização de CPU/memória.
  Em clusters gerenciados (EKS/AKS/GKE) geralmente já vem habilitado; em `kind`/`minikube`,
  instale com `minikube addons enable metrics-server` ou o manifesto oficial do projeto.
- Um **StorageClass** padrão disponível para o `PersistentVolumeClaim` do SQL Server.
- As imagens `mechanicltda-api:latest` e `mechanicltda-web:latest` precisam existir onde o
  cluster consegue puxá-las:
  - Em `kind`: `kind load docker-image mechanicltda-api:latest mechanicltda-web:latest`
  - Em `minikube`: `minikube image load mechanicltda-api:latest` (idem para o web)
  - Em cluster real: publique em um registro (ECR/GCR/GHCR/ACR) e ajuste `image:` e
    `imagePullPolicy` nos Deployments (`20-api-deployment.yaml`, `30-web-deployment.yaml`).

## Segredos — não use os valores do repositório

`02-secret.yaml` traz valores `CHANGE_ME_*` apenas para o manifesto ser válido. Substitua
antes de aplicar, de preferência **sem editar o arquivo versionado**:

```bash
kubectl create namespace mechanicltda --dry-run=client -o yaml | kubectl apply -f -

kubectl create secret generic mechanicltda-secrets \
  --namespace mechanicltda \
  --from-literal=SA_PASSWORD='<senha-forte>' \
  --from-literal=JWT_SECRET_KEY='<chave-jwt-min-32-chars>' \
  --from-literal=ENCRYPTION_KEY='<chave-criptografia-min-32-chars>' \
  --from-literal=ConnectionStrings__DefaultConnection='Server=mechanicltda-sqlserver;Database=MechanicLtdaDb;User Id=sa;Password=<senha-forte>;TrustServerCertificate=True;'
```

Em produção, prefira um provedor externo (Sealed Secrets, External Secrets Operator, Key
Vault/Secrets Manager via CSI driver) em vez de Secrets nativos — eles armazenam apenas em
base64, não criptografados.

## Deploy

Com o Secret já criado à parte (passo acima), aplique o restante via Kustomize:

```bash
kubectl apply -k k8s/ --prune -l app.kubernetes.io/part-of=mechanicltda
```

Ou, sem Kustomize, os arquivos numerados garantem a ordem correta com `kubectl apply -f`:

```bash
kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/01-configmap.yaml
kubectl apply -f k8s/02-secret.yaml   # se optar por não usar `kubectl create secret`
kubectl apply -f k8s/10-sqlserver-pvc.yaml -f k8s/11-sqlserver-deployment.yaml -f k8s/12-sqlserver-service.yaml
kubectl apply -f k8s/20-api-deployment.yaml -f k8s/21-api-service.yaml -f k8s/22-api-hpa.yaml
kubectl apply -f k8s/30-web-deployment.yaml -f k8s/31-web-service.yaml -f k8s/32-web-hpa.yaml
```

Acompanhar:

```bash
kubectl get pods,svc,hpa -n mechanicltda
kubectl port-forward -n mechanicltda svc/mechanicltda-api 8080:8080
kubectl port-forward -n mechanicltda svc/mechanicltda-web 8090:8080
```

## Decisões e limitações conhecidas

- **SQL Server como Deployment, não StatefulSet**: é uma instância única com PVC
  (`strategy: Recreate` evita dois pods montando o mesmo volume `ReadWriteOnce`
  simultaneamente). Não é candidato a HPA — não faz sentido escalar horizontalmente um
  banco relacional de instância única. Para produção, prefira um banco gerenciado
  (Azure SQL, RDS) ou um StatefulSet dedicado.
- **Ordem de subida via `initContainers`**: como Deployments não têm um equivalente nativo
  ao `depends_on: condition: service_healthy` do Docker Compose, a API aguarda o SQL Server
  responder na porta `1433` e o Web aguarda `GET /health` da API, reproduzindo a mesma
  sequência (`sqlserver → api → web`).
- **`minReplicas: 1` nos HPAs — proposital**: a aplicação aplica migrations e faz o seed
  inicial do banco a cada boot (idempotente, mas o seed usa um `check-then-insert` sem lock
  distribuído — diferente das migrations do EF Core, que já usam `sp_getapplock`). Com
  `minReplicas: 1`, apenas um pod sobe no primeiro boot, evitando dois pods inserindo o
  seed em paralelo antes de qualquer um ver o banco populado. O HPA ainda escala para até
  5 réplicas sob carga normalmente — o risco de corrida só existiria se o cluster já
  nascesse com `minReplicas >= 2` e o banco estivesse vazio. Se isso for necessário no
  futuro, mova a migration/seed para um `Job` único executado antes dos Deployments.
