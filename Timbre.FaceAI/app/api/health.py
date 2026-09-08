from fastapi import APIRouter
from fastapi.responses import JSONResponse

from app.services.face_service import FaceService


router = APIRouter(
    prefix="/health",
    tags=["Health"]
)


# =========================================================
# HEALTH GENERAL
#
# Se conserva por compatibilidad.
#
# Realiza una validación real de los componentes
# necesarios para reconocimiento facial.
# =========================================================

@router.get("")
async def health():

    estado_ia = (
        FaceService
        .verificar_disponibilidad()
    )


    if not estado_ia["disponible"]:

        return JSONResponse(
            status_code=503,

            content={
                "servicio":
                    "Timbre.FaceAI",

                "estado":
                    "no_disponible",

                "ia":
                    "no_lista",

                "componentes": {
                    "yunet":
                        estado_ia["yunet"],

                    "sface":
                        estado_ia["sface"]
                }
            }
        )


    return {
        "servicio":
            "Timbre.FaceAI",

        "estado":
            "ok",

        "ia":
            "lista",

        "componentes": {
            "yunet":
                True,

            "sface":
                True
        }
    }


# =========================================================
# LIVENESS
#
# Comprueba únicamente que FastAPI está vivo.
#
# NO carga ni valida los modelos.
#
# Azure:
# liveness probe
# =========================================================

@router.get("/live")
async def health_live():

    return {
        "servicio":
            "Timbre.FaceAI",

        "estado":
            "ok"
    }


# =========================================================
# READINESS
#
# Comprueba que FaceAI está realmente preparado para
# recibir solicitudes.
#
# Verifica:
#
# - YuNet
# - SFace
# - inicialización OpenCV
#
# Azure:
# readiness probe
#
# Si la IA no está preparada:
# HTTP 503
# =========================================================

@router.get("/ready")
async def health_ready():

    estado_ia = (
        FaceService
        .verificar_disponibilidad()
    )


    if not estado_ia["disponible"]:

        return JSONResponse(
            status_code=503,

            content={
                "servicio":
                    "Timbre.FaceAI",

                "estado":
                    "no_disponible",

                "ia":
                    "no_lista",

                "componentes": {
                    "yunet":
                        estado_ia["yunet"],

                    "sface":
                        estado_ia["sface"]
                }
            }
        )


    return {
        "servicio":
            "Timbre.FaceAI",

        "estado":
            "ok",

        "ia":
            "lista",

        "componentes": {
            "yunet":
                True,

            "sface":
                True
        }
    }