#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Tableros de referencia de 12-14 cuias para cada mecánica y para cada combinación del calendario 1-200 (propuesta de
    /// D-007, d.4) que usan los tests de rendimiento y de asignaciones de C3-3 (CLAUDE.md, regla dura 2; 03 §5-§6; 08 §14.2;
    /// GDD §6 #6).
    /// </summary>
    /// <remarks>
    /// <para><b>Cómo se generaron</b> (2026-10-08, una vez, con un programa fuera del repositorio sobre AF.Core y AF.Solver):
    /// tablero base con <c>InverseWalk</c> (03 §4.1; K = 10 colores de 4 dosis, E = 2 vacíos, uno más por cada mecánica que
    /// ocupa un vacío además de la primera: Hoja, Matapi, Lluvia y la cueva de la Piraña con Liana; una cuia de 4 más para que
    /// haya cuias a medias con Barro, Brote o Aguas turbias; Cuia chica de 2 con «Ch» y como cueva de la Piraña; Cuia alta de
    /// 5 o 6 con su fruta abundante con «Al» y «Al6»), superposición de las mecánicas (03 §9: Lluvia sobre una vacía con el
    /// agua 10, n = 2 y k = 3; Barro insertado en la capa 2-3 de una cuia a medias; 2-4 dosis ocultas con T3 y T4; Brote
    /// sobre una cuia a medias; Liana con fruta sobre una cuia con contenido; Hoja y Matapi sobre una vacía; Remolino entre
    /// dos cuias; Piraña con k = 4) y verificación con <see cref="LevelValidation"/> y A* (w = 1, 300.000 nodos). De 30
    /// semillas por combinación se eligió el tablero del percentil 75 de estados expandidos entre los resueltos (carga
    /// representativa, no el peor caso); el reparto completo está en la tabla de evidencias del README del paquete.</para>
    /// <para><see cref="ReferenceBoard.Expanded"/> es el número de estados que expandió A* al generarlo; los tests lo fijan
    /// con un margen del 25 % (nunca más de 300.000, 03 §5).</para>
    /// <para><b>Ti</b> (Tinaja, M13; T55, 2026-10-13, con un programa fuera del repositorio): el reto o hito de c.9 de la
    /// propuesta de la tinaja (K = 9, 1 vacío, 5 dosis con 4 ocultas). Tablero base con <c>InverseWalk</c> (K = 9, E = 1); el
    /// paso «Ti» de c.3 saca 5 dosis de las cimas, las ordena con T4 (boca visible, las demás ocultas) y añade la tinaja de 5
    /// y su cuia de 2 al final; verificación con <see cref="LevelValidation"/>, A* sin poda de fútiles (b.2 R14:
    /// <c>SolverOptions.ForPipeline(mode, level)</c>) y <c>SafePath</c>. De 30 semillas: 30 válidos, 30 resueltos y 30 con
    /// ruta segura (2-24 realidades; 130-7.525 ms en Release); expandidos: mediana 4.434, máximo 25.596; percentil 75 en la
    /// semilla 27 (par 23, 6 realidades). Con la poda, A* expande lo mismo o unos pocos estados menos y da el mismo par en las
    /// 30.</para>
    /// <para><b>HoA</b> y <b>Br2</b> (variantes de la 1.0, K3a, 2026-10-14, con un programa fuera del repositorio en el equipo de
    /// escritorio x64, .NET 10 Release): tablero base con <c>InverseWalk</c> (K = 10 colores de 4, 13 cuias de 4 y E = 2, con
    /// una cuia de más para que haya cuias a medias), y superposición de la variante en una cuia elegida con el PRNG del núcleo:
    /// la Hoja atada sobre una cuia con dosis y no sellada, con la llave = la fruta de su fondo (b.3: «puede empezar con dosis
    /// cuyo fondo es c»; capacidad 4 = <c>U_c</c>, la regla [P] de K3a); el Brote de dos hojas (<c>n</c> = 2) sobre una cuia
    /// con 1-3 dosis y espacio ≥ 2. Verificación con <see cref="LevelValidation"/>, A* (w = 1, 300.000 nodos) y Morpho (w = 2,
    /// 40.000). De 30 semillas: 30 válidos, 30 resueltos y 30 con Morpho; expandidos, mediana 498 y máximo 25.202 (HoA) y 580
    /// y 25.266 (Br2); percentil 75 en la semilla 23 (HoA) y 29 (Br2).</para>
    /// <para><b>Fl</b> (Flor de vitória-régia, variante de la 1.0, K3b, 2026-10-14, con un programa fuera del repositorio en el
    /// equipo de escritorio x64, .NET 10 Release): el mismo tablero base que HoA y Br2 (<c>InverseWalk</c> con K = 10 en 13 cuias
    /// de 4 y E = 2) y la Flor sobre una cuia con contenido y no sellada, elegida con el PRNG del núcleo
    /// (<c>Prng.SubSeed(semilla, 4031)</c>), con dos llaves distintas que no están dentro (b.3, como la Liana con fruta; con
    /// capacidades uniformes las dos pueden sellar fuera). Verificación con <see cref="LevelValidation"/>, A* (w = 1, 300.000 nodos)
    /// y Morpho (w = 2, 40.000). De 30 semillas: 30 válidos, 30 resueltos y 30 con Morpho; expandidos, mediana 1.223 y máximo
    /// 31.719; percentil 75 en la semilla 15 (par 18, la Flor de 6 y 9 sobre la cuia 0).</para>
    /// </remarks>
    internal static class ReferenceBoards
    {
        /// <summary>Motivo esperado de <see cref="LevelValidation"/> para la Liana con capacidades mixtas (bloqueo de C1-9).</summary>
        public const string LianaMixedBlocked = "bloqueo provisional";

        private const int Mud = Cuia.MudColor;

        private static readonly int[] E = new int[0];

        /// <summary>
        /// Los 40 tableros: 11 de una mecánica o capacidad, las 25 combinaciones del calendario d.4, la Tinaja (M13, reto con
        /// 5 dosis) y las variantes de la 1.0 de K3a y K3b: la Hoja atada (HoA), el Brote de dos hojas (Br2) y la Flor de
        /// vitória-régia (Fl).
        /// </summary>
        public static readonly ReferenceBoard[] All =
        {
            Board("Ch", 18, 1105,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 2),
                Doses(D(1, 5, 5, 6), D(2, 2, 10), D(3, 3, 3, 8), D(4, 4, 4, 9), D(5, 6, 10, 2), D(6, 3, 4, 8), D(7, 7, 7, 6), D(8, 8, 2, 10), D(9, 9, 9, 5), E, E, D(1, 1, 1, 7), D(10)),
                null),
            Board("Al", 22, 7365,
                Caps(5, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 9, 9, 2), E, D(3, 6, 6, 2), D(4, 4, 4, 7), D(5, 5, 5, 7), D(8, 8, 3, 5), E, D(8, 8, 4, 2), D(9, 3, 7, 6), D(10, 10, 10, 2), D(10, 1, 9, 7), D(1, 1, 6, 3)),
                null),
            Board("Ho", 23, 3694,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 10), D(2, 2, 7, 8), D(3, 3, 3, 8), D(4, 4, 8, 9), D(5, 2, 8, 9), D(6, 6, 4, 9), D(7, 7, 7, 10), D(1, 9, 6, 10), E, E, D(4, 3, 5, 2), D(5, 5, 6, 10)),
                Attrs(At(9, MechanicType.Hoja, key: 8))),
            Board("Ma", 23, 31188,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 7, 7, 3), D(2, 5, 1, 7), D(3, 3, 4, 9), E, D(5, 8, 8, 4), D(6, 6, 6, 5), D(7, 1, 10, 1), D(8, 8, 3, 9), E, D(10, 4, 9, 9), D(5, 10, 10, 6), D(2, 2, 2, 4)),
                Attrs(At(3, MechanicType.Matapi))),
            Board("Li", 21, 2975,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 2), D(2, 3, 4, 8), E, E, D(5, 5, 5, 4), D(6, 6, 10, 9), D(7, 7, 7, 5), D(8, 8, 2, 3), D(9, 2, 4, 9), D(10, 10, 9, 10), D(7, 6, 6, 1), D(3, 3, 8, 4)),
                Attrs(At(6, MechanicType.Liana, key: 10))),
            Board("Tu", 18, 5605,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 3), D(2, 2, 1, 9), D(3, 3, 9), D(4, 4, 4, 9), D(5, 5, 3, 5), D(6, 6, 5), D(7, 7, 10, 9), D(8, 8, 8, 2), E, D(10, 10, 10, 6), D(4, 7, 7, 6), D(8, 2, 1), E),
                null,
                Hidden(H(11, 1), H(11, 0))),
            Board("Re", 20, 67439,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 3, 3, 10), D(2, 2, 5, 5), D(3, 5, 2, 2), D(4, 4, 4, 5), E, D(6, 6, 3, 7), D(7, 7, 7, 10), D(8, 8, 1, 10), D(9, 8, 1, 4), E, D(1, 6, 6, 8), D(9, 9, 9, 10)),
                Attrs(At(7, MechanicType.Remolino, partner: 9), At(9, MechanicType.Remolino, partner: 7))),
            Board("Ba", 19, 4910,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 6, 9), D(2, Mud), D(3, 2, 3, 9), D(4, 4, 8), D(5, 5, 5, 10), D(6, 6, 6, 4), D(7, 5, 1, 2), D(8, 8, 9, 2), D(9, 3, 8, 3), D(10, 10, 10, 4), D(7, 7, 7, 1), E, E),
                null),
            Board("Ll", 21, 56095,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 8, 8), D(6, 3, 4, 2), E, E, D(5, 5, 5, 4), D(6, 6, 6, 5), D(7, 7, 7, 1), D(8, 8, 2, 4), D(9, 9, 9, 3), D(10, 9, 4, 3), D(10, 7, 2, 3), D(1, 1, 2)),
                Attrs(At(3, MechanicType.Lluvia, key: 10, n: 2))),
            Board("Br", 18, 1753,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 8), D(2, 2, 2, 5), D(3, 3, 4, 3), D(4, 9, 9, 5), D(5, 4, 10), D(6, 2, 5, 1), D(7, 7, 7, 8), D(8, 8, 10), D(9, 9, 3, 4), D(10, 10, 1), D(6, 6, 6, 7), E, E),
                Attrs(At(9, MechanicType.Brote))),
            Board("Pi", 22, 126829,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 2),
                Doses(D(1, 5, 5, 10), D(2, 3, 3, 7), D(9, 6, 2, 6), D(4, 4, 4, 7), D(5, 5, 2, 6), E, D(7, 9, 9, 6), D(8, 8, 8, 3), E, D(10, 10, 10, 7), D(8, 1, 1, 9), D(4, 2, 1, 3), E),
                Attrs(At(12, MechanicType.Pirana))),
            Board("Ho+Ch", 19, 1629,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 2),
                Doses(D(1, 6, 4, 2), D(2, 1, 9, 8), D(3, 3, 3, 10), D(4, 4, 9, 1), D(5, 2, 1), D(6, 6, 6, 4), D(7, 5, 5, 3), D(8, 8, 8, 9), E, D(10, 10, 10, 2), E, D(7, 7, 7, 5), D(9)),
                Attrs(At(8, MechanicType.Hoja, key: 10))),
            Board("Ma+Ho", 26, 90687,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 5, 3), E, D(3, 3, 2, 6), D(4, 10, 2, 9), E, D(6, 1, 5, 9), D(7, 7, 10, 3), D(8, 5, 9, 5), E, D(10, 6, 6, 2), D(8, 8, 8, 4), D(10, 2, 4, 9), D(7, 7, 4, 1)),
                Attrs(At(1, MechanicType.Hoja, key: 10), At(4, MechanicType.Matapi))),
            Board("Li+Ho", 21, 2861,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 6), D(2, 2, 2, 5), D(3, 3, 3, 8), D(4, 4, 4, 7), D(5, 5, 6, 1), E, D(7, 9, 7, 9), D(8, 8, 8, 6), E, D(10, 9, 6, 9), D(4, 3, 5, 7), D(10, 10, 10, 2)),
                Attrs(At(1, MechanicType.Liana, key: 1), At(5, MechanicType.Hoja, key: 8))),
            Board("Ho+Tu", 16, 1257,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 3), D(2, 2, 2, 8), D(3, 3, 4, 6), D(4, 6, 4), D(5, 5, 5, 7), D(6), D(7, 7, 3, 6), D(8, 8, 8, 10), D(9, 9, 9, 5), D(10, 10, 10, 1), E, E, D(9, 7, 2, 4)),
                Attrs(At(11, MechanicType.Hoja, key: 7)),
                Hidden(H(3, 1), H(3, 0))),
            Board("Re+Li", 20, 61355,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(5, 5, 7, 7), D(2, 2, 2, 1), D(3, 7, 3, 8), E, D(5, 5, 3, 9), D(6, 6, 6, 4), D(7, 8, 4, 4), E, D(9, 4, 8, 8), D(10, 3, 9, 6), D(10, 10, 10, 9), D(1, 1, 1, 2)),
                Attrs(At(6, MechanicType.Remolino, partner: 10), At(9, MechanicType.Liana, key: 4), At(10, MechanicType.Remolino, partner: 6))),
            Board("Ba+Ma", 18, 10598,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, Mud, 1, 7), D(2, 2, 1, 7), D(3, 3, 3, 8), D(4, 4, 4, 2), D(5, 5, 6, 2), D(6, 9, 9, 1), E, D(8, 8, 8, 5), D(9, 9, 3, 7), D(10, 10, 10, 5), E, D(10, 6, 4, 6), D(7)),
                Attrs(At(6, MechanicType.Matapi))),
            Board("Ll+Ho", 19, 186346,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(E, E, E, D(4, 4, 4, 8), D(5, 5, 5, 6), D(6, 6, 5, 8), D(7, 1, 2, 2), D(8, 3, 3, 3), D(9, 9, 4, 1), D(10, 8, 2, 2), D(9, 9, 6, 3), D(10, 1), D(7, 7, 7, 1)),
                Attrs(At(1, MechanicType.Lluvia, key: 10, n: 2), At(2, MechanicType.Hoja, key: 2))),
            Board("Br+Ba", 17, 11149,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 6), D(2, 2, 4, 8), D(3, 2, 2, 6), D(4, 4, 7, 6), D(5, 5, 5, 10), D(6), D(7, Mud, 7, 4), D(8, 8, 8, 10), D(9, 9, 9, 5), D(10, 10, 7, 1), E, D(3, 3, 3, 9), E),
                Attrs(At(5, MechanicType.Brote))),
            Board("Pi+Ma", 18, 66472,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 2),
                Doses(E, D(2, 2, 2, 5), D(5, 5, 10, 1), D(4, 4, 8, 6), D(5, 3, 3, 1), D(6, 6, 6, 2), D(7, 7, 7, 8), D(8, 8, 1, 1), D(9, 9, 9, 3), E, D(4, 4, 7, 10), D(9, 3, 10, 10), E),
                Attrs(At(0, MechanicType.Matapi), At(12, MechanicType.Pirana))),
            Board("Ma+Tu", 19, 8856,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(E, D(2, 2, 7, 6), D(3, 3, 1, 9), D(4, 9), D(5, 5, 5, 1), D(6, 6, 1), D(7, 7, 7, 6), D(8, 8, 3, 5), D(9, 1, 4), D(10, 10, 10, 2), D(3, 4, 4, 9), E, D(8, 8, 10, 2)),
                Attrs(At(0, MechanicType.Matapi)),
                Hidden(H(8, 1), H(8, 0), H(3, 0))),
            Board("Ba+Tu", 17, 4622,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 5), D(2, 5, Mud), D(3, 3, 3, 5), D(4, 4, 4, 5), E, D(6, 6, 6, 2), D(7, 8, 1), D(8, 8, 8, 4), D(9, 9, 6, 1), D(10, 7, 7, 2), D(10, 10, 10, 3), E, D(9, 9, 2, 7)),
                null,
                Hidden(H(0, 1), H(6, 1), H(6, 0))),
            Board("Br+Tu", 19, 20841,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 2), D(2, 4, 4, 3), D(3, 3, 3, 7), D(4, 10, 5, 9), E, D(6, 6, 6, 1), D(7, 7, 7, 5), D(8, 2, 2, 8), D(9), D(10, 10, 5, 10), E, D(6, 8, 8, 4), D(9, 5, 9)),
                Attrs(At(8, MechanicType.Brote)),
                Hidden(H(12, 1), H(12, 0))),
            Board("Ll+Re", 20, 169921,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 4), E, D(3, 3, 7, 2), E, D(5, 5, 5, 2), D(6, 6, 1), D(7, 7, 7, 2), D(8, 8, 3, 4), D(9, 9, 4, 2), D(10, 10, 3), D(6, 5, 8, 4), D(9, 9, 6, 8)),
                Attrs(At(1, MechanicType.Lluvia, key: 10, n: 2), At(6, MechanicType.Remolino, partner: 9), At(9, MechanicType.Remolino, partner: 6))),
            Board("Ll+Ba", 17, 23869,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 8), D(2, 2, 8, 4), D(3, 3, 4), E, D(5, 1, 7, 5), D(6, 6, 6, 3), D(7, 7, 6, 7), D(8, 8, 5, 4), D(9, 9, 9, 5), D(10, 10, 3), D(2, 2, 9), E, D(4, Mud)),
                Attrs(At(3, MechanicType.Lluvia, key: 10, n: 2))),
            Board("Br+Ma", 14, 1976,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 5), D(2, 2, 10, 9), D(3, 3, 3, 6), D(4, 4, 4, 7), D(5, 5, 5, 9), D(6, 6, 6, 7), D(7, 7, 9), D(8, 8, 8, 3), E, D(10, 10, 8, 10), D(9), E, D(2, 2, 4, 1)),
                Attrs(At(6, MechanicType.Brote), At(11, MechanicType.Matapi))),
            Board("Pi+Li", 23, 35971,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 7, 6), D(2, 2, 2, 10), D(3, 4, 9, 5), D(4, 9, 3, 2), E, E, D(7, 4, 4, 6), D(8, 8, 8, 10), D(9, 9, 5, 10), D(7, 6, 6, 5), D(3, 3, 7, 5), D(8, 10, 1, 1)),
                Attrs(At(3, MechanicType.Liana, key: 5), At(5, MechanicType.Pirana))),
            Board("Re+Ma", 22, 111046,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 7, 10), D(2, 2, 3, 9), D(9, 2, 2, 3), D(4, 4, 4, 9), D(5, 3, 1, 8), D(6, 6, 8, 7), E, D(8, 8, 10, 1), D(10, 10, 4, 7), E, D(6, 6, 9, 7), D(5, 5, 5, 3)),
                Attrs(At(1, MechanicType.Remolino, partner: 2), At(2, MechanicType.Remolino, partner: 1), At(6, MechanicType.Matapi))),
            Board("Re+Ho", 21, 64796,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(E, E, D(3, 1, 2, 2), D(4, 4, 4, 1), D(5, 10, 8, 4), D(6, 5, 5, 3), D(7, 7, 7, 3), D(8, 8, 8, 2), D(9, 9, 1, 2), D(10, 10, 10, 7), D(9, 3, 5, 1), D(6, 6, 6, 9)),
                Attrs(At(0, MechanicType.Hoja, key: 6), At(2, MechanicType.Remolino, partner: 4), At(4, MechanicType.Remolino, partner: 2))),
            Board("Ba+Li", 20, 3820,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 4, 10), D(2, 2, 2, 4), D(3, 3, 9, 6), D(4, 10, 7, 8), D(5, 5, 5, 1), D(6, 6, 6, 2), D(7, Mud, 10, 8), E, D(9, 9, 9, 1), D(8), D(7, 7, 10, 8), D(5, 3, 3, 4), E),
                Attrs(At(9, MechanicType.Liana, key: 9))),
            Board("Br+Ho", 15, 1385,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 7), D(2, 10, 2, 1), D(3, 3, 3, 8), D(4, 4), D(5, 2, 4), D(6, 6, 6, 7), D(7, 7, 4), D(8, 8, 8, 10), D(9, 9, 9, 3), D(10, 10, 6, 2), E, E, D(5, 5, 5, 9)),
                Attrs(At(3, MechanicType.Brote), At(11, MechanicType.Hoja, key: 7))),
            Board("Ba+Re", 17, 31897,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 2, 7), D(2, 2, 4, 2), D(3, 3, 8), D(4, 4, 4, 6), D(5, 5, 5, 3), D(6, 6, 8, 1), D(7, 7, 7, 6), D(8, Mud), D(9, 9, 9, 3), D(10, 9, 1, 8), D(10, 10, 10, 5), E, E),
                Attrs(At(9, MechanicType.Remolino, partner: 12), At(12, MechanicType.Remolino, partner: 9))),
            Board("Li+Ma", 23, 8903,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 5, 1), D(2, 3, 3, 1), E, D(4, 4, 6, 10), E, D(6, 6, 6, 9), D(7, 5, 3, 5), D(8, 8, 7, 2), D(9, 9, 7, 10), D(10, 2, 2, 3), D(9, 8, 8, 7), D(4, 4, 10, 5)),
                Attrs(At(2, MechanicType.Matapi), At(8, MechanicType.Liana, key: 8))),
            Board("Ll+Li+Al6", 22, 36553, LianaMixedBlocked,
                Caps(6, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 1, 1, 8), D(2, 2, 9, 7), D(9, 1, 8, 7), D(4, 4, 4, 3), D(5, 5, 6, 7), D(6, 3, 3, 7), E, E, D(9, 9, 6), D(10, 8, 4), D(10, 5, 5, 3), D(2, 2, 6, 8)),
                Attrs(At(1, MechanicType.Liana, key: 3), At(6, MechanicType.Lluvia, key: 10, n: 2))),
            Board("Pi+Ba", 18, 8678,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 2),
                Doses(D(1, 1, 5, 4), D(2, 2, 2, 8), D(3, Mud, 3), D(4, 4, 4, 10), D(5, 5, 5, 9), D(6, 7, 8, 6), D(7, 7, 7, 1), D(8, 8, 2, 10), D(9, 9, 9, 6), D(10, 3), E, D(1, 6, 3, 10), E, E),
                Attrs(At(13, MechanicType.Pirana))),
            Board("Ma+Li", 22, 14292,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 5), D(2, 2, 2, 3), D(3, 1, 4, 7), D(4, 4, 4, 10), D(5, 5, 8, 9), D(6, 6, 8, 7), E, D(8, 7, 9, 7), E, D(10, 10, 10, 9), D(2, 3, 8, 9), D(6, 6, 3, 5)),
                Attrs(At(0, MechanicType.Liana, key: 3), At(8, MechanicType.Matapi))),
            Board("Ti", 23, 7946,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 2),
                Doses(E, D(7, 3, 1, 3), D(9, 9, 7, 4), D(4, 4), D(5, 5, 3, 7), D(6, 6, 6), D(2, 2, 2, 8), D(8, 8, 8), D(9, 9, 7), D(5, 6, 3, 1), D(5, 1, 2, 1, 4), E),
                Attrs(At(10, MechanicType.Tinaja), At(11, MechanicType.Cucharon)),
                Hidden(H(10, 3), H(10, 2), H(10, 1), H(10, 0))),
            Board("HoA", 17, 1965,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 8, 9), D(2, 2, 10, 2), D(3, 3, 9), D(4, 4, 4, 7), D(5, 5, 5, 3), D(6, 6, 2, 1), D(7), D(8, 8, 3, 4), D(9, 7, 7, 8), D(10, 10, 10, 5), D(6, 6, 1, 9), E, E),
                Attrs(At(4, MechanicType.HojaAtada, key: 5))),
            Board("Br2", 18, 2068,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 1, 1, 9), D(2, 2, 2, 10), D(3, 3, 3, 9), D(4, 4, 9, 10), D(5, 6), E, D(7, 7, 1), D(8, 4, 4, 6), D(9, 5, 7, 6), D(10, 10, 6), D(5, 5, 2, 7), E, D(8, 8, 8, 3)),
                Attrs(At(4, MechanicType.Brote, n: 2))),
            Board("Fl", 18, 3591,
                Caps(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4),
                Doses(D(1, 4, 2, 1), D(2, 2, 2, 5), D(3, 3, 4, 3), D(4, 8, 3), D(5, 5, 5, 10), D(6, 6, 6, 9), D(7, 7, 6, 7), D(1), D(9, 9, 9, 8), D(10, 10, 10, 1), E, D(7, 8, 8, 4), E),
                Attrs(At(0, MechanicType.Flor, key: 6, key2: 9))),
        };

        /// <summary>Tablero <paramref name="name"/> (por ejemplo <c>"Ll+Re"</c>).</summary>
        public static ReferenceBoard Get(string name)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Name == name)
                {
                    return All[i];
                }
            }

            throw new ArgumentException("No hay tablero de referencia " + name, nameof(name));
        }

        /// <summary>Nombres de todos los tableros, en orden, para <c>TestCaseSource</c>.</summary>
        public static string[] Names()
        {
            var names = new string[All.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = All[i].Name;
            }

            return names;
        }

        private static ReferenceBoard Board(string name, int par, int expanded, int[] caps, int[][] doses, At[]? attrs, int[][]? hidden = null)
        {
            return Board(name, par, expanded, null, caps, doses, attrs, hidden);
        }

        private static ReferenceBoard Board(string name, int par, int expanded, string? expectedValidation, int[] caps, int[][] doses, At[]? attrs, int[][]? hidden = null)
        {
            MechanicSpec[]? specs = null;
            if (attrs != null)
            {
                specs = new MechanicSpec[caps.Length];
                for (int i = 0; i < attrs.Length; i++)
                {
                    specs[attrs[i].Index] = attrs[i].Spec;
                }
            }

            BoardBuildResult built = BoardBuilder.Build(caps, doses, specs, hidden);
            if (!built.IsValid)
            {
                throw new InvalidOperationException("Tablero de referencia " + name + " inválido: " + built.Error);
            }

            return new ReferenceBoard(name, par, expanded, expectedValidation, built.State!);
        }

        private static int[] Caps(params int[] caps)
        {
            return caps;
        }

        private static int[][] Doses(params int[][] doses)
        {
            return doses;
        }

        private static int[] D(params int[] doses)
        {
            return doses;
        }

        private static At[] Attrs(params At[] attrs)
        {
            return attrs;
        }

        private static At At(int index, MechanicType type, int key = 0, int partner = -1, int n = 0, int k = 0, int key2 = 0)
        {
            return new At(index, new MechanicSpec(type, key, partner, n, k, key2));
        }

        private static int[][] Hidden(params int[][] positions)
        {
            return positions;
        }

        private static int[] H(int cuia, int layer)
        {
            return new[] { cuia, layer };
        }
    }

    /// <summary>Un tablero de referencia: nombre, par y estados expandidos al generarlo, y el tablero.</summary>
    internal sealed class ReferenceBoard
    {
        public ReferenceBoard(string name, int par, int expanded, string? expectedValidation, BoardState state)
        {
            Name = name;
            Par = par;
            Expanded = expanded;
            ExpectedValidation = expectedValidation;
            State = state;
        }

        /// <summary>
        /// Combinación del calendario d.4 (Ho Hoja, Ma Matapi, Li Liana con fruta, Tu Aguas turbias, Re Remolino, Ba Barro, Ll
        /// Lluvia, Br Brote, Pi Piraña, Ch Cuia chica, Al y Al6 Cuia alta), Ti, la Tinaja con su cuia (M13), o una variante de la
        /// 1.0: HoA, la Hoja atada, y Br2, el Brote de dos hojas (K3a); Fl, la Flor de vitória-régia (K3b).
        /// </summary>
        public string Name { get; }

        /// <summary>Par (A*, w = 1, <c>run</c>; sin poda de fútiles si lleva Tinaja, b.2 R14).</summary>
        public int Par { get; }

        /// <summary>Estados expandidos por A* (w = 1, <c>run</c>; sin poda si lleva Tinaja) al generarlo.</summary>
        public int Expanded { get; }

        /// <summary>Tope que fijan los tests: <see cref="Expanded"/> + 25 % (+ 1.000), nunca más de 300.000 (03 §5).</summary>
        public int ExpandedLimit => Math.Min(300000, Expanded + (Expanded / 4) + 1000);

        /// <summary>Texto que debe contener el rechazo de <see cref="LevelValidation"/>, o null si el tablero es válido.</summary>
        public string? ExpectedValidation { get; }

        /// <summary>Tablero inicial.</summary>
        public BoardState State { get; }

        /// <summary>El nombre, para los mensajes de NUnit.</summary>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>Mecánica de una cuia en la tabla de tableros de referencia.</summary>
    internal readonly struct At
    {
        public At(int index, MechanicSpec spec)
        {
            Index = index;
            Spec = spec;
        }

        public int Index { get; }

        public MechanicSpec Spec { get; }
    }
}
