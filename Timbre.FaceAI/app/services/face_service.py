from pathlib import Path

import cv2
import numpy as np


class FaceService:

    _detector = None
    _recognizer = None

    _yunet_model_path = (
        Path(__file__).resolve().parents[2]
        / "models"
        / "face_detection_yunet_2026may.onnx"
    )

    _sface_model_path = (
        Path(__file__).resolve().parents[2]
        / "models"
        / "face_recognition_sface_2021dec.onnx"
    )

    @staticmethod
    def decodificar_imagen(contenido: bytes):
        if not contenido:
            raise ValueError(
                "La imagen está vacía."
            )

        buffer = np.frombuffer(
            contenido,
            dtype=np.uint8
        )

        imagen = cv2.imdecode(
            buffer,
            cv2.IMREAD_COLOR
        )

        if imagen is None:
            raise ValueError(
                "No fue posible interpretar la imagen."
            )

        return imagen

    @staticmethod
    def obtener_dimensiones(imagen):
        alto, ancho = imagen.shape[:2]

        return ancho, alto

    @classmethod
    def obtener_detector(cls):

        if cls._detector is not None:
            return cls._detector

        if not cls._yunet_model_path.exists():
            raise RuntimeError(
                "No se encontró el modelo YuNet en: "
                f"{cls._yunet_model_path}"
            )

        cls._detector = cv2.FaceDetectorYN.create(
            model=str(cls._yunet_model_path),
            config="",
            input_size=(320, 320),
            score_threshold=0.8,
            nms_threshold=0.3,
            top_k=5000
        )

        return cls._detector

    @classmethod
    def obtener_reconocedor(cls):

        if cls._recognizer is not None:
            return cls._recognizer

        if not cls._sface_model_path.exists():
            raise RuntimeError(
                "No se encontró el modelo SFace en: "
                f"{cls._sface_model_path}"
            )

        cls._recognizer = (
            cv2.FaceRecognizerSF.create(
                str(cls._sface_model_path),
                ""
            )
        )

        return cls._recognizer

    @classmethod
    def detectar_rostros(cls, imagen):

        detector = cls.obtener_detector()

        alto, ancho = imagen.shape[:2]

        detector.setInputSize(
            (ancho, alto)
        )

        resultado = detector.detect(
            imagen
        )

        rostros = resultado[1]

        if rostros is None:
            return []

        encontrados = []

        for rostro in rostros:

            x = int(rostro[0])
            y = int(rostro[1])
            ancho_rostro = int(rostro[2])
            alto_rostro = int(rostro[3])

            confianza = float(
                rostro[14]
            )

            encontrados.append(
                {
                    "x": x,
                    "y": y,
                    "ancho": ancho_rostro,
                    "alto": alto_rostro,
                    "confianza": confianza,
                    "datos_yunet": rostro
                }
            )

        return encontrados

    @classmethod
    def generar_embedding(
        cls,
        imagen,
        datos_yunet
    ):

        recognizer = cls.obtener_reconocedor()

        rostro_alineado = (
            recognizer.alignCrop(
                imagen,
                datos_yunet
            )
        )

        embedding = recognizer.feature(
            rostro_alineado
        )

        embedding = embedding.flatten()

        norma = np.linalg.norm(
            embedding
        )

        if norma == 0:
            raise RuntimeError(
                "No fue posible normalizar "
                "el embedding facial."
            )

        embedding_normalizado = (
            embedding / norma
        )

        return embedding_normalizado

    @staticmethod
    def comparar_embeddings(
        embedding_1,
        embedding_2
    ):
        if embedding_1 is None or embedding_2 is None:
            raise ValueError(
                "Los embeddings no pueden ser nulos."
            )

        if len(embedding_1) != len(embedding_2):
            raise ValueError(
                "Los embeddings tienen dimensiones diferentes."
            )

        embedding_1 = np.asarray(
            embedding_1,
            dtype=np.float32
        )

        embedding_2 = np.asarray(
            embedding_2,
            dtype=np.float32
        )

        norma_1 = np.linalg.norm(
            embedding_1
        )

        norma_2 = np.linalg.norm(
            embedding_2
        )

        if norma_1 == 0 or norma_2 == 0:
            raise ValueError(
                "Uno de los embeddings no es válido."
            )

        embedding_1 = embedding_1 / norma_1
        embedding_2 = embedding_2 / norma_2

        similitud = float(
            np.dot(
                embedding_1,
                embedding_2
            )
        )

        return similitud

    @staticmethod
    def promediar_embeddings(embeddings):

        if embeddings is None or len(embeddings) == 0:
            raise ValueError(
                "Debe existir al menos un embedding."
            )

        matriz = np.asarray(
            embeddings,
            dtype=np.float32
        )

        if matriz.ndim != 2:
            raise ValueError(
                "Los embeddings no tienen "
                "una estructura válida."
            )

        dimensiones = {
            len(embedding)
            for embedding in embeddings
        }

        if len(dimensiones) != 1:
            raise ValueError(
                "Los embeddings tienen "
                "dimensiones diferentes."
            )

        promedio = np.mean(
            matriz,
            axis=0
        )

        norma = np.linalg.norm(
            promedio
        )

        if norma == 0:
            raise ValueError(
                "No fue posible normalizar "
                "el embedding promedio."
            )

        promedio_normalizado = (
            promedio / norma
        )

        return promedio_normalizado