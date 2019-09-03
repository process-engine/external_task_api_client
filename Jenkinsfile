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
    stage('Prepare') {
      steps {
        dir('typescript') {
          nodejs(configId: env.NPM_RC_FILE, nodeJSInstallationName: env.NODE_JS_VERSION) {
            sh('node --version')
            sh('npm install --ignore-scripts')

            // does prepare the version, but not commit it
            sh('node ./node_modules/.bin/ci_tools prepare-version --allow-dirty-workdir')

            // stash the package.json because it contains the prepared version number
          }
        }
      }
    }
    stage('Lint') {
      steps {
        dir('typescript') {
          sh('node --version')
        sh('npm run lint')
        }
      }
    }
    stage('Build') {
      steps {
        dir('typescript') {
          sh('node --version')
          sh('npm run build')
        }

      }
    }
    stage('Test') {
      steps {
        dir('typescript') {
          sh('node --version')
          sh('npm run test')
        }
      }
    }
    stage('Commit & tag version') {
      when {
        anyOf {
          branch "master"
          branch "beta"
          branch "develop"
        }
      }
      steps {
        withCredentials([
          usernamePassword(credentialsId: 'process-engine-ci_github-token', passwordVariable: 'GH_TOKEN', usernameVariable: 'GH_USER')
        ]) {
          // does not change the version, but commit and tag it
          sh('node ./node_modules/.bin/ci_tools commit-and-tag-version --only-on-primary-branches')

          sh('node ./node_modules/.bin/ci_tools update-github-release --only-on-primary-branches --use-title-and-text-from-git-tag');
        }
      }
    }
    stage('Publish') {
      steps {
        dir('typescript') {

          nodejs(configId: env.NPM_RC_FILE, nodeJSInstallationName: env.NODE_JS_VERSION) {
            sh('node ./node_modules/.bin/ci_tools publish-npm-package --create-tag-from-branch-name')
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
