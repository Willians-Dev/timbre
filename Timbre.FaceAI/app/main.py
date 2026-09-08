import os

from fastapi import FastAPI

from app.api.health import router as health_router
from app.api.face import router as face_router


# =========================================================
# ENTORNO
#
# Valores esperados:
# Development
# Production
#
# En local, si la variable no existe, se asume Development.
# =========================================================

app_environment = os.getenv(
    "APP_ENVIRONMENT",
    "Development"
).strip()


es_produccion = (
    app_environment.lower()
    == "production"
)


# =========================================================
# FASTAPI
#
# En Development:
# /docs
# /redoc
# /openapi.json
#
# En Production:
# se deshabilitan para reducir superficie expuesta.
# =========================================================

app = FastAPI(
    title="Timbre FaceAI",

    description=(
        "Componente de inteligencia artificial "
        "para reconocimiento facial del sistema Timbre."
    ),

    version="1.0.0",

    docs_url=(
        None
        if es_produccion
        else "/docs"
    ),

    redoc_url=(
        None
        if es_produccion
        else "/redoc"
    ),

    openapi_url=(
        None
        if es_produccion
        else "/openapi.json"
    )
)


# =========================================================
# ROUTERS
# =========================================================

app.include_router(
    health_router
)

app.include_router(
    face_router
)


# =========================================================
# ROOT
#
# Se mantiene como respuesta mínima.
#
# No exponemos:
# - versión de Python
# - versión de OpenCV
# - versión de ONNX Runtime
# - rutas locales
# - ubicación de modelos
# =========================================================

@app.get("/")
async def root():
    return {
        "servicio": "Timbre.FaceAI",
        "estado": "activo"
    }