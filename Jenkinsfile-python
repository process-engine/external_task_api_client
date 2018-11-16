#!/usr/bin/env groovy

@Library('jenkins_shared-libraries@v1.1.0') _

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
  agent {
    docker {
      image 'kennethreitz/pipenv'
    }
  }
  environment {
    HOME = "$WORKSPACE"
  }

  stages {
    stage('prepare') {
      steps {
        dir('python') {
          sh('python3 --version')

          sh('pipenv install')
        }
      }
    }
    stage('package') {
      steps {
        dir('python') {
          sh('python3 --version')

          sh('python3 setup.py sdist bdist_wheel')
        }
      }
    }
    stage('publish') {
      when {
        branch 'master'
      }
      steps {
        dir('python') {
          sh('pipenv install twine')
          configFileProvider([configFile(fileId: 'pypi_processengine_settings', targetLocation: '.pypirc')]) {
            sh('pipenv run twine upload --config-file .pypirc dist/* && true')
          }
        }
      }
    }
    stage('cleanup') {
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
