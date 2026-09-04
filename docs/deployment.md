# Deployment Guide

CellScope supports Docker, Docker Compose, and zero-cost cloud deployment on Render.

---

## 1. Docker Compose (API + PostgreSQL)

```bash
docker-compose up -d --build
```

Access the dashboard at `http://localhost:5000`.

---

## 2. Render Cloud Deployment

CellScope includes `render.yaml` for automated deployment on Render's free tier:

1. Link your GitHub repository to Render.
2. Create a new **Blueprint** instance selecting `render.yaml`.
3. Render automatically provisions the Web service and PostgreSQL database.
