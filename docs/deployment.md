# Deployment Guide

CellScope is designed for zero-configuration, production-ready cloud deployment on **Render's Free Tier**, Docker, and Docker Compose.

---

## 1. Deploy on Render (Free Plan)

Render hosts CellScope directly from your GitHub repository: `https://github.com/shatru123/CellScope.git`.

### Method A: One-Click Blueprint (Recommended)
1. Go to [dashboard.render.com](https://dashboard.render.com).
2. Click **New +** → **Blueprint**.
3. Connect your GitHub repository (`shatru123/CellScope`).
4. Render automatically reads [`render.yaml`](file:///Users/shatrughnaambhore/Shatru/Learning/Projects/CellScope/render.yaml), provisioning:
   - **Web Service**: `cellscope` (Docker runtime on Free Plan).
   - **PostgreSQL Database**: `cellscope-db` (Free Tier).
5. Click **Apply**. Render will build the Docker container and deploy your live app at `https://cellscope.onrender.com`.

### Method B: Manual Web Service (Single Free Service)
1. Go to [dashboard.render.com](https://dashboard.render.com) → **New +** → **Web Service**.
2. Select your repository `shatru123/CellScope`.
3. Configure the service settings:
   - **Name**: `cellscope`
   - **Language / Runtime**: `Docker`
   - **Dockerfile Path**: `./Dockerfile`
   - **Instance Type**: `Free` ($0/month)
   - **Health Check Path**: `/health`
4. Add Environment Variables:
   - `ASPNETCORE_ENVIRONMENT`: `Production`
   - *(Optional)* `DATABASE_URL`: Your PostgreSQL connection string, or leave empty to use built-in SQLite database automatically.
5. Click **Deploy Web Service**.

---

## 2. Docker & Local Deployment

### Standalone Docker Container
```bash
docker build -t cellscope:latest .
docker run -d -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production cellscope:latest
```

### Docker Compose (App + PostgreSQL)
```bash
docker compose up -d --build
```

---

## 3. Post-Deployment Verification
- **Web App**: `https://<your-subdomain>.onrender.com/`
- **GIS Map**: `https://<your-subdomain>.onrender.com/map`
- **3GPP Security Hub**: `https://<your-subdomain>.onrender.com/security`
- **Radio Analytics**: `https://<your-subdomain>.onrender.com/radio`
- **Health Check**: `https://<your-subdomain>.onrender.com/health`

