#nullable enable
using System;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Save;

namespace AF.GameFlow
{
    /// <summary>
    /// <c>album.fruits</c> (08 §4.3; 06 §4.5: cada tinte «se registra como descubierto para que el Cuaderno de campo de 1.1 se llene
    /// de forma retroactiva») y <c>af_fruit_discovered {tint_id}</c> (08 §8.2: «primer nivel en que aparece un tinte»; tarea B14):
    /// al cargar un nivel de campaña, cada tinte de su tablero inicial (1-14, glosario §9) que aún no estaba en el álbum se apunta y
    /// se devuelve como descubierto. Guarda con el <see cref="IProfileStore"/> que recibe (en el juego, el lote del perfil: la
    /// escritura sale con la de la carga). Un fallo de escritura no lanza (el álbum queda en el perfil en memoria). Sin motor.
    /// </summary>
    public sealed class FruitAlbum
    {
        /// <summary>
        /// Id de cada tinte por su número de motor (glosario §9; índice 0 vacío): el <c>tint_id</c> de <c>af_fruit_discovered</c>.
        /// </summary>
        public static readonly string[] TintIds =
        {
            string.Empty, "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo",
            "agua_tapajos", "lulo", "araza", "cacao", "agua_negro",
        };

        private readonly Profile _profile;
        private readonly IProfileStore _store;

        /// <summary>Crea el álbum sobre <paramref name="profile"/>, que se guarda en <paramref name="store"/>.</summary>
        public FruitAlbum(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>Los tintes descubiertos (<c>album.fruits</c>), en orden.</summary>
        public IReadOnlyList<int> Fruits => _profile.Album.Fruits;

        /// <summary>El último guardado falló: el álbum está en el perfil en memoria y sale con el siguiente.</summary>
        public bool PendingSave { get; private set; }

        /// <summary>
        /// Apunta los tintes de <paramref name="board"/> que aún no estaban (en orden de número de motor) y los añade a
        /// <paramref name="discovered"/> (que no vacía); guarda si hubo alguno. Devuelve cuántos.
        /// </summary>
        public int Discover(BoardState board, List<int> discovered)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (discovered == null)
            {
                throw new ArgumentNullException(nameof(discovered));
            }

            int added = 0;
            for (int tint = AlbumData.MinTint; tint <= AlbumData.MaxTint; tint++)
            {
                if (board.UnitsOf(tint) > 0 && _profile.Album.AddFruit(tint))
                {
                    discovered.Add(tint);
                    added++;
                }
            }

            if (added > 0)
            {
                PendingSave = !_store.TrySave(_profile);
            }

            return added;
        }

        /// <summary>Id del tinte <paramref name="tint"/> (<see cref="TintIds"/>), o «» si no es un tinte.</summary>
        public static string TintId(int tint)
        {
            return AlbumData.IsTint(tint) ? TintIds[tint] : string.Empty;
        }
    }
}
