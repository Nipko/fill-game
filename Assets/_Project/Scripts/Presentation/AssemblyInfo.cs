using System.Runtime.CompilerServices;

// El test de las celdas del atlas de TiTino (SlothAtlasCellsTests) pinta cada pieza con los métodos internos de ProceduralArt
// y lee el lienzo (PixelCanvas) sin hacerlos públicos.
[assembly: InternalsVisibleTo("AF.Tests.EditMode")]
