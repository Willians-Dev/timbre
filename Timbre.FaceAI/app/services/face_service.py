from pathlib import Path

import cv2
import numpy as np


class FaceService:

    # =====================================================
    # INSTANCIAS DE LOS MODELOS
    #
    # Se cargan una sola vez y se reutilizan.
    # =====================================================

    _detector = None
    _recognizer = None


    # =====================================================
    # RUTAS DE MODELOS
    # =====================================================

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


    # =====================================================
    # DECODIFICAR IMAGEN
    # =====================================================

    @staticmethod
    def decodificar_imagen(
        contenido: bytes
    ):
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


    # =====================================================
    # DIMENSIONES
    # =====================================================

    @staticmethod
    def obtener_dimensiones(
        imagen
    ):
        alto, ancho = imagen.shape[:2]

        return ancho, alto


    # =====================================================
    # OBTENER DETECTOR YUNET
    # =====================================================

    @classmethod
    def obtener_detector(
        cls
    ):
        if cls._detector is not None:
            return cls._detector

        if not cls._yunet_model_path.exists():
            raise RuntimeError(
                "No se encontró el modelo YuNet."
            )

        if not cls._yunet_model_path.is_file():
            raise RuntimeError(
                "El modelo YuNet no corresponde a un archivo válido."
            )

        try:
            cls._detector = cv2.FaceDetectorYN.create(
                model=str(
                    cls._yunet_model_path
                ),
                config="",
                input_size=(
                    320,
                    320
                ),
                score_threshold=0.8,
                nms_threshold=0.3,
                top_k=5000
            )

        except Exception as exc:
            cls._detector = None

            raise RuntimeError(
                "No fue posible inicializar el modelo YuNet."
            ) from exc

        if cls._detector is None:
            raise RuntimeError(
                "No fue posible inicializar el detector facial."
            )

        return cls._detector


    # =====================================================
    # OBTENER RECONOCEDOR SFACE
    # =====================================================

    @classmethod
    def obtener_reconocedor(
        cls
    ):
        if cls._recognizer is not None:
            return cls._recognizer

        if not cls._sface_model_path.exists():
            raise RuntimeError(
                "No se encontró el modelo SFace."
            )

        if not cls._sface_model_path.is_file():
            raise RuntimeError(
                "El modelo SFace no corresponde a un archivo válido."
            )

        try:
            cls._recognizer = cv2.FaceRecognizerSF.create(
                str(
                    cls._sface_model_path
                ),
                ""
            )

        except Exception as exc:
            cls._recognizer = None

            raise RuntimeError(
                "No fue posible inicializar el modelo SFace."
            ) from exc

        if cls._recognizer is None:
            raise RuntimeError(
                "No fue posible inicializar el reconocedor facial."
            )

        return cls._recognizer


    # =====================================================
    # VERIFICAR DISPONIBILIDAD DE IA
    #
    # Utilizado por /health/ready.
    #
    # Comprueba independientemente:
    # - archivo YuNet
    # - inicialización YuNet
    # - archivo SFace
    # - inicialización SFace
    # =====================================================

    @classmethod
    def verificar_disponibilidad(
        cls
    ):
        estado = {
            "disponible": False,
            "yunet": False,
            "sface": False
        }

        # =================================================
        # YUNET
        # =================================================

        try:
            if (
                cls._yunet_model_path.exists()
                and cls._yunet_model_path.is_file()
            ):
                detector = cls.obtener_detector()

                estado["yunet"] = (
                    detector is not None
                )

        except Exception:
            estado["yunet"] = False

        # =================================================
        # SFACE
        # =================================================

        try:
            if (
                cls._sface_model_path.exists()
                and cls._sface_model_path.is_file()
            ):
                recognizer = cls.obtener_reconocedor()

                estado["sface"] = (
                    recognizer is not None
                )

        except Exception:
            estado["sface"] = False

        # =================================================
        # DISPONIBILIDAD GLOBAL
        # =================================================

        estado["disponible"] = (
            estado["yunet"]
            and estado["sface"]
        )

        return estado


    # =====================================================
    # DETECTAR ROSTROS
    # =====================================================

    @classmethod
    def detectar_rostros(
        cls,
        imagen
    ):
        detector = cls.obtener_detector()

        alto, ancho = imagen.shape[:2]

        detector.setInputSize(
            (
                ancho,
                alto
            )
        )

        resultado = detector.detect(
            imagen
        )

        rostros = resultado[1]

        if rostros is None:
            return []

        encontrados = []

        for rostro in rostros:

            x = int(
                rostro[0]
            )

            y = int(
                rostro[1]
            )

            ancho_rostro = int(
                rostro[2]
            )

            alto_rostro = int(
                rostro[3]
            )

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


    # =====================================================
    # GENERAR EMBEDDING
    # =====================================================

    @classmethod
    def generar_embedding(
        cls,
        imagen,
        datos_yunet
    ):
        recognizer = (
            cls.obtener_reconocedor()
        )

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


    # =====================================================
    # COMPARAR EMBEDDINGS
    # =====================================================

    @staticmethod
    def comparar_embeddings(
        embedding_1,
        embedding_2
    ):
        if (
            embedding_1 is None
            or embedding_2 is None
        ):
            raise ValueError(
                "Los embeddings no pueden ser nulos."
            )

        if (
            len(embedding_1)
            != len(embedding_2)
        ):
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

        if (
            norma_1 == 0
            or norma_2 == 0
        ):
            raise ValueError(
                "Uno de los embeddings no es válido."
            )

        embedding_1 = (
            embedding_1 / norma_1
        )

        embedding_2 = (
            embedding_2 / norma_2
        )

        similitud = float(
            np.dot(
                embedding_1,
                embedding_2
            )
        )

        return similitud


    # =====================================================
    # PROMEDIAR EMBEDDINGS
    # =====================================================

    @staticmethod
    def promediar_embeddings(
        embeddings
    ):
        if (
            embeddings is None
            or len(embeddings) == 0
        ):
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
            for embedding
            in embeddings
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