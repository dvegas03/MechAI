## To start the backend
1. Install Docker on your PC
2. Run ```docker compose up --build -d```

## To stop the backend:
```docker compose down --remove-orphans```

## When you pip install:
Run this command after:
```docker compose exec api pip freeze | Out-File -Encoding UTF8 requirements.txt```
To save the exact versions in the requirements.txt