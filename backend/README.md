# Using uv for Dependency Management

## Install dependencies

```sh
cd backend
uv pip install -r uv.lock
```

## Update dependencies

If you need to add or update dependencies, modify `requirements.txt` and then run:

```sh
uv pip compile requirements.txt --output-file=uv.lock
uv pip install -r uv.lock
```

## Running the backend

You can run the backend as before (e.g., with Docker or your preferred method). The Dockerfile now uses uv for dependency management. 