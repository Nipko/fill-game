#nullable enable
using System;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Overlay de una mecánica sobre una cuia (07 §4.3, «Bloqueada por mecánica»; 02 §9.1; contrato de overlays de la
    /// arquitectura del paso 1, §5): el estado legible de la mecánica, dibujado siempre desde el <see cref="Cuia"/> del núcleo.
    /// La vista nunca decide reglas (02 §3.2): el overlay solo pinta lo que dice el estado, y nada cambia al seleccionar
    /// (D-002). El contenido de la cuia siempre se ve (Q17 A).
    /// </summary>
    /// <remarks>
    /// <para>Ciclo de vida: <c>CuiaView</c> lo crea con <see cref="CuiaOverlays.Create"/> la primera vez que muestra una cuia
    /// con esa mecánica y llama a <see cref="Attach"/>, que llama una sola vez a <see cref="OnAttach"/> para crear sus
    /// <c>Image</c> bajo las anclas del <see cref="CuiaOverlayHost"/>. Después le reenvía <see cref="Show"/> (desde
    /// <c>ShowContent</c>, salvo mientras <see cref="IsAnimating"/>), <see cref="Layout"/> (desde <c>SetGeometry</c>),
    /// <see cref="OnRejected"/> (desde <c>Shake</c>), <see cref="Tick"/> y <see cref="SetInFlight"/> (desde <c>MoveBodyTo</c>
    /// y <c>ReturnBody</c>).</para>
    /// <para>Pasos de mecánica: <see cref="StepDurationMs"/> da la duración nominal (sin factor de velocidad) y
    /// <see cref="PlayStep"/> arranca el paso y devuelve <c>StepDurationMs × ctx.SpeedFactor</c>; 0 = paso por defecto.
    /// <see cref="SnapStep"/> lo termina en el acto. Al acabar cada paso manda <c>ShowContent(ctx.After)</c>.</para>
    /// <para>Rendimiento (08 §5.4): nada de LINQ, closures ni <c>new</c> en <see cref="Tick"/>, en los pasos ni en
    /// <see cref="Show"/>; solo se escriben valores que cambian.</para>
    /// <para>Color (direccion-visual-v2 §2.4, regla 1; 07 §3, regla 6): <see cref="CollectSurfaceColors"/> devuelve el color
    /// principal de cada superficie de más de 2 dp, que debe quedar a ΔE00 ≥ 10 de los 14 tintes (lo comprueba
    /// <c>CuiaOverlayTests</c>). Quedan fuera las líneas de 2 dp o menos, los tonos <c>fibra_*</c> y lo que lleva un tinte a
    /// propósito (la fruta llave, el capullo del Brote, las gotas de la Lluvia).</para>
    /// </remarks>
    public abstract class CuiaOverlay
    {
        private CuiaOverlayHost? _host;

        /// <summary>Mecánica que dibuja.</summary>
        public abstract MechanicType Type { get; }

        /// <summary>Anfitrión: anclas, arte, efectos y tablero de la cuia. Null antes de <see cref="Attach"/>.</summary>
        public CuiaOverlayHost? AttachedHost => _host;

        /// <summary>El overlay está a la vista (su mecánica está en la cuia mostrada).</summary>
        public bool Visible { get; protected set; }

        /// <summary>Hay un paso de mecánica en curso: mientras, <c>ShowContent</c> no llama a <see cref="Show"/>.</summary>
        public virtual bool IsAnimating => false;

        /// <summary>Anfitrión, para las subclases (solo tras <see cref="Attach"/>).</summary>
        protected CuiaOverlayHost Host => _host ?? throw new InvalidOperationException("El overlay aún no está unido a una cuia (Attach).");

        /// <summary>Une el overlay a su cuia: guarda el anfitrión, crea las imágenes (<see cref="OnAttach"/>) y las coloca.</summary>
        /// <exception cref="InvalidOperationException">Ya estaba unido.</exception>
        public void Attach(CuiaOverlayHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (_host != null)
            {
                throw new InvalidOperationException("Un overlay se une a una sola cuia.");
            }

            _host = host;
            OnAttach();
            Layout(host.U, host.HeightU);
        }

        /// <summary>
        /// Muestra el estado de <paramref name="cuia"/>. Idempotente y desde el estado: con <c>cuia.Mechanic != Type</c> todo
        /// queda oculto (la mecánica se abrió o se soltó).
        /// </summary>
        public abstract void Show(Cuia cuia, bool sealedNow);

        /// <summary>Recoloca las piezas: <paramref name="u"/> en dp y el alto de la cuia en u.</summary>
        public virtual void Layout(float u, float heightU)
        {
        }

        /// <summary>El tablero se recolocó (también tras añadir una cuia): lo que dependa de otras cuias se recalcula.</summary>
        public virtual void OnBoardLayout()
        {
        }

        /// <summary>Avanza las animaciones propias (sin asignaciones).</summary>
        public virtual void Tick(float dtMs)
        {
        }

        /// <summary>La cuia se tocó con un vertido ilegal y se sacude (02 §4.1; R11): sin texto ni color de error.</summary>
        public virtual void OnRejected()
        {
        }

        /// <summary>El cuerpo de la cuia sale a verter (true) o vuelve a su ranura (false).</summary>
        public virtual void SetInFlight(bool inFlight)
        {
        }

        /// <summary>Duración nominal del paso <paramref name="kind"/> (ms, sin factor de velocidad); 0 = sin paso propio.</summary>
        public virtual float StepDurationMs(MechanicStepKind kind)
        {
            return 0f;
        }

        /// <summary>Arranca el paso descrito por <paramref name="ctx"/>; devuelve su duración real (ms), o 0 para el paso por defecto.</summary>
        public virtual float PlayStep(in MechanicStepContext ctx)
        {
            return 0f;
        }

        /// <summary>Termina en el acto el paso en curso, si lo hay.</summary>
        public virtual void SnapStep()
        {
        }

        /// <summary>Añade a <paramref name="into"/> el color principal de cada superficie de más de 2 dp (test de ΔE00).</summary>
        public virtual void CollectSurfaceColors(List<Color> into)
        {
        }

        /// <summary>Crea las imágenes del overlay bajo las anclas de <see cref="Host"/>. Se llama una sola vez.</summary>
        protected abstract void OnAttach();
    }
}
