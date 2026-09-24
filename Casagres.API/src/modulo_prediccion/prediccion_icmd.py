#!/usr/bin/env python3
"""
Predicción ICMD: KRR + fallback estacional + ACI secuencial
============================================================

Comportamiento:
1. Las referencias presentes en HIPERPARAMETROS_POR_SERIE se reentrenan con
   TODO el histórico disponible y se pronostica por defecto el siguiente mes.
2. Las referencias no configuradas usan y[t-12]; si el mes no existe, usan 0.
3. Cada referencia mantiene un Adaptive Conformal Inference (ACI) independiente.
4. El estado ACI se persiste en estado_aci.json y solo se actualiza cuando una
   predicción pendiente pasa a disponer de un valor real en el panel.
5. Las predicciones futuras nunca se usan como observaciones para actualizar ACI.

Salidas:
    salidas_prediccion/pronostico.csv
    salidas_prediccion/pronostico_intervalos.csv
    salidas_prediccion/pronostico_historico.csv
    salidas_prediccion/metodo_por_serie.csv
    salidas_prediccion/estado_aci.json
"""

from __future__ import annotations

import argparse
import json
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np
import pandas as pd
from sklearn.kernel_ridge import KernelRidge
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

# =========================================================================== #
# CONFIGURACIÓN
# =========================================================================== #
MAX_LAG = 12
FACTOR_TECHO = 1.5

# ACI: cobertura nominal de 90 % por defecto.
ACI_TARGET_MISCOVERAGE = 0.10
ACI_STEP_SIZE = 0.05
ACI_MIN_ALPHA = 0.01
ACI_MAX_ALPHA = 0.50
ACI_CALIBRATION_MONTHS = 12
ACI_INTERVAL_SCALE = 1.0
ACI_MINIMUM_WIDTH = 0.0
ACI_CLIP_LOWER_ZERO = True
ESTADO_ACI_VERSION = 1


@dataclass(frozen=True)
class Representacion:
    """Define cómo se convierte una serie en una matriz supervisada para h=1."""

    nombre: str
    lags: tuple[int, ...]
    ventanas: tuple[int, ...]
    estacional: bool
    log: bool
    diff: bool


REPRESENTACIONES: dict[str, Representacion] = {
    r.nombre: r
    for r in (
        Representacion("corto_nivel", (1, 2, 3), (3,), False, False, False),
        Representacion("corto_log_nivel", (1, 2, 3), (3,), False, True, False),
        Representacion("corto_diff", (1, 2, 3), (3,), False, False, True),
        Representacion("estacional_nivel", (1, 2, 3, 12), (3, 12), True, False, False),
        Representacion(
            "estacional_log_nivel", (1, 2, 3, 12), (3, 12), True, True, False
        ),
        Representacion("estacional_diff", (1, 2, 3, 12), (3, 12), True, False, True),
        Representacion(
            "extendido_nivel", (1, 2, 3, 4, 5, 6, 12), (3, 6, 12), True, False, False
        ),
        Representacion(
            "extendido_log_nivel", (1, 2, 3, 4, 5, 6, 12), (3, 6, 12), True, True, False
        ),
        Representacion(
            "extendido_diff", (1, 2, 3, 4, 5, 6, 12), (3, 6, 12), True, False, True
        ),
    )
}


# Hiperparámetros optimizados del script original.
HIPERPARAMETROS_POR_SERIE: dict[str, dict] = {
    "MC201230RHRP": {
        "representacion": "estacional_log_nivel",
        "kernel": "poly",
        "alpha": 300.0,
        "gamma": 1.0,
        "degree": 2,
        "coef0": 0.0,
    },
    "TT082380QHRP": {
        "representacion": "estacional_diff",
        "kernel": "poly",
        "alpha": 3.0,
        "gamma": 0.1,
        "degree": 3,
        "coef0": 0.0,
    },
    "MC201030RHRP": {
        "representacion": "extendido_diff",
        "kernel": "poly",
        "alpha": 300.0,
        "gamma": 0.001,
        "degree": 2,
        "coef0": 0.0,
    },
    "MC201230RHRC": {
        "representacion": "corto_log_nivel",
        "kernel": "linear",
        "alpha": 30.0,
    },
    "MC201030RHRC": {
        "representacion": "corto_diff",
        "kernel": "laplacian",
        "alpha": 0.01,
        "gamma": 0.01,
    },
    "TT082380QHRC": {
        "representacion": "corto_log_nivel",
        "kernel": "linear",
        "alpha": 10.0,
    },
    "MC201215RHRP": {
        "representacion": "corto_diff",
        "kernel": "poly",
        "alpha": 300.0,
        "gamma": 1.0,
        "degree": 3,
        "coef0": 0.0,
    },
    "MC101330LVRP": {
        "representacion": "extendido_diff",
        "kernel": "poly",
        "alpha": 300.0,
        "gamma": 0.001,
        "degree": 2,
        "coef0": 0.0,
    },
    "MC201015RHRP": {
        "representacion": "corto_diff",
        "kernel": "poly",
        "alpha": 3.0,
        "gamma": 0.1,
        "degree": 3,
        "coef0": 0.0,
    },
    "MC201230RVRP": {
        "representacion": "estacional_log_nivel",
        "kernel": "rbf",
        "alpha": 0.03,
        "gamma": 0.03,
    },
}


def crear_krr(params: dict) -> Pipeline:
    """KRR con estandarización previa."""
    krr = {k: v for k, v in params.items() if k != "representacion"}
    return Pipeline([("escalador", StandardScaler()), ("krr", KernelRidge(**krr))])


def construir_matriz(serie: pd.Series, rep: Representacion) -> pd.DataFrame:
    """Construye las variables supervisadas de una serie mensual."""
    y = serie.astype(float)
    if rep.log:
        y = np.log1p(y.clip(lower=0))

    tabla = pd.DataFrame({"y": y}, index=serie.index)
    for lag in rep.lags:
        tabla[f"lag_{lag}"] = tabla["y"].shift(lag)
    for ventana in rep.ventanas:
        tabla[f"media_{ventana}"] = tabla["y"].shift(1).rolling(ventana).mean()

    tabla["dif_1"] = tabla["y"].shift(1) - tabla["y"].shift(2)
    tabla["dif_2"] = tabla["y"].shift(2) - tabla["y"].shift(3)

    if rep.estacional:
        mes = tabla.index.month.to_numpy()
        tabla["mes_sin"] = np.sin(2 * np.pi * mes / 12)
        tabla["mes_cos"] = np.cos(2 * np.pi * mes / 12)

    tabla["ancla"] = tabla["y"].shift(1)
    return tabla.iloc[MAX_LAG:].dropna()


def columnas_features(tabla: pd.DataFrame) -> list[str]:
    return [c for c in tabla.columns if c not in ("y", "ancla")]


def a_nivel(
    objetivo: np.ndarray,
    ancla: np.ndarray,
    mu: float,
    sd: float,
    rep: Representacion,
    techo: float,
) -> np.ndarray:
    """Deshace estandarización, diferenciación y log1p; acota a [0, techo]."""
    valores = np.asarray(objetivo, float) * sd + mu
    if rep.diff:
        valores = valores + np.asarray(ancla, float)
    if rep.log:
        valores = np.expm1(np.clip(valores, None, 700))
    return np.clip(valores, 0.0, techo)


# =========================================================================== #
# ADAPTIVE CONFORMAL INFERENCE (ACI)
# =========================================================================== #
def cuantil_conformal_muestra_finita(scores: list[float], alpha: float) -> float:
    """Cuantil conformal conservador ceil((n+1)*(1-alpha))."""
    valores = np.asarray(scores, dtype=float)
    valores = valores[np.isfinite(valores)]
    if valores.size == 0:
        return 0.0
    alpha = float(np.clip(alpha, 1e-6, 1.0))
    rango = int(np.ceil((valores.size + 1) * (1.0 - alpha)))
    rango = min(max(rango, 1), valores.size)
    return float(np.sort(valores)[rango - 1])


@dataclass
class SequentialACI:
    """Adaptive Conformal Inference secuencial por referencia."""

    target_miscoverage: float = ACI_TARGET_MISCOVERAGE
    step_size: float = ACI_STEP_SIZE
    min_alpha: float = ACI_MIN_ALPHA
    max_alpha: float = ACI_MAX_ALPHA
    scores: list[float] = field(default_factory=list)
    alpha_t: float | None = None

    def __post_init__(self) -> None:
        if self.alpha_t is None:
            self.alpha_t = float(self.target_miscoverage)

    def interval(self, point_prediction: float) -> tuple[float, float]:
        q = cuantil_conformal_muestra_finita(self.scores, float(self.alpha_t))
        p = float(point_prediction)
        return p - q, p + q

    def update(
        self,
        y_true: float,
        point_prediction: float,
        *,
        interval: tuple[float, float] | None = None,
    ) -> "SequentialACI":
        if interval is None:
            interval = self.interval(point_prediction)
        lo, hi = map(float, interval)
        real = float(y_true)
        pred = float(point_prediction)
        miss = int(real < lo or real > hi)
        self.alpha_t = float(
            np.clip(
                float(self.alpha_t) + self.step_size * (self.target_miscoverage - miss),
                self.min_alpha,
                self.max_alpha,
            )
        )
        self.scores.append(abs(real - pred))
        return self

    def state_dict(self) -> dict:
        return {
            "target_miscoverage": float(self.target_miscoverage),
            "step_size": float(self.step_size),
            "min_alpha": float(self.min_alpha),
            "max_alpha": float(self.max_alpha),
            "scores": [float(x) for x in self.scores],
            "alpha_t": float(self.alpha_t),
        }

    @classmethod
    def from_state_dict(cls, state: dict) -> "SequentialACI":
        return cls(
            target_miscoverage=float(
                state.get("target_miscoverage", ACI_TARGET_MISCOVERAGE)
            ),
            step_size=float(state.get("step_size", ACI_STEP_SIZE)),
            min_alpha=float(state.get("min_alpha", ACI_MIN_ALPHA)),
            max_alpha=float(state.get("max_alpha", ACI_MAX_ALPHA)),
            scores=[float(x) for x in state.get("scores", [])],
            alpha_t=float(
                state.get(
                    "alpha_t", state.get("target_miscoverage", ACI_TARGET_MISCOVERAGE)
                )
            ),
        )


def finalizar_intervalo(
    prediccion: float,
    intervalo_crudo: tuple[float, float],
) -> tuple[float, float, float]:
    """Aplica escala, ancho mínimo y cota inferior 0 a un intervalo ACI."""
    p = float(prediccion)
    lo0, hi0 = map(float, intervalo_crudo)
    radio = max(p - lo0, hi0 - p, 0.0)
    radio = max(ACI_INTERVAL_SCALE * radio, ACI_MINIMUM_WIDTH)
    lo = p - radio
    hi = p + radio
    if ACI_CLIP_LOWER_ZERO:
        lo = max(0.0, lo)
    return float(lo), float(hi), float(radio)


# =========================================================================== #
# MODELO KRR POR SERIE
# =========================================================================== #
class KRRPorSerie:
    """Entrena KRR únicamente para las referencias configuradas y disponibles."""

    def __init__(self, params_por_codigo: dict[str, dict]) -> None:
        self.params = params_por_codigo
        self.modelos: dict[str, Pipeline] = {}
        self.reps: dict[str, Representacion] = {}
        self.escalas: dict[str, tuple[float, float]] = {}
        self.techos: dict[str, float] = {}

    def _preparar(self, serie: pd.Series, rep: Representacion):
        tabla = construir_matriz(serie, rep)
        cols = columnas_features(tabla)
        objetivo = (tabla["y"] - tabla["ancla"] if rep.diff else tabla["y"]).to_numpy()
        return tabla, cols, tabla[cols].to_numpy(), objetivo

    def entrenar(self, series: pd.DataFrame) -> "KRRPorSerie":
        codigos_krr = [c for c in series.columns if c in self.params]

        for codigo in codigos_krr:
            params = self.params[codigo]
            rep = REPRESENTACIONES[params["representacion"]]
            tabla, _, X, objetivo = self._preparar(series[codigo], rep)

            if len(tabla) == 0:
                print(
                    f"[AVISO] {codigo}: historial insuficiente para KRR; "
                    "se usará fallback estacional."
                )
                continue

            mu = float(objetivo.mean())
            sd = float(objetivo.std() or 1.0)
            techo = FACTOR_TECHO * float(series[codigo].max())

            self.reps[codigo] = rep
            self.escalas[codigo] = (mu, sd)
            self.techos[codigo] = techo
            self.modelos[codigo] = crear_krr(params).fit(X, (objetivo - mu) / sd)

        return self

    def pronosticar_serie(
        self, codigo: str, serie: pd.Series, horizonte: int
    ) -> pd.Series:
        """Pronóstico recursivo KRR para una referencia ya entrenada."""
        if codigo not in self.modelos:
            raise KeyError(f"No existe un KRR entrenado para {codigo}")

        historia = serie.astype(float).copy()
        futuro = pd.date_range(
            historia.index[-1] + pd.DateOffset(months=1),
            periods=horizonte,
            freq="MS",
        )

        rep = self.reps[codigo]
        mu, sd = self.escalas[codigo]
        techo = self.techos[codigo]
        predicciones: list[float] = []

        for fecha in futuro:
            serie_aux = historia.copy()
            serie_aux.loc[fecha] = 0.0  # placeholder; el objetivo no entra como feature
            tabla = construir_matriz(serie_aux, rep)

            if fecha not in tabla.index:
                raise RuntimeError(
                    f"{codigo}: no se pudo construir la fila de características "
                    f"para {fecha:%Y-%m}."
                )

            fila = tabla.loc[[fecha]]
            pred = a_nivel(
                self.modelos[codigo].predict(fila[columnas_features(tabla)].to_numpy()),
                fila["ancla"].to_numpy(),
                mu,
                sd,
                rep,
                techo,
            )[0]

            historia.loc[fecha] = float(pred)
            predicciones.append(float(pred))

        return pd.Series(predicciones, index=futuro, name=codigo)


# =========================================================================== #
# FALLBACK ESTACIONAL
# =========================================================================== #
def pronostico_anio_anterior(
    serie_historica: pd.Series, futuro: pd.DatetimeIndex
) -> pd.Series:
    """
    Devuelve el valor del mismo mes del año anterior.

    La búsqueda se hace SOLO en la serie histórica de entrada. Si la fecha t-12
    no está presente, devuelve 0. Si está presente pero el valor es NaN, también 0.
    """
    historico = serie_historica.astype(float)
    valores: list[float] = []

    for fecha in futuro:
        fecha_anterior = fecha - pd.DateOffset(years=1)
        if fecha_anterior in historico.index:
            valor = historico.loc[fecha_anterior]
            if isinstance(valor, pd.Series):
                valor = valor.iloc[-1]
            valor = 0.0 if pd.isna(valor) else float(valor)
        else:
            valor = 0.0
        valores.append(valor)

    return pd.Series(valores, index=futuro, name=serie_historica.name)


# =========================================================================== #
# CALIBRACIÓN Y PERSISTENCIA ACI
# =========================================================================== #
def _fecha_iso(fecha: pd.Timestamp) -> str:
    return pd.Timestamp(fecha).strftime("%Y-%m-%d")


def _cargar_estado_aci(ruta: Path) -> dict:
    if not ruta.exists():
        return {"version": ESTADO_ACI_VERSION, "series": {}}
    try:
        estado = json.loads(ruta.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        print(f"[ACI][AVISO] No se pudo leer {ruta}: {exc}. Se recalibrará.")
        return {"version": ESTADO_ACI_VERSION, "series": {}}
    if int(estado.get("version", -1)) != ESTADO_ACI_VERSION:
        print("[ACI] Versión de estado distinta; se recalibrará.")
        return {"version": ESTADO_ACI_VERSION, "series": {}}
    estado.setdefault("series", {})
    return estado


def _guardar_estado_aci(ruta: Path, estado: dict) -> None:
    temporal = ruta.with_suffix(ruta.suffix + ".tmp")
    temporal.write_text(
        json.dumps(estado, ensure_ascii=False, indent=2, sort_keys=True),
        encoding="utf-8",
    )
    temporal.replace(ruta)


def _prediccion_un_paso_historica(
    codigo: str,
    historial: pd.Series,
    fecha_objetivo: pd.Timestamp,
) -> tuple[float, str]:
    """Predicción causal h=1 usando exclusivamente información anterior."""
    historial = historial.astype(float).sort_index()
    if codigo in HIPERPARAMETROS_POR_SERIE:
        modelo = KRRPorSerie(HIPERPARAMETROS_POR_SERIE).entrenar(
            historial.to_frame(name=codigo)
        )
        if codigo in modelo.modelos:
            pred = modelo.pronosticar_serie(codigo, historial, 1)
            return float(pred.iloc[0]), "KRR"

    pred = pronostico_anio_anterior(
        historial,
        pd.DatetimeIndex([pd.Timestamp(fecha_objetivo)]),
    )
    return float(pred.iloc[0]), "ANIO_ANTERIOR"


def _calibrar_aci_inicial(
    codigo: str,
    serie: pd.Series,
    *,
    meses: int,
    target: float,
    step_size: float,
) -> tuple[SequentialACI, int]:
    """Backtesting causal rolling one-step en los últimos meses observados."""
    aci = SequentialACI(target_miscoverage=target, step_size=step_size)
    if len(serie) <= 1:
        return aci, 0

    candidatos = list(serie.index[-min(int(meses), len(serie) - 1) :])
    usados = 0
    for fecha in candidatos:
        historial = serie.loc[serie.index < fecha]
        if historial.empty:
            continue
        try:
            pred, _ = _prediccion_un_paso_historica(codigo, historial, fecha)
        except Exception as exc:
            print(f"[ACI][AVISO] {codigo} {fecha:%Y-%m}: calibración omitida ({exc}).")
            continue
        real = float(serie.loc[fecha])
        if not (np.isfinite(real) and np.isfinite(pred)):
            continue
        lo, hi, _ = finalizar_intervalo(pred, aci.interval(pred))
        aci.update(real, pred, interval=(lo, hi))
        usados += 1
    return aci, usados


def _inicializar_estado_serie(
    codigo: str,
    serie: pd.Series,
    metodo: str,
    *,
    meses_calibracion: int,
    target: float,
    step_size: float,
) -> dict:
    aci, n = _calibrar_aci_inicial(
        codigo,
        serie,
        meses=meses_calibracion,
        target=target,
        step_size=step_size,
    )
    return {
        "metodo": metodo,
        "aci": aci.state_dict(),
        "n_calibracion_inicial": int(n),
        "last_aci_observation": _fecha_iso(serie.index[-1]),
        "pending": None,
    }


def _actualizar_estado_con_observaciones(
    codigo: str,
    serie: pd.Series,
    estado_serie: dict,
) -> dict:
    """Actualiza ACI únicamente con observaciones reales nuevas."""
    aci = SequentialACI.from_state_dict(estado_serie.get("aci", {}))
    ultima = pd.Timestamp(estado_serie.get("last_aci_observation", serie.index[0]))
    pendiente = estado_serie.get("pending")

    for fecha in serie.index[serie.index > ultima]:
        real = float(serie.loc[fecha])
        usar_pendiente = isinstance(pendiente, dict) and pd.Timestamp(
            pendiente.get("mes")
        ) == pd.Timestamp(fecha)

        if usar_pendiente:
            pred = float(pendiente["prediccion"])
            intervalo = (float(pendiente["inferior"]), float(pendiente["superior"]))
            fuente = "prediccion_operacional"
            pendiente = None
        else:
            historial = serie.loc[serie.index < fecha]
            try:
                pred, _ = _prediccion_un_paso_historica(codigo, historial, fecha)
            except Exception as exc:
                print(
                    f"[ACI][AVISO] {codigo} {fecha:%Y-%m}: actualización omitida ({exc})."
                )
                ultima = fecha
                continue
            lo, hi, _ = finalizar_intervalo(pred, aci.interval(pred))
            intervalo = (lo, hi)
            fuente = "backfill_causal"

        aci.update(real, pred, interval=intervalo)
        ultima = fecha
        print(
            f"[ACI] {codigo} {fecha:%Y-%m}: actualizado ({fuente}); "
            f"alpha_t={aci.alpha_t:.4f}, scores={len(aci.scores)}."
        )

    estado_serie["aci"] = aci.state_dict()
    estado_serie["last_aci_observation"] = _fecha_iso(ultima)
    estado_serie["pending"] = pendiente
    return estado_serie


def _backtest_causal_reciente(
    codigo: str,
    serie: pd.Series,
    aci: SequentialACI,
    *,
    meses: int,
) -> list[dict]:
    """Backtest causal de solo lectura para poblar el histórico visible.

    A diferencia de `_calibrar_aci_inicial` (que solo corre la primera vez
    que se calibra una serie), esto se recalcula en cada corrida del
    pipeline para los últimos `meses` meses observados, sin mutar `aci`
    (usa el intervalo vigente según los scores ya acumulados). Así se
    autocura si pronostico_historico.csv se pierde, y cubre series cuyo
    estado ACI ya venía calibrado desde antes de existir esta salida.
    """
    registros: list[dict] = []
    if len(serie) <= 1:
        return registros

    candidatos = list(serie.index[-min(int(meses), len(serie) - 1) :])
    for fecha in candidatos:
        historial = serie.loc[serie.index < fecha]
        if historial.empty:
            continue
        try:
            pred, metodo = _prediccion_un_paso_historica(codigo, historial, fecha)
        except Exception as exc:
            print(f"[HISTORICO][AVISO] {codigo} {fecha:%Y-%m}: omitido ({exc}).")
            continue
        if not np.isfinite(pred):
            continue
        lo, hi, _ = finalizar_intervalo(pred, aci.interval(float(pred)))
        registros.append(
            {
                "mes": pd.Timestamp(fecha),
                "codigo_producto": codigo,
                "metodo": metodo,
                "prediccion": float(pred),
                "inferior": lo,
                "superior": hi,
            }
        )
    return registros


def pronosticar_todas_las_series(
    series: pd.DataFrame,
    horizonte: int,
    *,
    ruta_estado: Path,
    meses_calibracion: int = ACI_CALIBRATION_MONTHS,
    aci_target: float = ACI_TARGET_MISCOVERAGE,
    aci_step_size: float = ACI_STEP_SIZE,
    reiniciar_aci: bool = False,
) -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame, pd.DataFrame, dict]:
    """Reentrena KRR, pronostica y produce intervalos ACI persistentes."""
    if horizonte <= 0:
        raise ValueError("El horizonte debe ser mayor que cero.")
    if series.empty or len(series.columns) == 0:
        raise ValueError("El panel de series está vacío.")
    if not 0.0 < aci_target < 1.0:
        raise ValueError("aci_target debe estar entre 0 y 1.")
    if aci_step_size <= 0:
        raise ValueError("aci_step_size debe ser mayor que cero.")

    series = series.sort_index().astype(float)
    futuro = pd.date_range(
        series.index[-1] + pd.DateOffset(months=1),
        periods=horizonte,
        freq="MS",
    )

    # Reentrenamiento KRR completo en cada ejecución.
    modelo = KRRPorSerie(HIPERPARAMETROS_POR_SERIE).entrenar(series)

    estado = (
        {"version": ESTADO_ACI_VERSION, "series": {}}
        if reiniciar_aci
        else _cargar_estado_aci(ruta_estado)
    )

    ultimo_global = estado.get("last_history_month")
    if ultimo_global is not None and pd.Timestamp(ultimo_global) > series.index[-1]:
        print("[ACI] El dataset retrocedió temporalmente; se recalibrará ACI.")
        estado = {"version": ESTADO_ACI_VERSION, "series": {}}

    pronostico = pd.DataFrame(index=futuro)
    metodos = []
    filas_intervalos = []
    filas_historico = []

    for codigo in series.columns:
        metodo = "KRR" if codigo in modelo.modelos else "ANIO_ANTERIOR"
        estado_serie = estado["series"].get(codigo)

        if estado_serie is None or estado_serie.get("metodo") != metodo:
            estado_serie = _inicializar_estado_serie(
                codigo,
                series[codigo],
                metodo,
                meses_calibracion=meses_calibracion,
                target=aci_target,
                step_size=aci_step_size,
            )
            print(
                f"[ACI] {codigo}: calibración inicial con "
                f"{estado_serie['n_calibracion_inicial']} errores; "
                f"alpha_t={estado_serie['aci']['alpha_t']:.4f}."
            )
        else:
            estado_serie["aci"]["target_miscoverage"] = float(aci_target)
            estado_serie["aci"]["step_size"] = float(aci_step_size)
            estado_serie = _actualizar_estado_con_observaciones(
                codigo, series[codigo], estado_serie
            )

        aci = SequentialACI.from_state_dict(estado_serie["aci"])

        filas_historico.extend(
            _backtest_causal_reciente(
                codigo, series[codigo], aci, meses=meses_calibracion
            )
        )

        if metodo == "KRR":
            pred = modelo.pronosticar_serie(codigo, series[codigo], horizonte)
        else:
            pred = pronostico_anio_anterior(series[codigo], futuro)

        pronostico[codigo] = pred.to_numpy(float)
        metodos.append(
            {
                "codigo_producto": codigo,
                "metodo": metodo,
                "tiene_hiperparametros": codigo in HIPERPARAMETROS_POR_SERIE,
                "n_scores_aci": len(aci.scores),
                "alpha_aci": float(aci.alpha_t),
            }
        )

        pending = None
        for fecha, punto in pred.items():
            inferior, superior, half_width = finalizar_intervalo(
                float(punto), aci.interval(float(punto))
            )
            filas_intervalos.append(
                {
                    "mes": pd.Timestamp(fecha),
                    "codigo_producto": codigo,
                    "metodo": metodo,
                    "prediccion": float(punto),
                    "inferior": inferior,
                    "superior": superior,
                    "half_width": half_width,
                    "alpha_aci": float(aci.alpha_t),
                }
            )

            # Solo el mes inmediato queda pendiente para la actualización ACI real.
            if pending is None:
                pending = {
                    "mes": _fecha_iso(fecha),
                    "prediccion": float(punto),
                    "inferior": inferior,
                    "superior": superior,
                    "metodo": metodo,
                }

        estado_serie["metodo"] = metodo
        estado_serie["aci"] = aci.state_dict()
        estado_serie["pending"] = pending
        estado["series"][codigo] = estado_serie

    pronostico.index.name = "mes"
    intervalos = pd.DataFrame(filas_intervalos)
    if not intervalos.empty:
        intervalos = intervalos[
            [
                "mes",
                "codigo_producto",
                "metodo",
                "prediccion",
                "inferior",
                "superior",
                "half_width",
                "alpha_aci",
            ]
        ].sort_values(["mes", "codigo_producto"], kind="stable")

    historico = pd.DataFrame(filas_historico)
    if not historico.empty:
        historico = historico[
            ["mes", "codigo_producto", "metodo", "prediccion", "inferior", "superior"]
        ].sort_values(["mes", "codigo_producto"], kind="stable")

    estado["version"] = ESTADO_ACI_VERSION
    estado["last_history_month"] = _fecha_iso(series.index[-1])
    estado["config"] = {
        "aci_target_miscoverage": float(aci_target),
        "aci_nominal_coverage": float(1.0 - aci_target),
        "aci_step_size": float(aci_step_size),
        "calibration_months": int(meses_calibracion),
    }
    return pronostico, pd.DataFrame(metodos), intervalos, historico, estado


def cargar_panel(ruta: Path) -> pd.DataFrame:
    """Carga y valida el panel mensual producido por preprocesamiento_icmd.py."""
    series = pd.read_csv(ruta, index_col="mes", parse_dates=True)
    if series.empty:
        raise ValueError("El archivo de series está vacío.")

    series.index = pd.to_datetime(series.index).to_period("M").to_timestamp()
    series = series[~series.index.duplicated(keep="last")].sort_index()

    # Conserva una rejilla mensual completa. Los huecos internos equivalen a 0 ventas.
    rejilla = pd.date_range(series.index.min(), series.index.max(), freq="MS")
    series = series.reindex(rejilla).fillna(0.0)
    series.index.name = "mes"

    for c in series.columns:
        series[c] = pd.to_numeric(series[c], errors="coerce").fillna(0.0)

    return series


COLUMNAS_HISTORICO = ["mes", "codigo_producto", "metodo", "prediccion", "inferior", "superior"]


def _fusionar_historico(ruta: Path, nuevo: pd.DataFrame) -> pd.DataFrame:
    """Acumula el histórico de predicciones causales de meses ya observados.

    Cada corrida solo trae registros de los meses recién confirmados (o, la
    primera vez que se calibra una serie, la ventana de calibración inicial).
    Se combinan con lo ya persistido para que el histórico crezca mes a mes
    en vez de recalcularse desde cero cada vez.
    """
    previo = (
        pd.read_csv(ruta, parse_dates=["mes"]) if ruta.exists() else pd.DataFrame(columns=COLUMNAS_HISTORICO)
    )

    combinado = pd.concat([previo, nuevo], ignore_index=True)
    if combinado.empty:
        return combinado.reindex(columns=COLUMNAS_HISTORICO)

    combinado["mes"] = pd.to_datetime(combinado["mes"])
    combinado = combinado.drop_duplicates(subset=["mes", "codigo_producto"], keep="last")
    combinado = combinado.sort_values(["mes", "codigo_producto"], kind="stable")
    return combinado[COLUMNAS_HISTORICO]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--series",
        type=Path,
        default=Path("data/datos_preprocesados/series_mensuales.csv"),
        help="Panel mensual generado por preprocesamiento_icmd.py.",
    )
    parser.add_argument(
        "--horizonte",
        type=int,
        default=1,
        help="Meses futuros a pronosticar. Por defecto: solo el siguiente mes.",
    )
    parser.add_argument(
        "--salidas",
        type=Path,
        default=Path("data/salidas_prediccion"),
        help="Directorio de salida.",
    )
    parser.add_argument(
        "--aci-target",
        type=float,
        default=ACI_TARGET_MISCOVERAGE,
        help="No cobertura objetivo ACI. 0.10 implica cobertura nominal 90%%.",
    )
    parser.add_argument(
        "--aci-step-size",
        type=float,
        default=ACI_STEP_SIZE,
        help="Paso adaptativo de alpha_t.",
    )
    parser.add_argument(
        "--calibracion-meses",
        type=int,
        default=ACI_CALIBRATION_MONTHS,
        help="Meses usados en la calibración causal inicial ACI.",
    )
    parser.add_argument(
        "--reiniciar-aci",
        action="store_true",
        help="Ignora estado_aci.json y recalibra desde el histórico.",
    )
    args = parser.parse_args()

    if not args.series.exists():
        raise SystemExit(f"No se encontró el panel de series: {args.series}")
    if args.calibracion_meses <= 0:
        raise SystemExit("--calibracion-meses debe ser mayor que cero.")

    args.salidas.mkdir(parents=True, exist_ok=True)
    ruta_estado = args.salidas / "estado_aci.json"

    series = cargar_panel(args.series)
    print(
        f"[DATOS] {series.shape[1]:,} productos x {series.shape[0]:,} meses "
        f"({series.index.min():%Y-%m} a {series.index.max():%Y-%m})."
    )
    presentes_krr = [c for c in series.columns if c in HIPERPARAMETROS_POR_SERIE]
    fallback = [c for c in series.columns if c not in HIPERPARAMETROS_POR_SERIE]
    print(
        f"[MODELO] KRR configurado: {len(presentes_krr)}; "
        f"fallback y[t-12]: {len(fallback)}."
    )
    print(
        f"[ACI] cobertura nominal={1.0 - args.aci_target:.1%}; "
        f"step_size={args.aci_step_size}; calibración={args.calibracion_meses} meses."
    )

    pronostico, metodos, intervalos, historico_nuevo, estado = pronosticar_todas_las_series(
        series,
        args.horizonte,
        ruta_estado=ruta_estado,
        meses_calibracion=args.calibracion_meses,
        aci_target=args.aci_target,
        aci_step_size=args.aci_step_size,
        reiniciar_aci=args.reiniciar_aci,
    )

    ruta_pronostico = args.salidas / "pronostico.csv"
    ruta_intervalos = args.salidas / "pronostico_intervalos.csv"
    ruta_historico = args.salidas / "pronostico_historico.csv"
    ruta_metodos = args.salidas / "metodo_por_serie.csv"

    historico = _fusionar_historico(ruta_historico, historico_nuevo)

    pronostico.to_csv(ruta_pronostico, encoding="utf-8-sig")
    intervalos.to_csv(ruta_intervalos, index=False, encoding="utf-8-sig")
    historico.to_csv(ruta_historico, index=False, encoding="utf-8-sig")
    metodos.to_csv(ruta_metodos, index=False, encoding="utf-8-sig")
    _guardar_estado_aci(ruta_estado, estado)

    print("\nPronóstico puntual:")
    print(pronostico.round(0).to_string())
    print("\nPronóstico con intervalos ACI:")
    print(intervalos.head(min(20, len(intervalos))).round(3).to_string(index=False))
    print("\nArchivos generados:")
    print(f"  {ruta_pronostico}")
    print(f"  {ruta_intervalos}")
    print(f"  {ruta_historico}")
    print(f"  {ruta_metodos}")
    print(f"  {ruta_estado}")


if __name__ == "__main__":
    main()
