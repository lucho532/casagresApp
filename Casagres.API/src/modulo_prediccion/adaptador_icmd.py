#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Adaptador ICMD
==============

Toma el CSV limpio generado por procesamiento_analitica_final.py
y genera el archivo df_preprocessed.csv con el formato esperado
por preparacion_icmd.py.

Entrada:
    C:\source\casagres\data\Ventas_Casagres_Limpio_PowerBI.csv

Salida:
    C:\source\casagres\data\df_preprocessed.csv
"""

from pathlib import Path

import pandas as pd


class AdaptadorICMD:
    def __init__(self, carpeta_datos):
        self.carpeta_datos = Path(carpeta_datos)

        self.archivo_entrada = (
            self.carpeta_datos / "Ventas_Casagres_Limpio_PowerBI.xlsx"
        )

        self.archivo_salida = self.carpeta_datos / "df_preprocessed.csv"

    def cargar_datos(self):
        """Carga el CSV generado por el ETL."""

        if not self.archivo_entrada.exists():
            raise FileNotFoundError(
                f"No se encontró el archivo de entrada: {self.archivo_entrada}"
            )

        print(f"[ADAPTADOR] Cargando:")
        print(f"  {self.archivo_entrada}")

        df = pd.read_excel(self.archivo_entrada, engine="openpyxl")

        print(f"[ADAPTADOR] Registros cargados: {len(df):,}")

        return df

    def transformar_columnas(self, df):
        """Adapta los nombres de columnas al formato esperado por ICMD."""

        columnas = {
            "periodo": "Periodo",
            "referencia_producto": "Referencia Producto",
            "descripcion_producto": "Descripcion Producto",
            "cantidad_neta": "Cantidad_Neta",
            "valor_venta_neta": "Valor Venta Neta",
        }

        faltantes = [columna for columna in columnas if columna not in df.columns]

        if faltantes:
            raise ValueError(
                "Faltan columnas necesarias en el CSV limpio: " + ", ".join(faltantes)
            )

        df = df.rename(columns=columnas)

        return df

    def transformar_periodo(self, df):
        """
        Convierte Periodo al formato YYYYMM esperado
        por preparacion_icmd.py.
        """

        periodo = df["Periodo"]

        # Caso normal: el ETL puede haber generado Period
        if isinstance(periodo.dtype, pd.PeriodDtype):
            df["Periodo"] = periodo.dt.strftime("%Y%m")

        else:
            # Convertimos cualquier representación de fecha
            # a YYYYMM.
            fecha = pd.to_datetime(periodo, errors="coerce")

            df["Periodo"] = fecha.dt.strftime("%Y%m")

        return df

    def convertir_numericos(self, df):
        """Asegura que las variables numéricas sean realmente numéricas."""

        columnas_numericas = [
            "Cantidad_Neta",
            "Valor Venta Neta",
        ]

        for columna in columnas_numericas:
            if columna in df.columns:
                df[columna] = pd.to_numeric(
                    df[columna].astype(str).str.replace(",", ".", regex=False),
                    errors="coerce",
                ).fillna(0.0)

        return df

    def validar(self, df):
        """Realiza validaciones básicas antes de generar el archivo."""

        columnas_obligatorias = [
            "Periodo",
            "Referencia Producto",
            "Descripcion Producto",
            "Cantidad_Neta",
            "Valor Venta Neta",
        ]

        faltantes = [
            columna for columna in columnas_obligatorias if columna not in df.columns
        ]

        if faltantes:
            raise ValueError(
                "El DataFrame no contiene las columnas obligatorias: "
                + ", ".join(faltantes)
            )

        registros_invalidos_periodo = df["Periodo"].isna().sum()

        if registros_invalidos_periodo > 0:
            print(
                f"[ADAPTADOR] ⚠ "
                f"{registros_invalidos_periodo:,} registros "
                f"sin Periodo válido."
            )

        registros_sin_producto = df["Referencia Producto"].isna().sum()

        if registros_sin_producto > 0:
            print(
                f"[ADAPTADOR] ⚠ "
                f"{registros_sin_producto:,} registros "
                f"sin referencia de producto."
            )

        print(
            f"[ADAPTADOR] Productos encontrados: "
            f"{df['Referencia Producto'].nunique():,}"
        )

    def guardar(self, df):
        """Guarda el DataFrame en el formato esperado por ICMD."""

        df.to_csv(self.archivo_salida, index=False, encoding="utf-8-sig")

        print("\n[ADAPTADOR] ✓ Archivo generado:")
        print(f"  {self.archivo_salida}")

    def ejecutar(self):
        """Ejecuta todo el proceso de adaptación."""

        print("\n========================================")
        print("      ADAPTADOR ICMD")
        print("========================================")

        df = self.cargar_datos()

        df = self.transformar_columnas(df)

        df = self.transformar_periodo(df)

        df = self.convertir_numericos(df)

        self.validar(df)

        self.guardar(df)

        print("========================================")
        print("      ADAPTACIÓN FINALIZADA")
        print("========================================\n")


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--carpeta-datos",
        type=str,
        default=r"C:\source\casagres\Casagres.API\proyecto_icmd",
        help="Carpeta con el Excel limpio y donde se guardará df_preprocessed.csv.",
    )
    args = parser.parse_args()

    adaptador = AdaptadorICMD(args.carpeta_datos)

    adaptador.ejecutar()
