# ExternalTaskApiClient written in Python

This is an ExternalTaskApiClient written in Python.

### Requirements

- pipenv >= `2018.10.13`
- Python 3.7.x

### Setup/Installation

Install all Dependencies

```bash
pipenv install
```

## Install from pypi.org

Install package with pip:

```bash
pip install external_task_api_client
```

Install package with pipenv:

```bash
pipenv install external_task_api_client
```

## Test the Setup

Please use the integration test, we provided:

```bash
pipenv run python -m unittest test_external_task_client.py
```

Additionally, this needs a running instance of ExternalTaskTest BPMN-Process.

You have to terminate the process manually.
