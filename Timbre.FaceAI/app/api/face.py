from fastapi import (
    APIRouter,
    File,
    HTTPException,
    UploadFile
)

from app.models.face_models import (
    FaceBoundingBox,
    FaceDetectionResponse,
    FaceEmbeddingResponse,
    FaceComparisonResponse,
    FaceEnrollmentResponse
)

from app.services.face_service import (
    FaceService
)

router = APIRouter(
    prefix="/face",
    tags=["Face AI"]
)


@router.post(
    "/detect",
    response_model=FaceDetectionResponse
)
async def detect_face(
    file: UploadFile = File(...)
):

    tipos_permitidos = {
        "image/jpeg",
        "image/jpg",
        "image/png"
    }

    if file.content_type not in tipos_permitidos:
        raise HTTPException(
            status_code=400,
            detail=(
                "Formato de archivo no permitido. "
                "Utilice JPG, JPEG o PNG."
            )
        )

    try:

        contenido = await file.read()

        # =================================================
        # TAMAÑO MÁXIMO
        # =================================================
        limite = 10 * 1024 * 1024

        if len(contenido) > limite:
            raise HTTPException(
                status_code=400,
                detail=(
                    "La imagen supera el tamaño máximo "
                    "permitido de 10 MB."
                )
            )

        # =================================================
        # DECODIFICAR
        # =================================================
        imagen = FaceService.decodificar_imagen(
            contenido
        )

        ancho, alto = (
            FaceService.obtener_dimensiones(
                imagen
            )
        )

        # =================================================
        # DETECTAR
        # =================================================
        rostros = FaceService.detectar_rostros(
            imagen
        )

        cantidad = len(rostros)

        # =================================================
        # SIN ROSTROS
        # =================================================
        if cantidad == 0:
            return FaceDetectionResponse(
                rostro_detectado=False,
                cantidad_rostros=0,
                ancho_imagen=ancho,
                alto_imagen=alto,
                rostro=None,
                mensaje=(
                    "No se detectó ningún rostro "
                    "en la imagen."
                )
            )

        # =================================================
        # MÁS DE UN ROSTRO
        # =================================================
        if cantidad > 1:
            return FaceDetectionResponse(
                rostro_detectado=True,
                cantidad_rostros=cantidad,
                ancho_imagen=ancho,
                alto_imagen=alto,
                rostro=None,
                mensaje=(
                    "Se detectó más de un rostro. "
                    "Debe existir una sola persona "
                    "frente a la cámara."
                )
            )

        # =================================================
        # UN SOLO ROSTRO
        # =================================================
        rostro = rostros[0]

        return FaceDetectionResponse(
            rostro_detectado=True,
            cantidad_rostros=1,
            ancho_imagen=ancho,
            alto_imagen=alto,
            rostro=FaceBoundingBox(
                x=rostro["x"],
                y=rostro["y"],
                ancho=rostro["ancho"],
                alto=rostro["alto"],
                confianza=round(
                    rostro["confianza"],
                    4
                )
            ),
            mensaje=(
                "Rostro detectado correctamente."
            )
        )

    except HTTPException:
        raise

    except ValueError as ex:
        raise HTTPException(
            status_code=400,
            detail=str(ex)
        )

    except RuntimeError as ex:
        raise HTTPException(
            status_code=500,
            detail=str(ex)
        )

    except Exception as ex:
        print(
            f"Error detectando rostro: {ex}"
        )

        raise HTTPException(
            status_code=500,
            detail=(
                "Ocurrió un error durante "
                "la detección facial."
            )
        )

@router.post(
    "/embedding",
    response_model=FaceEmbeddingResponse
)
async def generar_embedding(
    file: UploadFile = File(...)
):

    tipos_permitidos = {
        "image/jpeg",
        "image/jpg",
        "image/png"
    }

    if file.content_type not in tipos_permitidos:
        raise HTTPException(
            status_code=400,
            detail=(
                "Formato de archivo no permitido. "
                "Utilice JPG, JPEG o PNG."
            )
        )

    try:

        contenido = await file.read()

        limite = 10 * 1024 * 1024

        if len(contenido) > limite:
            raise HTTPException(
                status_code=400,
                detail=(
                    "La imagen supera el tamaño "
                    "máximo permitido de 10 MB."
                )
            )

        imagen = (
            FaceService.decodificar_imagen(
                contenido
            )
        )

        rostros = (
            FaceService.detectar_rostros(
                imagen
            )
        )

        cantidad = len(rostros)

        if cantidad == 0:
            raise HTTPException(
                status_code=400,
                detail=(
                    "No se detectó ningún rostro."
                )
            )

        if cantidad > 1:
            raise HTTPException(
                status_code=400,
                detail=(
                    "Se detectó más de un rostro. "
                    "Debe existir una sola persona."
                )
            )

        rostro = rostros[0]

        embedding = (
            FaceService.generar_embedding(
                imagen,
                rostro["datos_yunet"]
            )
        )

        return FaceEmbeddingResponse(
            rostro_detectado=True,
            cantidad_rostros=1,
            dimension_embedding=len(
                embedding
            ),
            embedding=[
                float(valor)
                for valor in embedding
            ],
            confianza=round(
                rostro["confianza"],
                4
            ),
            mensaje=(
                "Embedding facial generado "
                "correctamente."
            )
        )

    except HTTPException:
        raise

    except ValueError as ex:
        raise HTTPException(
            status_code=400,
            detail=str(ex)
        )

    except RuntimeError as ex:
        raise HTTPException(
            status_code=500,
            detail=str(ex)
        )

    except Exception as ex:
        print(
            f"Error generando embedding: {ex}"
        )

        raise HTTPException(
            status_code=500,
            detail=(
                "Ocurrió un error generando "
                "el embedding facial."
            )
        )

@router.post(
    "/compare",
    response_model=FaceComparisonResponse
)
async def comparar_rostros(
    file1: UploadFile = File(...),
    file2: UploadFile = File(...)
):

    tipos_permitidos = {
        "image/jpeg",
        "image/jpg",
        "image/png"
    }

    # =====================================================
    # VALIDAR FORMATOS
    # =====================================================

    if file1.content_type not in tipos_permitidos:
        raise HTTPException(
            status_code=400,
            detail=(
                "El archivo 1 tiene un formato "
                "no permitido."
            )
        )

    if file2.content_type not in tipos_permitidos:
        raise HTTPException(
            status_code=400,
            detail=(
                "El archivo 2 tiene un formato "
                "no permitido."
            )
        )

    try:

        # =================================================
        # LEER ARCHIVOS
        # =================================================

        contenido_1 = await file1.read()
        contenido_2 = await file2.read()

        limite = 10 * 1024 * 1024

        if len(contenido_1) > limite:
            raise HTTPException(
                status_code=400,
                detail=(
                    "La imagen 1 supera el tamaño "
                    "máximo permitido de 10 MB."
                )
            )

        if len(contenido_2) > limite:
            raise HTTPException(
                status_code=400,
                detail=(
                    "La imagen 2 supera el tamaño "
                    "máximo permitido de 10 MB."
                )
            )

        # =================================================
        # DECODIFICAR
        # =================================================

        imagen_1 = FaceService.decodificar_imagen(
            contenido_1
        )

        imagen_2 = FaceService.decodificar_imagen(
            contenido_2
        )

        # =================================================
        # DETECTAR ROSTROS
        # =================================================

        rostros_1 = FaceService.detectar_rostros(
            imagen_1
        )

        rostros_2 = FaceService.detectar_rostros(
            imagen_2
        )

        if len(rostros_1) == 0:
            raise HTTPException(
                status_code=400,
                detail=(
                    "No se detectó un rostro "
                    "en la imagen 1."
                )
            )

        if len(rostros_2) == 0:
            raise HTTPException(
                status_code=400,
                detail=(
                    "No se detectó un rostro "
                    "en la imagen 2."
                )
            )

        if len(rostros_1) > 1:
            raise HTTPException(
                status_code=400,
                detail=(
                    "La imagen 1 contiene más "
                    "de un rostro."
                )
            )

        if len(rostros_2) > 1:
            raise HTTPException(
                status_code=400,
                detail=(
                    "La imagen 2 contiene más "
                    "de un rostro."
                )
            )

        rostro_1 = rostros_1[0]
        rostro_2 = rostros_2[0]

        # =================================================
        # GENERAR EMBEDDINGS
        # =================================================

        embedding_1 = FaceService.generar_embedding(
            imagen_1,
            rostro_1["datos_yunet"]
        )

        embedding_2 = FaceService.generar_embedding(
            imagen_2,
            rostro_2["datos_yunet"]
        )

        # =================================================
        # COMPARAR
        # =================================================

        similitud = FaceService.comparar_embeddings(
            embedding_1,
            embedding_2
        )

        # =================================================
        # UMBRAL INICIAL
        #
        # Lo calibramos después con pruebas reales.
        # =================================================

        umbral = 0.45

        coincide = similitud >= umbral

        return FaceComparisonResponse(
            coincide=coincide,
            similitud=round(
                similitud,
                4
            ),
            umbral=umbral,
            confianza_rostro_1=round(
                rostro_1["confianza"],
                4
            ),
            confianza_rostro_2=round(
                rostro_2["confianza"],
                4
            ),
            mensaje=(
                "Los rostros corresponden "
                "a la misma persona."
                if coincide
                else
                "Los rostros no corresponden "
                "a la misma persona."
            )
        )

    except HTTPException:
        raise

    except ValueError as ex:
        raise HTTPException(
            status_code=400,
            detail=str(ex)
        )

    except RuntimeError as ex:
        raise HTTPException(
            status_code=500,
            detail=str(ex)
        )

    except Exception as ex:
        print(
            f"Error comparando rostros: {ex}"
        )

        raise HTTPException(
            status_code=500,
            detail=(
                "Ocurrió un error durante "
                "la comparación facial."
            )
        )

@router.post(
    "/enroll",
    response_model=FaceEnrollmentResponse
)
async def enrolar_rostro(
    file1: UploadFile = File(...),
    file2: UploadFile = File(...),
    file3: UploadFile = File(...)
):

    archivos = [
        file1,
        file2,
        file3
    ]

    tipos_permitidos = {
        "image/jpeg",
        "image/jpg",
        "image/png"
    }

    try:

        embeddings = []

        for indice, archivo in enumerate(
            archivos,
            start=1
        ):

            if archivo.content_type not in tipos_permitidos:
                raise HTTPException(
                    status_code=400,
                    detail=(
                        f"La imagen {indice} tiene "
                        "un formato no permitido."
                    )
                )

            contenido = await archivo.read()

            limite = 10 * 1024 * 1024

            if len(contenido) > limite:
                raise HTTPException(
                    status_code=400,
                    detail=(
                        f"La imagen {indice} supera "
                        "los 10 MB."
                    )
                )

            imagen = (
                FaceService.decodificar_imagen(
                    contenido
                )
            )

            rostros = (
                FaceService.detectar_rostros(
                    imagen
                )
            )

            if len(rostros) == 0:
                raise HTTPException(
                    status_code=400,
                    detail=(
                        f"No se detectó ningún rostro "
                        f"en la imagen {indice}."
                    )
                )

            if len(rostros) > 1:
                raise HTTPException(
                    status_code=400,
                    detail=(
                        f"La imagen {indice} contiene "
                        "más de un rostro."
                    )
                )

            rostro = rostros[0]

            embedding = (
                FaceService.generar_embedding(
                    imagen,
                    rostro["datos_yunet"]
                )
            )

            embeddings.append(
                embedding
            )

        # =================================================
        # VERIFICAR QUE LAS 3 FOTOS SEAN DE LA MISMA PERSONA
        # =================================================

        similitud_12 = (
            FaceService.comparar_embeddings(
                embeddings[0],
                embeddings[1]
            )
        )

        similitud_13 = (
            FaceService.comparar_embeddings(
                embeddings[0],
                embeddings[2]
            )
        )

        similitud_23 = (
            FaceService.comparar_embeddings(
                embeddings[1],
                embeddings[2]
            )
        )

        umbral = 0.45

        if (
            similitud_12 < umbral
            or similitud_13 < umbral
            or similitud_23 < umbral
        ):
            raise HTTPException(
                status_code=400,
                detail=(
                    "Las fotografías no presentan "
                    "suficiente similitud entre sí. "
                    "Repita el enrolamiento."
                )
            )

        # =================================================
        # GENERAR EMBEDDING MAESTRO
        # =================================================

        embedding_maestro = (
            FaceService.promediar_embeddings(
                embeddings
            )
        )

        return FaceEnrollmentResponse(
            enrolamiento_valido=True,
            cantidad_muestras=3,
            dimension_embedding=len(
                embedding_maestro
            ),
            embedding=[
                float(valor)
                for valor in embedding_maestro
            ],
            mensaje=(
                "Enrolamiento facial generado "
                "correctamente."
            )
        )

    except HTTPException:
        raise

    except ValueError as ex:
        raise HTTPException(
            status_code=400,
            detail=str(ex)
        )

    except RuntimeError as ex:
        raise HTTPException(
            status_code=500,
            detail=str(ex)
        )

    except Exception as ex:

        print(
            f"Error en enrolamiento facial: {ex}"
        )

        raise HTTPException(
            status_code=500,
            detail=(
                "Ocurrió un error durante "
                "el enrolamiento facial."
            )
        )