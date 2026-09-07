from pydantic import BaseModel


class FaceBoundingBox(BaseModel):
    x: int
    y: int
    ancho: int
    alto: int
    confianza: float


class FaceDetectionResponse(BaseModel):
    rostro_detectado: bool
    cantidad_rostros: int

    ancho_imagen: int
    alto_imagen: int

    rostro: FaceBoundingBox | None = None

    mensaje: str

class FaceEmbeddingResponse(BaseModel):
    rostro_detectado: bool
    cantidad_rostros: int
    dimension_embedding: int
    embedding: list[float]
    confianza: float
    mensaje: str

class FaceComparisonResponse(BaseModel):
    coincide: bool
    similitud: float
    umbral: float

    confianza_rostro_1: float
    confianza_rostro_2: float

    mensaje: str

class FaceEnrollmentResponse(BaseModel):
    enrolamiento_valido: bool
    cantidad_muestras: int
    dimension_embedding: int
    embedding: list[float]
    mensaje: str