# -*- coding: utf-8 -*-
"""shapefile.py - lector minimo de shapefiles (.shp, .shx y .dbf) con la biblioteca estandar (M5.1).

Mapa del rio (D-013, punto 5, Q11-A; propuesta del Mapa §11.3, punto 1): geometry.py lee con el las descargas de Natural
Earth 1:10m (ne_10m_rivers_lake_centerlines, ne_10m_land y ne_10m_coastline) y de HydroRIVERS v1.0 (HydroRIVERS_v10_sa)
desde tools/river-map/source/. Solo struct y mmap: sin paquetes nuevos (08 §4.5).

Formato (ESRI Shapefile Technical Description, julio de 1998; dBASE III para el .dbf):
- .shp: cabecera de 100 bytes (codigo 9994 big-endian, version 1000 y tipo de forma little-endian, caja en dobles LE) y
  registros con cabecera big-endian (numero y longitud en palabras de 16 bits) y contenido little-endian. Se leen los
  tipos 0 (nulo), 3 (PolyLine), 5 (Polygon) y sus variantes con M (23, 25) y Z (13, 15), de las que solo se toman X e Y.
- .shx: la misma cabecera y, por registro, su desplazamiento y su longitud (big-endian, en palabras): acceso directo.
- .dbf: cabecera con el numero de registros, el largo de la cabecera y el de cada registro, y los campos (nombre, tipo,
  largo y decimales). Tipos C (texto, en la codificacion del .cpg o UTF-8), N y F (numeros; vacio = None), L y D (texto).
  Los registros borrados (marca «*») se devuelven como None en record() y se saltan en column() con None.

Los archivos grandes (HydroRIVERS: 212 MB de .shp y 167 MB de .dbf) se abren con mmap y se leen por registro, sin cargarlos
en memoria. Coordenadas tal cual (X = longitud, Y = latitud en WGS 84 para las dos fuentes).
"""
from __future__ import annotations

import mmap
import struct
from pathlib import Path

NULL_SHAPE = 0
POLYLINE_TYPES = frozenset({3, 13, 23})
POLYGON_TYPES = frozenset({5, 15, 25})
SUPPORTED_TYPES = POLYLINE_TYPES | POLYGON_TYPES | {NULL_SHAPE}
FILE_CODE = 9994
VERSION = 1000


class ShapefileError(Exception):
    """El archivo no es un shapefile que este lector entienda."""


def _open_map(path: Path):
    f = open(path, "rb")
    try:
        size = Path(path).stat().st_size
        if size == 0:
            raise ShapefileError(f"{Path(path).name}: archivo vacio")
        return f, mmap.mmap(f.fileno(), 0, access=mmap.ACCESS_READ)
    except Exception:
        f.close()
        raise


class Shapes:
    """Geometria de un .shp con su indice .shx: tipo, caja, numero de registros, caja y partes de cada registro."""

    def __init__(self, shp: Path, shx: Path | None = None):
        self.path = Path(shp)
        shx = Path(shx) if shx is not None else self.path.with_suffix(".shx")
        self._f, self._m = _open_map(self.path)
        try:
            self._fx, self._mx = _open_map(shx)
        except Exception:
            self.close()
            raise
        self.shape_type, self.bbox = self._header(self._m, self.path.name)
        self._header(self._mx, shx.name)
        n = (len(self._mx) - 100) // 8
        if 100 + 8 * n != len(self._mx):
            raise ShapefileError(f"{shx.name}: el indice no tiene registros de 8 bytes")
        self.count = n
        if self.shape_type not in SUPPORTED_TYPES:
            raise ShapefileError(f"{self.path.name}: tipo de forma {self.shape_type} no soportado")

    @staticmethod
    def _header(m, name: str) -> tuple:
        if len(m) < 100:
            raise ShapefileError(f"{name}: cabecera de menos de 100 bytes")
        code, = struct.unpack_from(">i", m, 0)
        version, shape_type = struct.unpack_from("<ii", m, 28)
        if code != FILE_CODE or version != VERSION:
            raise ShapefileError(f"{name}: no es un shapefile (codigo {code}, version {version})")
        return shape_type, struct.unpack_from("<4d", m, 36)

    def close(self) -> None:
        for name in ("_m", "_f", "_mx", "_fx"):
            obj = getattr(self, name, None)
            if obj is not None:
                obj.close()
                setattr(self, name, None)

    def __enter__(self) -> "Shapes":
        return self

    def __exit__(self, *exc) -> None:
        self.close()

    def _content(self, i: int) -> tuple:
        """(desplazamiento del contenido, su longitud en bytes, tipo de forma) del registro i (desde 0)."""
        if not 0 <= i < self.count:
            raise IndexError(i)
        off_words, len_words = struct.unpack_from(">ii", self._mx, 100 + 8 * i)
        off = 2 * off_words
        number, clen = struct.unpack_from(">ii", self._m, off)
        if number != i + 1 or clen != len_words or off + 8 + 2 * clen > len(self._m):
            raise ShapefileError(f"{self.path.name}: el registro {i + 1} no coincide con el indice")
        start = off + 8
        shape_type, = struct.unpack_from("<i", self._m, start)
        if shape_type != NULL_SHAPE and shape_type != self.shape_type:
            raise ShapefileError(f"{self.path.name}: el registro {i + 1} es de tipo {shape_type}")
        return start, 2 * clen, shape_type

    def record_bbox(self, i: int) -> tuple | None:
        """Caja (xmin, ymin, xmax, ymax) del registro i, o None si es nulo."""
        start, _, shape_type = self._content(i)
        if shape_type == NULL_SHAPE:
            return None
        return struct.unpack_from("<4d", self._m, start + 4)

    def parts(self, i: int) -> list:
        """Partes del registro i: una lista de listas de puntos (x, y) en el orden del archivo (vacia si es nulo)."""
        start, length, shape_type = self._content(i)
        if shape_type == NULL_SHAPE:
            return []
        nparts, npoints = struct.unpack_from("<ii", self._m, start + 36)
        if nparts < 0 or npoints < 0 or 44 + 4 * nparts + 16 * npoints > length:
            raise ShapefileError(f"{self.path.name}: el registro {i + 1} tiene partes o puntos fuera de su longitud")
        index = list(struct.unpack_from(f"<{nparts}i", self._m, start + 44)) + [npoints]
        base = start + 44 + 4 * nparts
        flat = struct.unpack_from(f"<{2 * npoints}d", self._m, base)
        out = []
        for a, b in zip(index, index[1:]):
            if not 0 <= a <= b <= npoints:
                raise ShapefileError(f"{self.path.name}: el registro {i + 1} tiene partes desordenadas")
            out.append([(flat[2 * k], flat[2 * k + 1]) for k in range(a, b)])
        return out


class Table:
    """Atributos de un .dbf (dBASE III): campos y valores por registro o por columna."""

    def __init__(self, dbf: Path, encoding: str | None = None):
        self.path = Path(dbf)
        if encoding is None:
            cpg = self.path.with_suffix(".cpg")
            encoding = cpg.read_text(encoding="ascii").strip() if cpg.exists() else "utf-8"
            if encoding.upper() in ("UTF-8", "UTF8"):
                encoding = "utf-8"
        self.encoding = encoding
        self._f, self._m = _open_map(self.path)
        try:
            self._parse_header()
        except Exception:
            self.close()
            raise

    def _parse_header(self) -> None:
        m = self._m
        if len(m) < 33:
            raise ShapefileError(f"{self.path.name}: cabecera dBASE incompleta")
        self.count, self.header_len, self.record_len = struct.unpack_from("<IHH", m, 4)
        fields = []
        pos = 32
        offset = 1   # el primer byte de cada registro es la marca de borrado
        while pos < self.header_len - 1 and m[pos] != 0x0D:
            raw = bytes(m[pos:pos + 32])
            if len(raw) < 32:
                raise ShapefileError(f"{self.path.name}: descriptor de campo incompleto")
            name = raw[:11].split(b"\x00", 1)[0].decode("ascii", "replace").strip()
            ftype = chr(raw[11])
            flen, fdec = raw[16], raw[17]
            fields.append((name, ftype, offset, flen, fdec))
            offset += flen
            pos += 32
        if offset != self.record_len:
            raise ShapefileError(f"{self.path.name}: los campos suman {offset} bytes y el registro mide {self.record_len}")
        if self.header_len + self.count * self.record_len > len(m):
            raise ShapefileError(f"{self.path.name}: el archivo es mas corto que sus {self.count} registros")
        self.fields = fields
        self._by_name = {f[0]: f for f in fields}

    def close(self) -> None:
        for name in ("_m", "_f"):
            obj = getattr(self, name, None)
            if obj is not None:
                obj.close()
                setattr(self, name, None)

    def __enter__(self) -> "Table":
        return self

    def __exit__(self, *exc) -> None:
        self.close()

    @property
    def names(self) -> list:
        return [f[0] for f in self.fields]

    def _field(self, name: str) -> tuple:
        try:
            return self._by_name[name]
        except KeyError:
            raise ShapefileError(f"{self.path.name}: no tiene el campo {name!r} (campos: {', '.join(self.names)})") from None

    def _value(self, field: tuple, raw: bytes):
        _, ftype, _, _, fdec = field
        if ftype in ("N", "F"):
            text = raw.strip()
            if not text or text.startswith(b"*"):
                return None
            if fdec == 0 and b"." not in text and b"e" not in text.lower():
                return int(text)
            return float(text)
        if ftype == "L":
            c = raw.strip()[:1].upper()
            return True if c in (b"T", b"Y") else False if c in (b"F", b"N") else None
        return raw.decode(self.encoding, "replace").rstrip(" \x00")

    def column(self, name: str) -> list:
        """Valores del campo name de todos los registros, en orden (None en los borrados)."""
        field = self._field(name)
        _, _, off, flen, _ = field
        rl = self.record_len
        base = self.header_len
        fmt = f"<{off}x{flen}s{rl - off - flen}x" if rl - off - flen else f"<{off}x{flen}s"
        flags = struct.Struct(f"<c{rl - 1}x")
        area = memoryview(self._m)[base:base + self.count * rl]
        try:
            raws = [r[0] for r in struct.iter_unpack(fmt, area)]
            deleted = [f[0] == b"*" for f in flags.iter_unpack(area)]
        finally:
            area.release()
        return [None if d else self._value(field, raw) for raw, d in zip(raws, deleted)]

    def record(self, i: int) -> dict | None:
        """Registro i (desde 0) como {campo: valor}, o None si esta borrado."""
        if not 0 <= i < self.count:
            raise IndexError(i)
        start = self.header_len + i * self.record_len
        raw = bytes(self._m[start:start + self.record_len])
        if raw[:1] == b"*":
            return None
        return {f[0]: self._value(f, raw[f[2]:f[2] + f[3]]) for f in self.fields}


class Layer:
    """Un shapefile entero (la ruta sin extension): Shapes del .shp/.shx y Table del .dbf, con el mismo numero de registros."""

    def __init__(self, base: Path):
        base = Path(base)
        self.name = base.name
        self.shapes = Shapes(base.with_suffix(".shp"), base.with_suffix(".shx"))
        try:
            self.table = Table(base.with_suffix(".dbf"))
        except Exception:
            self.shapes.close()
            raise
        if self.shapes.count != self.table.count:
            self.close()
            raise ShapefileError(f"{self.name}: {self.shapes.count} formas y {self.table.count} registros en el .dbf")
        self.count = self.shapes.count

    def close(self) -> None:
        self.shapes.close()
        self.table.close()

    def __enter__(self) -> "Layer":
        return self

    def __exit__(self, *exc) -> None:
        self.close()


# ----------------------------------------------------------------------------------------------- escritura (fixtures)

def write_layer(base: Path, shape_type: int, records: list, fields: list, encoding: str = "utf-8") -> None:
    """Escribe un shapefile pequenno (.shp, .shx, .dbf y .cpg) para las pruebas: records es una lista de (partes, valores),
    con partes = lista de listas de (x, y) ([] = forma nula) y valores en el orden de fields = [(nombre, tipo, largo,
    decimales)], tipos C o N."""
    base = Path(base)
    if shape_type not in (3, 5):
        raise ShapefileError("write_layer solo escribe PolyLine (3) y Polygon (5)")
    contents = []
    xs, ys = [], []
    for parts, _ in records:
        if not parts:
            contents.append(struct.pack("<i", NULL_SHAPE))
            continue
        pts = [p for part in parts for p in part]
        bx = (min(p[0] for p in pts), min(p[1] for p in pts), max(p[0] for p in pts), max(p[1] for p in pts))
        xs += [bx[0], bx[2]]
        ys += [bx[1], bx[3]]
        index, k = [], 0
        for part in parts:
            index.append(k)
            k += len(part)
        body = struct.pack("<i4dii", shape_type, *bx, len(parts), len(pts))
        body += struct.pack(f"<{len(index)}i", *index)
        body += b"".join(struct.pack("<2d", x, y) for x, y in pts)
        contents.append(body)
    box = (min(xs), min(ys), max(xs), max(ys)) if xs else (0.0, 0.0, 0.0, 0.0)

    def header(total_bytes: int) -> bytes:
        return (struct.pack(">i", FILE_CODE) + b"\x00" * 20 + struct.pack(">i", total_bytes // 2)
                + struct.pack("<ii", VERSION, shape_type) + struct.pack("<4d", *box) + struct.pack("<4d", 0, 0, 0, 0))

    shp = bytearray()
    shx = bytearray()
    offset = 100
    for n, body in enumerate(contents, start=1):
        shx += struct.pack(">ii", offset // 2, len(body) // 2)
        shp += struct.pack(">ii", n, len(body) // 2) + body
        offset += 8 + len(body)
    base.with_suffix(".shp").write_bytes(header(100 + len(shp)) + bytes(shp))
    base.with_suffix(".shx").write_bytes(header(100 + len(shx)) + bytes(shx))
    rec_len = 1 + sum(f[2] for f in fields)
    head_len = 32 + 32 * len(fields) + 1
    dbf = bytearray(struct.pack("<B3BIHH20x", 3, 126, 10, 18, len(records), head_len, rec_len))
    for name, ftype, flen, fdec in fields:
        dbf += name.encode("ascii")[:11].ljust(11, b"\x00") + ftype.encode("ascii") + b"\x00" * 4
        dbf += bytes([flen, fdec]) + b"\x00" * 14
    dbf += b"\x0d"
    for _, values in records:
        dbf += b" "
        for (name, ftype, flen, fdec), v in zip(fields, values):
            if ftype == "N":
                text = "" if v is None else (f"{v:.{fdec}f}" if fdec else str(int(v)))
                dbf += text.rjust(flen).encode("ascii")[:flen]
            else:
                dbf += ("" if v is None else str(v)).encode(encoding)[:flen].ljust(flen, b" ")
    dbf += b"\x1a"
    base.with_suffix(".dbf").write_bytes(bytes(dbf))
    base.with_suffix(".cpg").write_text("UTF-8", encoding="ascii")
