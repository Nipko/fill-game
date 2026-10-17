#nullable enable
using System;
using UnityEngine;

namespace AF.Audio.Unity
{
    /// <summary>
    /// Registro de los clips grabados de la entrega 1 (tarea AU3): id de la especificación → <see cref="AudioClip"/>, en el orden
    /// de las ranuras de <see cref="AudioClipIds"/>. Lo genera <c>AF.EditorTools.AudioAssetSetup.Apply</c> (nunca a mano) en
    /// <see cref="AssetPath"/> y la escena lo cuelga de <c>GameBootstrap</c> (<c>GameSceneBuilder</c>), sin <c>Resources</c>.
    /// El servicio de audio (<see cref="UnityAudioService.UseClips"/>) toma de aquí cada clip y, si una ranura no tiene, suena
    /// con el audio por código de respaldo. Las búsquedas son por número de ranura: sin cadenas ni asignaciones al sonar.
    /// </summary>
    public sealed class AudioClipRegistry : ScriptableObject
    {
        /// <summary>Ruta del registro en el proyecto (fuera de cualquier carpeta <c>Resources</c>).</summary>
        public const string AssetPath = "Assets/_Project/Audio/AudioClipRegistry.asset";

        [SerializeField]
        private string[] ids = Array.Empty<string>();

        [SerializeField]
        private AudioClip[] clips = Array.Empty<AudioClip>();

        [NonSerialized]
        private AudioClip?[]? _bySlot;

        /// <summary>Ids guardados, en el orden del registro (editor y tests).</summary>
        public string[] Ids => ids;

        /// <summary>Clips guardados, en el orden de <see cref="Ids"/> (editor y tests).</summary>
        public AudioClip[] Clips => clips;

        /// <summary>Ranuras con clip.</summary>
        public int Count
        {
            get
            {
                AudioClip?[] table = Table();
                int n = 0;
                for (int i = 0; i < table.Length; i++)
                {
                    if (table[i] != null)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>
        /// Crea un registro en memoria (tests: un registro incompleto o con clips de prueba): cada id de
        /// <paramref name="idList"/> (de la especificación) con el clip de la misma posición de <paramref name="clipList"/>.
        /// </summary>
        public static AudioClipRegistry Create(string[] idList, AudioClip[] clipList)
        {
            AudioClipRegistry registry = CreateInstance<AudioClipRegistry>();
            registry.Assign(idList, clipList);
            return registry;
        }

        /// <summary>
        /// Sustituye el contenido por los ids de <paramref name="idList"/> con los clips de <paramref name="clipList"/> (lo usa el
        /// método de Editor que genera el registro, y los tests).
        /// </summary>
        public void Assign(string[] idList, AudioClip[] clipList)
        {
            if (idList == null || clipList == null || idList.Length != clipList.Length)
            {
                throw new ArgumentException("Hacen falta tantos ids como clips.");
            }

            ids = (string[])idList.Clone();
            clips = (AudioClip[])clipList.Clone();
            _bySlot = null;
        }

        /// <summary>El clip de la ranura <paramref name="slot"/> (<see cref="AudioClipIds"/>), o null si no tiene.</summary>
        public AudioClip? Get(int slot)
        {
            AudioClip?[] table = Table();
            if ((uint)slot >= (uint)table.Length)
            {
                return null;
            }

            AudioClip? clip = table[slot];
            return clip != null ? clip : null; // un clip destruido cuenta como ninguno
        }

        /// <summary>La ranura tiene clip.</summary>
        public bool Has(int slot)
        {
            return Get(slot) != null;
        }

        private void OnEnable()
        {
            _bySlot = null;
        }

        private void OnValidate()
        {
            _bySlot = null;
        }

        // Ranura → clip, calculada una vez (al primer uso tras cargar o cambiar el registro). Un id desconocido se ignora.
        private AudioClip?[] Table()
        {
            if (_bySlot != null)
            {
                return _bySlot;
            }

            var table = new AudioClip?[AudioClipIds.Count];
            int n = Math.Min(ids.Length, clips.Length);
            for (int i = 0; i < n; i++)
            {
                int slot = AudioClipIds.SlotOf(ids[i]);
                if (slot >= 0 && clips[i] != null)
                {
                    table[slot] = clips[i];
                }
            }

            _bySlot = table;
            return table;
        }
    }
}
