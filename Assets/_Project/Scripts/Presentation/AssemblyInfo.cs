using System.Runtime.CompilerServices;

// El test de las celdas del atlas de TiTino (SlothAtlasCellsTests) pinta cada pieza con los métodos internos de ProceduralArt
// y lee el lienzo (PixelCanvas) sin hacerlos públicos.
[assembly: InternalsVisibleTo("AF.Tests.EditMode")]

// Los pliegues de la cinta del río (RiverRibbonGraphic.CountFoldedTriangles y FoldGuardOff; segunda ronda de la 1.1) se miden en
// PlayMode, con el Mapa real (MapRound2Tests).
[assembly: InternalsVisibleTo("AF.Tests.PlayMode")]
