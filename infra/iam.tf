data "aws_caller_identity" "current" {}

data "aws_kms_alias" "ssm" {
  name = "alias/aws/ssm"
}

data "aws_iam_policy_document" "ec2_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ec2.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "ec2_instance_role" {
  name               = "${var.project_name}-${var.environment}-ec2-role"
  assume_role_policy = data.aws_iam_policy_document.ec2_assume_role.json
}

data "aws_iam_policy_document" "ecr_pull" {
  statement {
    sid       = "EcrAuth"
    effect    = "Allow"
    actions   = ["ecr:GetAuthorizationToken"]
    resources = ["*"]
  }

  statement {
    sid    = "EcrPull"
    effect = "Allow"
    actions = [
      "ecr:BatchGetImage",
      "ecr:GetDownloadUrlForLayer",
      "ecr:BatchCheckLayerAvailability",
    ]
    resources = [aws_ecr_repository.api.arn, aws_ecr_repository.web.arn]
  }
}

resource "aws_iam_role_policy" "ecr_pull" {
  name   = "${var.project_name}-${var.environment}-ecr-pull"
  role   = aws_iam_role.ec2_instance_role.id
  policy = data.aws_iam_policy_document.ecr_pull.json
}

data "aws_iam_policy_document" "ssm_read" {
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

resource "aws_iam_role_policy" "ssm_read" {
  name   = "${var.project_name}-${var.environment}-ssm-read"
  role   = aws_iam_role.ec2_instance_role.id
  policy = data.aws_iam_policy_document.ssm_read.json
}

resource "aws_iam_instance_profile" "ec2_profile" {
  name = "${var.project_name}-${var.environment}-ec2-profile"
  role = aws_iam_role.ec2_instance_role.name
}

resource "aws_iam_role_policy_attachment" "ssm_core" {
  role       = aws_iam_role.ec2_instance_role.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

# Acesso via SSM Session Manager (sem SSH/porta 22 aberta).
# IAM clássico, não Identity Center: Permission Sets/Account Assignments só
# são suportados em Organization instances do Identity Center, não na Account
# instance usada aqui (confirmado por erro real da API: "This operation is
# not supported for account instances of IAM Identity Center").

data "aws_iam_policy_document" "ssm_session_access" {
  statement {
    sid       = "StartSession"
    effect    = "Allow"
    actions   = ["ssm:StartSession"]
    resources = [aws_instance.app.arn]
  }

  statement {
    sid       = "SessionSelfManage"
    effect    = "Allow"
    actions   = ["ssm:TerminateSession", "ssm:ResumeSession"]
    resources = ["arn:aws:ssm:*:*:session/$${aws:userid}-*"]
  }

  statement {
    sid       = "SessionDescribe"
    effect    = "Allow"
    actions   = ["ssm:DescribeSessions", "ssm:GetConnectionStatus", "ec2:DescribeInstances"]
    resources = ["*"]
  }
}

resource "aws_iam_group" "ssm_users" {
  name = "${var.project_name}-${var.environment}-ssm-users"
}

resource "aws_iam_group_policy" "ssm_session" {
  name   = "${var.project_name}-${var.environment}-ssm-session"
  group  = aws_iam_group.ssm_users.name
  policy = data.aws_iam_policy_document.ssm_session_access.json
}

# Permite ao terraform-deployer usar EC2 Instance Connect para debug direto
# via SSH, sem depender do key pair original (perdido) nem do SSM (que
# ainda nao registrou a instancia - motivo desta propria investigacao).
data "aws_iam_policy_document" "ec2_instance_connect" {
  statement {
    sid       = "SendSshPublicKey"
    effect    = "Allow"
    actions   = ["ec2-instance-connect:SendSSHPublicKey"]
    resources = [aws_instance.app.arn]
  }
}

resource "aws_iam_user_policy" "ec2_instance_connect" {
  name   = "${var.project_name}-instance-connect-debug"
  user   = "terraform-deployer"
  policy = data.aws_iam_policy_document.ec2_instance_connect.json
}
