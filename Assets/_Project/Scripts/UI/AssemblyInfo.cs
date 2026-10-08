using System.Runtime.CompilerServices;

// Los tests de la pantalla de nivel y del embudo (B2-1 a B2-3) leen los paneles internos (bandeja, modales) sin hacerlos públicos.
[assembly: InternalsVisibleTo("AF.Tests.PlayMode")]
[assembly: InternalsVisibleTo("AF.Tests.EditMode")]
