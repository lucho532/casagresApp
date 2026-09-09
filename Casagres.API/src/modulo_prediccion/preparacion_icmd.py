#!/usr/bin/env python3
"""
Preprocesamiento ICMD para pronóstico mensual
=============================================

Convierte el CSV transaccional crudo en un panel mensual completo:
    filas    -> meses (inicio de mes)
    columnas -> Referencia Producto
    valores  -> suma mensual de Cantidad_Neta

A diferencia del script original, NO se limita a las TOP 10 series. Se conservan
TODAS las referencias válidas del dataset para que el módulo de predicción pueda:

1. aplicar KRR a las referencias con hiperparámetros específicos, y
2. aplicar la regla estacional y[t-12] al resto de referencias.

Uso:
    python preprocesamiento_icmd.py
    python preprocesamiento_icmd.py --csv df_preprocessed.csv --salidas datos_preprocesados

Salidas:
    datos_preprocesados/series_mensuales.csv
    datos_preprocesados/catalogo_series.csv
"""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
import pandas as pd

# =========================================================================== #
# CONFIGURACIÓN
# =========================================================================== #
COL_FECHA = "Periodo"  # entero YYYYMM
COL_CLAVE = "Referencia Producto"  # clave de producto
COL_ETIQUETA = "Descripcion Producto"
COL_CANTIDAD = "Cantidad_Neta"  # objetivo del pronóstico
COL_INGRESOS = "Valor Venta Neta"


# =========================================================================== #
# CARGA Y DEPURACIÓN
# =========================================================================== #
def cargar_y_depurar(ruta: Path) -> pd.DataFrame:
    """Aplica el pipeline de depuración necesario antes de agregar por mes."""
    df = pd.read_csv(ruta, low_memory=False)
    n0 = len(df)

    columnas_obligatorias = {COL_FECHA, COL_CLAVE, COL_CANTIDAD}
    faltantes = columnas_obligatorias.difference(df.columns)
    if faltantes:
        raise ValueError(
            "El CSV no contiene las columnas obligatorias: "
            + ", ".join(sorted(faltantes))
        )

    # Se eliminan únicamente columnas constantes que no sean necesarias para el ETL.
    protegidas = {COL_FECHA, COL_CLAVE, COL_ETIQUETA, COL_CANTIDAD, COL_INGRESOS}
    sin_info = [
        c for c in df.columns if c not in protegidas and df[c].nunique(dropna=True) <= 1
    ]
    df = df.drop(columns=sin_info)

    texto = [
        c for c in df.columns if df[c].dtype == object or str(df[c].dtype) == "str"
    ]
    for c in texto:
        df[c] = df[c].astype(str).str.strip().replace({"": np.nan, "nan": np.nan})
        if df[c].nunique(dropna=True) / max(len(df), 1) < 0.5:
            df[c] = df[c].str.upper()

    df = df.drop_duplicates().reset_index(drop=True)

    periodo = pd.to_numeric(df[COL_FECHA], errors="coerce")
    df["mes"] = pd.to_datetime(
        periodo.astype("Int64").astype(str), format="%Y%m", errors="coerce"
    )
    df = df.dropna(subset=["mes", COL_CLAVE])

    for c in (COL_CANTIDAD, COL_INGRESOS):
        if c in df.columns:
            df[c] = pd.to_numeric(df[c], errors="coerce").fillna(0.0)

    print(
        f"[ETL] {n0:,} filas -> {len(df):,} "
        f"({len(df) / max(n0, 1):.2%} conservado); "
        f"{len(sin_info)} columnas sin información descartadas."
    )
    return df


# =========================================================================== #
# CONSTRUCCIÓN DEL PANEL MENSUAL
# =========================================================================== #
def construir_panel_mensual(df: pd.DataFrame) -> tuple[pd.DataFrame, pd.DataFrame]:
    """Construye el panel mensual de TODAS las referencias del dataset."""
    agregado = (
        df.groupby([COL_CLAVE, "mes"], observed=True)[COL_CANTIDAD].sum().reset_index()
    )

    if agregado.empty:
        raise ValueError(
            "No quedaron observaciones válidas después del preprocesamiento."
        )

    rejilla = pd.date_range(agregado["mes"].min(), agregado["mes"].max(), freq="MS")

    panel = (
        agregado.pivot(index="mes", columns=COL_CLAVE, values=COL_CANTIDAD)
        .reindex(rejilla)
        .fillna(0.0)
        .sort_index(axis=1)
    )
    panel.index.name = "mes"

    if COL_ETIQUETA in df.columns:
        catalogo_nombres = (
            df.dropna(subset=[COL_CLAVE])
            .drop_duplicates(subset=COL_CLAVE)
            .set_index(COL_CLAVE)[COL_ETIQUETA]
            .astype(str)
            .to_dict()
        )
    else:
        catalogo_nombres = {}

    meses_con_venta = (panel != 0).sum(axis=0)
    catalogo = (
        pd.DataFrame(
            {
                "codigo_producto": panel.columns,
                "producto": [catalogo_nombres.get(c, c) for c in panel.columns],
                "meses_con_ventas": meses_con_venta.to_numpy(),
                "pct_continuidad": (meses_con_venta / max(len(panel), 1) * 100)
                .round(2)
                .to_numpy(),
                "volumen_total": panel.sum(axis=0).to_numpy(),
            }
        )
        .sort_values(["pct_continuidad", "volumen_total"], ascending=False)
        .reset_index(drop=True)
    )

    print(
        f"[ETL] Panel mensual: {len(panel):,} meses "
        f"({panel.index.min():%Y-%m} a {panel.index.max():%Y-%m}) x "
        f"{panel.shape[1]:,} productos."
    )
    return panel, catalogo


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--csv",
        type=Path,
        default=Path("data\df_preprocessed.csv"),
        help="CSV transaccional crudo del dataset ICMD.",
    )
    parser.add_argument(
        "--salidas",
        type=Path,
        default=Path("data/datos_preprocesados"),
        help="Directorio donde se guardará el panel mensual.",
    )
    args = parser.parse_args()

    if not args.csv.exists():
        raise SystemExit(f"No se encontró el CSV de entrada: {args.csv}")

    args.salidas.mkdir(parents=True, exist_ok=True)

    df = cargar_y_depurar(args.csv)
    panel, catalogo = construir_panel_mensual(df)

    ruta_panel = args.salidas / "series_mensuales.csv"
    ruta_catalogo = args.salidas / "catalogo_series.csv"

    panel.to_csv(ruta_panel, encoding="utf-8-sig")
    catalogo.to_csv(ruta_catalogo, index=False, encoding="utf-8-sig")

    print("\nArchivos generados:")
    print(f"  {ruta_panel}")
    print(f"  {ruta_catalogo}")


if __name__ == "__main__":
    main()
