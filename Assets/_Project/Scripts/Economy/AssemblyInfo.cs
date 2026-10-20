using System.Runtime.CompilerServices;

// Los tests EditMode prueban piezas internas de AF.Economy sin hacerlas públicas: p. ej. Transaction.DoubleHarvestRiver
// (parche S4-wallet, E35), que solo pide la Crecida desde dentro del ensamblado.
[assembly: InternalsVisibleTo("AF.Tests.EditMode")]
