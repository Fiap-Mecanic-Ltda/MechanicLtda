# SonarQube Coverage Setup Checklist

## ✅ What Has Been Configured

1. **`sonar-project.properties`** - Created with:
   - Project key matching your SonarQube Cloud organization: `Fiap-Mecanic-Ltda_MechanicLtda`
   - Source and test directory paths
   - Coverage report paths configured for OpenCover format

2. **`.github/workflows/sonar-analysis.yml`** - Updated with:
   - Test execution with XPlat Code Coverage collection
   - Coverage report merging before sending to SonarQube
   - Automatic triggers on push to `main`, `homolog`, `develop` branches
   - PR triggers for automatic analysis

3. **`run-coverage.ps1`** - Local script for:
   - Running tests with coverage locally
   - Merging coverage reports to `./coverage/SonarQubeCoverage.xml`
   - Validating coverage before pushing to GitHub

## 🔧 What You Need to Do

### Step 1: Commit and Push Files
```powershell
git add sonar-project.properties .github/workflows/sonar-analysis.yml run-coverage.ps1
git commit -m "Configure SonarQube code coverage integration"
git push origin homolog
```

### Step 2: Configure GitHub Secrets (REQUIRED)
If not already done:

1. Go to GitHub repository → **Settings** → **Secrets and variables** → **Actions**
2. Add these secrets:
   - `SONAR_TOKEN`: Your SonarQube Cloud token
   - (Optional) `SONAR_HOST_URL`: If using self-hosted SonarQube

### Step 3: Verify SonarQube Configuration
In your SonarQube project:
1. Navigate to **Project Settings** → **General Settings**
2. Ensure "Code Coverage" section shows:
   - Coverage report paths are properly configured
   - Coverage metric is enabled

### Step 4: Run First Analysis
1. Push changes to GitHub
2. GitHub Actions workflow will automatically trigger
3. Check **Actions** tab to see build progress
4. After workflow completes, check SonarQube project dashboard

## 🧪 Testing Locally

Run this command to generate coverage locally:
```powershell
.\run-coverage.ps1
```

This will:
- Build the project
- Run all tests with coverage collection
- Generate `./coverage/SonarQubeCoverage.xml`

## 📊 Expected Results

When properly configured, SonarQube should display:
- **Coverage %**: Overall code coverage percentage
- **Lines to Cover**: Number of lines without coverage
- **Covered Lines**: Number of tested lines
- **Branches Coverage**: Branch coverage percentage
- **Uncovered Lines**: Detailed list of untested code

## ⚠️ Common Issues & Solutions

### Coverage not appearing in SonarQube
1. Check that coverage file is being generated: `./coverage/SonarQubeCoverage.xml`
2. Verify `SONAR_TOKEN` is correctly set in GitHub secrets
3. Check GitHub Actions workflow logs for errors
4. Ensure project key in `sonar-project.properties` matches SonarQube Cloud project

### No test results in GitHub Actions
1. Run locally: `dotnet test --collect:"XPlat Code Coverage"`
2. Verify test projects have `coverlet.collector` NuGet package
3. Check workflow logs for test failures

### Coverage file not merging correctly
1. Run `.\run-coverage.ps1` locally to test
2. Verify test projects are in `tests/` directory
3. Check that `.opencover.xml` files are being generated

## 📝 Files Modified/Created

- ✅ `sonar-project.properties` - NEW
- ✅ `.github/workflows/sonar-analysis.yml` - UPDATED
- ✅ `run-coverage.ps1` - NEW

## 🚀 Next Steps

1. Commit and push all files
2. Add GitHub secrets if needed
3. Watch GitHub Actions run the analysis
4. Check SonarQube dashboard for coverage metrics

**Note**: Coverage metrics may take 1-2 minutes to appear on the SonarQube dashboard after the workflow completes.
