#!/bin/bash
set -eux
exec > >(tee /var/log/user-data.log) 2>&1

dnf update -y
dnf install -y docker aws-cli
systemctl enable --now docker

aws ecr get-login-password --region ${region} | docker login --username AWS --password-stdin ${ecr_repo_url}

install -d -m 700 /opt/mechanicltda

get_param() {
  aws ssm get-parameter --region ${region} --with-decryption --name "$1" --query 'Parameter.Value' --output text
}

{
  echo "ConnectionStrings__DefaultConnection=$(get_param ${ssm_path_prefix}/db-connection-string)"
  echo "JWT_SECRET_KEY=$(get_param ${ssm_path_prefix}/jwt-secret-key)"
  echo "Encryption__CpfCnpjKey=$(get_param ${ssm_path_prefix}/encryption-key)"
  echo "EmailSettings__Password=$(get_param ${ssm_path_prefix}/email-password)"
  echo "AppSettings__BaseUrlAprovacao=$(get_param ${ssm_path_prefix}/app-base-url-aprovacao)"
} > /opt/mechanicltda/app.env

chmod 600 /opt/mechanicltda/app.env
chown root:root /opt/mechanicltda/app.env

# A imagem pode ainda não ter sido publicada no ECR quando a instância sobe;
# tenta por até 10 minutos antes de desistir.
for i in $(seq 1 60); do
  if docker pull ${ecr_repo_url}:${image_tag}; then
    break
  fi
  sleep 10
done

docker run -d \
  --name mechanicltda-api \
  --restart unless-stopped \
  -p 8080:8080 \
  --env-file /opt/mechanicltda/app.env \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:8080 \
  ${ecr_repo_url}:${image_tag}
