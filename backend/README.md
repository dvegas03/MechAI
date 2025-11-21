## To start the backend
1. Install Docker on your PC
2. Start Docker Engine
3. Run ```docker compose up --build -d```

## To stop the backend:
```docker compose down --remove-orphans```

## Api access:
HOST: ```localhost``` or ```127.0.0.1``` \
PORT: ```80```

## DB acess:
HOST: ```localhost``` or ```127.0.0.1``` \
PORT: ```3306```

## When you pip install:
Run this command after:\
```docker compose exec api pip freeze | Out-File -Encoding UTF8 requirements.txt```\
to save the exact versions in the requirements.txt