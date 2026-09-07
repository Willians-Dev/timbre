from fastapi import FastAPI

from app.api.health import router as health_router
from app.api.face import router as face_router


app = FastAPI(
    title="Timbre FaceAI",
    description=(
        "Componente de inteligencia artificial "
        "para reconocimiento facial del sistema Timbre."
    ),
    version="1.0.0"
)

app.include_router(health_router)
app.include_router(face_router)


@app.get("/")
async def root():
    return {
        "servicio": "Timbre.FaceAI",
        "estado": "activo"
    }