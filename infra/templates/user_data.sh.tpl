#!/bin/bash
set -eux
exec > >(tee /var/log/user-data.log) 2>&1

dnf update -y
dnf install -y aws-cli

# k3s: Kubernetes leve rodando na própria instância. --service-node-port-range
# amplo o suficiente para expor os Services da API (8080) e do Web (8090) via
# NodePort, batendo com o que o security group libera.
# Retry: o instalador (get.k3s.io/update.k3s.io, atrás de CDN) já falhou aqui
# antes com erro de SSL transitório; com "set -e" ativo, uma falha aqui
# abortava o script inteiro e nada mais rodava.
for i in $(seq 1 10); do
  if curl -sfL https://get.k3s.io | INSTALL_K3S_EXEC="server --service-node-port-range=8000-9000" sh -; then
    break
  fi
  echo "Falha ao instalar k3s (tentativa $i/10), tentando novamente em 15s..."
  sleep 15
done

if ! command -v /usr/local/bin/kubectl >/dev/null 2>&1; then
  echo "ERRO: k3s nao foi instalado apos 10 tentativas" >&2
  exit 1
fi

export KUBECONFIG=/etc/rancher/k3s/k3s.yaml
until /usr/local/bin/kubectl get nodes >/dev/null 2>&1; do sleep 2; done
/usr/local/bin/kubectl wait --for=condition=Ready node --all --timeout=180s

/usr/local/bin/kubectl create namespace mechanicltda --dry-run=client -o yaml \
  | /usr/local/bin/kubectl apply -f -

# Deploy da aplicação (ConfigMap/Secret/Deployments/Services/HPA) fica 100% a
# cargo da pipeline de CI/CD (.github/workflows/deploy.yml via SSM Run
# Command) — evita duas fontes de verdade para os manifestos.

# ── Refresh periódico das credenciais do ECR ────────────────────────────────
# k3s (containerd) não integra nativamente com IAM/ECR como o EKS, e o token
# de auth do ECR expira em 12h. Um timer local recria o Secret
# docker-registry "ecr-creds" a cada 6h usando a IAM role da própria
# instância; a pipeline também atualiza esse Secret a cada deploy.
cat <<'SCRIPT' > /opt/refresh-ecr-creds.sh
#!/bin/bash
set -eu
export KUBECONFIG=/etc/rancher/k3s/k3s.yaml

REGION="${region}"
REGISTRY="${ecr_registry_host}"
PASSWORD="$(aws ecr get-login-password --region "$REGION")"

/usr/local/bin/kubectl create secret docker-registry ecr-creds \
  --namespace mechanicltda \
  --docker-server="$REGISTRY" \
  --docker-username=AWS \
  --docker-password="$PASSWORD" \
  --dry-run=client -o yaml | /usr/local/bin/kubectl apply -f -
SCRIPT
chmod 700 /opt/refresh-ecr-creds.sh

cat <<'UNIT' > /etc/systemd/system/refresh-ecr-creds.service
[Unit]
Description=Atualiza o Secret docker-registry do ECR para o k3s
After=k3s.service
Requires=k3s.service

[Service]
Type=oneshot
ExecStart=/opt/refresh-ecr-creds.sh
UNIT

cat <<'TIMER' > /etc/systemd/system/refresh-ecr-creds.timer
[Unit]
Description=Roda refresh-ecr-creds.service a cada 6 horas

[Timer]
OnBootSec=1min
OnUnitActiveSec=6h

[Install]
WantedBy=timers.target
TIMER

systemctl daemon-reload
systemctl enable --now refresh-ecr-creds.timer
systemctl start refresh-ecr-creds.service
