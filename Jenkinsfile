#!/usr/bin/env groovy

def cleanup_workspace() {
  cleanWs()
  dir("${env.WORKSPACE}@tmp") {
    deleteDir()
  }
  dir("${env.WORKSPACE}@script") {
    deleteDir()
  }
  dir("${env.WORKSPACE}@script@tmp") {
    deleteDir()
  }
}

pipeline {
  agent any
  tools {
    nodejs "node-lts"
  }
  environment {
    NPM_RC_FILE = 'process-engine-ci-token'
    NODE_JS_VERSION = 'node-lts'
  }

  stages {
    stage('Install dependencies') {
      steps {
        dir('typescript') {
          nodejs(configId: env.NPM_RC_FILE, nodeJSInstallationName: env.NODE_JS_VERSION) {
            sh('node --version')
            sh('npm install --ignore-scripts')
          }
        }
      }
    }
    stage('Build Sources') {
      steps {
        dir('typescript') {
          sh('node --version')
          sh('npm run build')
        }
      }
    }
    stage('Test') {
      parallel {
        stage('Lint sources') {
          steps {
            dir('typescript') {
              sh('node --version')
              sh('npm run lint')
            }
          }
        }
        stage('Execute tests') {
          steps {
            dir('typescript') {
              sh('node --version')
              sh('npm run test')
            }
          }
        }
      }
    }
    stage('Set package version') {
      steps {
        dir('typescript') {
          sh('node --version')
          sh('node ./node_modules/.bin/ci_tools prepare-version --allow-dirty-workdir');

          withCredentials([
            usernamePassword(credentialsId: 'process-engine-ci_github-token', passwordVariable: 'GH_TOKEN', usernameVariable: 'GH_USER')
          ]) {
            sh('node ./node_modules/.bin/ci_tools commit-and-tag-version --only-on-primary-branches')
          }
        }
      }
    }
    stage('Publish') {
      parallel {
        stage('npm') {
          steps {
            dir('typescript') {
              nodejs(configId: env.NPM_RC_FILE, nodeJSInstallationName: env.NODE_JS_VERSION) {
                sh('node ./node_modules/.bin/ci_tools publish-npm-package --create-tag-from-branch-name')
              }
            }
          }
        }
        stage('GitHub') {
          when {
            anyOf {
              branch "beta"
              branch "develop"
              branch "master"
            }
          }
          steps {
            dir('typescript') {
              withCredentials([
                usernamePassword(credentialsId: 'process-engine-ci_github-token', passwordVariable: 'GH_TOKEN', usernameVariable: 'GH_USER')
              ]) {
                sh('node ./node_modules/.bin/ci_tools update-github-release --only-on-primary-branches --use-title-and-text-from-git-tag');
              }
            }
          }
        }
      }
    }
    stage('Cleanup') {
      steps {
        script {
          // this stage just exists, so the cleanup-work that happens in the post-script
          // will show up in its own stage in Blue Ocean
          sh(script: ':', returnStdout: true);
        }
      }
    }
  }
  post {
    always {
      script {
        cleanup_workspace();
      }
    }
  }
}
