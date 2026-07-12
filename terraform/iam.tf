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
# Requer o IAM Identity Center já habilitado manualmente no console.

data "aws_ssoadmin_instances" "this" {}

resource "aws_identitystore_group" "ssm_users" {
  identity_store_id = data.aws_ssoadmin_instances.this.identity_store_ids[0]
  display_name      = "${var.project_name}-${var.environment}-ssm-users"
  description       = "Usuarios com acesso via SSM Session Manager a instancia da API"
}

resource "aws_ssoadmin_permission_set" "ssm_session" {
  name             = "${var.project_name}-${var.environment}-ssm-session"
  instance_arn     = data.aws_ssoadmin_instances.this.arns[0]
  session_duration = "PT4H"
}

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

resource "aws_ssoadmin_permission_set_inline_policy" "ssm_session" {
  instance_arn       = data.aws_ssoadmin_instances.this.arns[0]
  permission_set_arn = aws_ssoadmin_permission_set.ssm_session.arn
  inline_policy      = data.aws_iam_policy_document.ssm_session_access.json
}

resource "aws_ssoadmin_account_assignment" "ssm_session" {
  instance_arn       = data.aws_ssoadmin_instances.this.arns[0]
  permission_set_arn = aws_ssoadmin_permission_set.ssm_session.arn

  principal_id   = aws_identitystore_group.ssm_users.group_id
  principal_type = "GROUP"

  target_id   = data.aws_caller_identity.current.account_id
  target_type = "AWS_ACCOUNT"
}

# Permite ao usuario que roda o Terraform (terraform-deployer) gerenciar
# os recursos de Identity Center acima.

data "aws_iam_policy_document" "sso_admin_deploy" {
  statement {
    sid    = "SsoAdminManage"
    effect = "Allow"
    actions = [
      "sso:ListInstances",
      "sso:CreatePermissionSet",
      "sso:DescribePermissionSet",
      "sso:UpdatePermissionSet",
      "sso:DeletePermissionSet",
      "sso:ListPermissionSets",
      "sso:PutInlinePolicyToPermissionSet",
      "sso:GetInlinePolicyForPermissionSet",
      "sso:DeleteInlinePolicyFromPermissionSet",
      "sso:ProvisionPermissionSet",
      "sso:DescribePermissionSetProvisioningStatus",
      "sso:CreateAccountAssignment",
      "sso:DeleteAccountAssignment",
      "sso:DescribeAccountAssignmentCreationStatus",
      "sso:DescribeAccountAssignmentDeletionStatus",
      "sso:ListAccountAssignments",
      "sso:TagResource",
      "sso:UntagResource",
      "sso:ListTagsForResource",
    ]
    resources = ["*"]
  }

  statement {
    sid    = "IdentityStoreManageGroup"
    effect = "Allow"
    actions = [
      "identitystore:CreateGroup",
      "identitystore:DeleteGroup",
      "identitystore:DescribeGroup",
      "identitystore:ListGroups",
      "identitystore:UpdateGroup",
    ]
    resources = ["*"]
  }
}

resource "aws_iam_user_policy" "sso_admin_deploy" {
  name   = "${var.project_name}-sso-admin"
  user   = "terraform-deployer"
  policy = data.aws_iam_policy_document.sso_admin_deploy.json
}
