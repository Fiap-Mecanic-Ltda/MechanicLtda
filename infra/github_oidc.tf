# Permite a pipeline do GitHub Actions assumir uma role via OIDC (sem access
# keys estáticas como GitHub Secret). Requer que o provider ainda não exista
# na conta — se outro projeto já criou
# "token.actions.githubusercontent.com", importe o recurso existente em vez
# de tentar criar um novo (`terraform import aws_iam_openid_connect_provider.github ...`).

data "tls_certificate" "github_actions" {
  url = "https://token.actions.githubusercontent.com/.well-known/openid-configuration"
}

resource "aws_iam_openid_connect_provider" "github" {
  url             = "https://token.actions.githubusercontent.com"
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = [data.tls_certificate.github_actions.certificates[0].sha1_fingerprint]
}

data "aws_iam_policy_document" "github_actions_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github.arn]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    # Restringe a role a workflows deste repositório rodando na branch main
    # (push/workflow_dispatch) OU em jobs que referenciam o Environment
    # "production" (o GitHub troca o formato do sub claim nesse caso).
    condition {
      test     = "StringLike"
      variable = "token.actions.githubusercontent.com:sub"
      values = [
        "repo:${var.github_repository}:ref:refs/heads/main",
        "repo:${var.github_repository}:environment:production",
      ]
    }
  }
}

resource "aws_iam_role" "github_actions" {
  name               = "${var.project_name}-${var.environment}-github-actions"
  assume_role_policy = data.aws_iam_policy_document.github_actions_assume_role.json
}

data "aws_iam_policy_document" "github_actions_ecr_push" {
  statement {
    sid       = "EcrAuth"
    effect    = "Allow"
    actions   = ["ecr:GetAuthorizationToken"]
    resources = ["*"]
  }

  statement {
    sid    = "EcrPush"
    effect = "Allow"
    actions = [
      "ecr:BatchGetImage",
      "ecr:GetDownloadUrlForLayer",
      "ecr:BatchCheckLayerAvailability",
      "ecr:InitiateLayerUpload",
      "ecr:UploadLayerPart",
      "ecr:CompleteLayerUpload",
      "ecr:PutImage",
    ]
    resources = [aws_ecr_repository.api.arn, aws_ecr_repository.web.arn]
  }
}

resource "aws_iam_role_policy" "github_actions_ecr_push" {
  name   = "${var.project_name}-${var.environment}-github-actions-ecr-push"
  role   = aws_iam_role.github_actions.id
  policy = data.aws_iam_policy_document.github_actions_ecr_push.json
}

data "aws_iam_policy_document" "github_actions_ssm_deploy" {
  statement {
    sid    = "SendCommand"
    effect = "Allow"
    actions = [
      "ssm:SendCommand",
    ]
    resources = [
      aws_instance.app.arn,
      "arn:aws:ssm:${var.aws_region}::document/AWS-RunShellScript",
    ]
  }

  statement {
    sid    = "ReadCommandResult"
    effect = "Allow"
    actions = [
      "ssm:GetCommandInvocation",
      "ssm:ListCommandInvocations",
    ]
    resources = ["*"]
  }
}

resource "aws_iam_role_policy" "github_actions_ssm_deploy" {
  name   = "${var.project_name}-${var.environment}-github-actions-ssm-deploy"
  role   = aws_iam_role.github_actions.id
  policy = data.aws_iam_policy_document.github_actions_ssm_deploy.json
}

# Lê os parâmetros da aplicação (ssm.tf) pra montar o Secret do Kubernetes a
# cada deploy — mesmo escopo já concedido à role da EC2 em iam.tf.
data "aws_iam_policy_document" "github_actions_ssm_read_app_params" {
  statement {
    sid    = "SsmReadAppParams"
    effect = "Allow"
    actions = [
      "ssm:GetParameter",
      "ssm:GetParameters",
    ]
    resources = [
      "arn:aws:ssm:${var.aws_region}:${data.aws_caller_identity.current.account_id}:parameter/${var.project_name}/${var.environment}/*",
    ]
  }

  statement {
    sid       = "SsmKmsDecrypt"
    effect    = "Allow"
    actions   = ["kms:Decrypt"]
    resources = [data.aws_kms_alias.ssm.target_key_arn]
  }
}

resource "aws_iam_role_policy" "github_actions_ssm_read_app_params" {
  name   = "${var.project_name}-${var.environment}-github-actions-ssm-read"
  role   = aws_iam_role.github_actions.id
  policy = data.aws_iam_policy_document.github_actions_ssm_read_app_params.json
}

# Leitura do state remoto (S3) — necessária pra `terraform output` na pipeline
# resolver ec2_instance_id, ssm_path_prefix e as URLs do ECR a partir do que
# foi de fato provisionado (em vez de hardcodar esses valores no workflow).
# Somente leitura: `terraform output` não grava nem trava o state.
data "aws_iam_policy_document" "github_actions_tfstate_read" {
  statement {
    sid       = "TfStateGetObject"
    effect    = "Allow"
    actions   = ["s3:GetObject"]
    resources = ["arn:aws:s3:::mechanicltda-terraform-state-430606112709/prod/terraform.tfstate"]
  }

  statement {
    sid       = "TfStateListBucket"
    effect    = "Allow"
    actions   = ["s3:ListBucket"]
    resources = ["arn:aws:s3:::mechanicltda-terraform-state-430606112709"]
  }
}

resource "aws_iam_role_policy" "github_actions_tfstate_read" {
  name   = "${var.project_name}-${var.environment}-github-actions-tfstate-read"
  role   = aws_iam_role.github_actions.id
  policy = data.aws_iam_policy_document.github_actions_tfstate_read.json
}
