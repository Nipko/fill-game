#nullable enable
using System.Reflection;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// <see cref="InputTestFixture"/> para los tests que cargan la escena (con su <see cref="InputSystemUIInputModule"/>) y
    /// pulsan teclas, como el Atrás de Android. Aísla del Input System real las acciones del módulo de UI.
    /// <para>
    /// El módulo de la escena usa el asset <c>DefaultInputActions</c> del paquete (o, creado por código sin acciones, el
    /// <c>DefaultInputActions</c> que el módulo guarda en un campo estático). Es un único objeto para toda la sesión de tests, y
    /// el primer test que carga la escena lo enlaza con los dispositivos del Input System real. <see cref="InputTestFixture"/>
    /// cambia el Input System por uno de prueba sin dispositivos, pero el asset conserva los controles ya resueltos, que son
    /// de dispositivos de otro gestor. En el Editor de Windows en batchmode no hay dispositivos reales y no se notaba; en la CI
    /// Linux (Xvfb y udev) sí: al activar el módulo salía <c>IndexOutOfRangeException</c> en <c>AddStateChangeMonitor</c>, y
    /// sin teclado de prueba, <c>ArgumentNullException: statePtr</c> en <c>OnBeforeInitialUpdate</c> (primera ejecución de
    /// ci.yml, 2026-10-27; se reproduce en el escritorio añadiendo un teclado, un ratón y una pantalla táctil al Input System
    /// real antes de cargar la escena).
    /// </para>
    /// <para>
    /// Aquí, antes de cambiar de Input System, se apaga la entrada de la escena que dejó cargada el test anterior (sus módulos
    /// de UI y la acción de Atrás de <c>GameBootstrap</c>), para que nada se desactive después contra el gestor equivocado.
    /// Con el Input System de prueba ya puesto, a cada asset de acciones cargado e inactivo (salvo las acciones de todo el
    /// proyecto, que ya trata <see cref="InputTestFixture"/>) se le avisa de que el sistema cambió, como hace el propio paquete
    /// con las acciones del proyecto (<c>InputActionAsset.OnSetupChanged</c>): suelta los controles resueltos y los vuelve a
    /// resolver en el acto con los dispositivos de prueba, con un estado nuevo que se entera de los dispositivos que añada el
    /// test. Al acabar se apaga otra vez la entrada de la escena y, ya con el Input System real restaurado, se repite para que
    /// el test siguiente los resuelva con los dispositivos reales. Lo que comprueban los tests no cambia: el Atrás llega por el
    /// mismo camino del juego (<c>GameBootstrap</c> y su acción <c>&lt;Keyboard&gt;/escape</c>).
    /// </para>
    /// </summary>
    public abstract class UiInputTestFixture : InputTestFixture
    {
        private static readonly MethodInfo? SetupChanged =
            typeof(InputActionAsset).GetMethod("OnSetupChanged", BindingFlags.Instance | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);

        /// <inheritdoc />
        public override void Setup()
        {
            DisableSceneInput(); // aún con el Input System real: la escena del test anterior sigue cargada
            base.Setup();
            ForgetResolvedControls();
        }

        /// <inheritdoc />
        public override void TearDown()
        {
            DisableSceneInput(); // aún con el Input System de prueba, con el que se activaron
            base.TearDown();
            ForgetResolvedControls();
        }

        /// <summary>Apaga los módulos de UI y <c>GameBootstrap</c> (con su acción de Atrás) de la escena cargada.</summary>
        private static void DisableSceneInput()
        {
            InputSystemUIInputModule[] modules = Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None);
            for (int i = 0; i < modules.Length; i++)
            {
                modules[i].enabled = false;
            }

            GameBootstrap[] boots = Object.FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None);
            for (int i = 0; i < boots.Length; i++)
            {
                boots[i].enabled = false;
            }
        }

        /// <summary>
        /// Los assets de acciones cargados e inactivos olvidan sus controles resueltos y los resuelven de nuevo, con los
        /// dispositivos del Input System actual.
        /// </summary>
        private static void ForgetResolvedControls()
        {
            Assert.That(SetupChanged, Is.Not.Null,
                "InputActionAsset.OnSetupChanged no existe en esta versión del Input System: revisa UiInputTestFixture");
            InputActionAsset projectWide = InputSystem.actions;
            InputActionAsset[] assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            for (int i = 0; i < assets.Length; i++)
            {
                InputActionAsset asset = assets[i];
                if (asset == null || ReferenceEquals(asset, projectWide) || asset.enabled)
                {
                    continue;
                }

                SetupChanged!.Invoke(asset, null);

                // Sin estado, la acción no puede ni decir si está activa (InputAction.currentState lo exige): se resuelve ya,
                // con un estado nuevo registrado en el Input System actual, y se deja apagada como estaba.
                asset.Enable();
                asset.Disable();
            }
        }
    }
}
