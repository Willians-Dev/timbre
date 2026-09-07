from fastapi import APIRouter

router = APIRouter(
    prefix="/health",
    tags=["Health"]
)


@router.get("")
async def health():
    return {
        "servicio": "Timbre.FaceAI",
        "estado": "ok",
        "ia": "pendiente"
    }