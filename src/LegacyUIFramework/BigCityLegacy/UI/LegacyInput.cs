using System;
using System.Reflection;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Compatibility wrapper for Unity's Legacy Input API.
    ///
    /// Does not have a compile-time dependency on UnityEngine.Input,
    /// so it works with both:
    ///
    /// - old Unity versions where Input lives in UnityEngine.dll
    /// - newer Unity versions where Input lives in UnityEngine.InputLegacyModule.dll
    ///
    /// Call Init() once before use.
    /// </summary>
    public static class LegacyInput
    {
        private static bool _initialized;
        private static bool _available;

        private static Func<string, bool> _getKey;
        private static Func<string, bool> _getKeyDown;
        private static Func<string, bool> _getKeyUp;

        private static Func<int, bool> _getMouseButton;
        private static Func<int, bool> _getMouseButtonDown;
        private static Func<int, bool> _getMouseButtonUp;

        private static Func<string, bool> _getButton;
        private static Func<string, bool> _getButtonDown;
        private static Func<string, bool> _getButtonUp;

        private static Func<string, float> _getAxis;
        private static Func<string, float> _getAxisRaw;

        private static Func<bool> _getAnyKey;
        private static Func<bool> _getAnyKeyDown;
        private static Func<Vector3> _getMousePosition;

        /// <summary>
        /// True after Init() has been called.
        /// </summary>
        public static bool Initialized
        {
            get { return _initialized; }
        }

        /// <summary>
        /// True if UnityEngine.Input was found and at least the basic keyboard input API could be initialized.
        /// </summary>
        public static bool Available
        {
            get { return _available; }
        }

        /// <summary>
        /// Finds UnityEngine.Input and creates cached delegates.
        /// </summary>
        public static bool Init()
        {
            if (_initialized)
                return _available;

            _initialized = true;

            Type inputType = FindInputType();

            if (inputType == null)
            {
                _available = false;
                return false;
            }

            _getKey = CreateMethodDelegate<Func<string, bool>>(
                inputType,
                "GetKey",
                typeof(string));

            _getKeyDown = CreateMethodDelegate<Func<string, bool>>(
                inputType,
                "GetKeyDown",
                typeof(string));

            _getKeyUp = CreateMethodDelegate<Func<string, bool>>(
                inputType,
                "GetKeyUp",
                typeof(string));

            _getMouseButton = CreateMethodDelegate<Func<int, bool>>(
                inputType,
                "GetMouseButton",
                typeof(int));

            _getMouseButtonDown = CreateMethodDelegate<Func<int, bool>>(
                inputType,
                "GetMouseButtonDown",
                typeof(int));

            _getMouseButtonUp = CreateMethodDelegate<Func<int, bool>>(
                inputType,
                "GetMouseButtonUp",
                typeof(int));

            _getButton = CreateMethodDelegate<Func<string, bool>>(
                inputType,
                "GetButton",
                typeof(string));

            _getButtonDown = CreateMethodDelegate<Func<string, bool>>(
                inputType,
                "GetButtonDown",
                typeof(string));

            _getButtonUp = CreateMethodDelegate<Func<string, bool>>(
                inputType,
                "GetButtonUp",
                typeof(string));

            _getAxis = CreateMethodDelegate<Func<string, float>>(
                inputType,
                "GetAxis",
                typeof(string));

            _getAxisRaw = CreateMethodDelegate<Func<string, float>>(
                inputType,
                "GetAxisRaw",
                typeof(string));

            _getAnyKey = CreatePropertyGetter<bool>(
                inputType,
                "anyKey");

            _getAnyKeyDown = CreatePropertyGetter<bool>(
                inputType,
                "anyKeyDown");

            _getMousePosition = CreatePropertyGetter<Vector3>(
                inputType,
                "mousePosition");

            // GetKey(string) is a good indicator that the
            // Legacy Input API is actually usable.
            _available = _getKey != null;

            return _available;
        }

        // Keyboard
        public static bool GetKey(string key)
        {
            EnsureInitialized();
            return _getKey != null && _getKey(key.ToLower());
        }

        public static bool GetKeyDown(string key)
        {
            EnsureInitialized();
            return _getKeyDown != null && _getKeyDown(key.ToLower());
        }

        public static bool GetKeyUp(string key)
        {
            EnsureInitialized();
            return _getKeyUp != null && _getKeyUp(key.ToLower());
        }

        // Mouse

        public static bool GetMouseButton(int button)
        {
            EnsureInitialized();
            return _getMouseButton != null && _getMouseButton(button);
        }

        public static bool GetMouseButtonDown(int button)
        {
            EnsureInitialized();
            return _getMouseButtonDown != null && _getMouseButtonDown(button);
        }

        public static bool GetMouseButtonUp(int button)
        {
            EnsureInitialized();
            return _getMouseButtonUp != null && _getMouseButtonUp(button);
        }

        /// <summary>
        /// Current mouse position in screen coordinates.
        /// Accessed through reflection for the same cross-version compatibility as the other Legacy Input members.
        /// </summary>
        public static Vector3 MousePosition
        {
            get
            {
                EnsureInitialized();
                return _getMousePosition != null ? _getMousePosition() : Vector3.zero;
            }
        }

        // Input Manager
        public static bool GetButton(string buttonName)
        {
            EnsureInitialized();
            return _getButton != null && _getButton(buttonName);
        }

        public static bool GetButtonDown(string buttonName)
        {
            EnsureInitialized();
            return _getButtonDown != null && _getButtonDown(buttonName);
        }

        public static bool GetButtonUp(string buttonName)
        {
            EnsureInitialized();
            return _getButtonUp != null && _getButtonUp(buttonName);
        }

        public static float GetAxis(string axisName)
        {
            EnsureInitialized();
            return _getAxis != null ? _getAxis(axisName) : 0f;
        }

        public static float GetAxisRaw(string axisName)
        {
            EnsureInitialized();
            return _getAxisRaw != null ? _getAxisRaw(axisName) : 0f;
        }

        // Misc
        public static bool AnyKey
        {
            get
            {
                EnsureInitialized();
                return _getAnyKey != null && _getAnyKey();
            }
        }

        public static bool AnyKeyDown
        {
            get
            {
                EnsureInitialized();
                return _getAnyKeyDown != null && _getAnyKeyDown();
            }
        }

        // Init helpers
        private static void EnsureInitialized()
        {
            if (!_initialized)
                Init();
        }

        private static Type FindInputType()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type;

                try
                {
                    type = assemblies[i].GetType(
                        "UnityEngine.Input",
                        false);
                }
                catch
                {
                    continue;
                }

                if (type != null)
                    return type;
            }

            return null;
        }

        private static T CreateMethodDelegate<T>(
            Type type,
            string methodName,
            params Type[] parameterTypes)
            where T : class
        {
            try
            {
                MethodInfo method = type.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    parameterTypes,
                    null);

                if (method == null)
                    return null;

                return Delegate.CreateDelegate(
                    typeof(T),
                    method,
                    false) as T;
            }
            catch
            {
                return null;
            }
        }

        private static Func<T> CreatePropertyGetter<T>(
            Type type,
            string propertyName)
        {
            try
            {
                PropertyInfo property = type.GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Static);

                if (property == null)
                    return null;

                MethodInfo getter = property.GetGetMethod();

                if (getter == null)
                    return null;

                return Delegate.CreateDelegate(
                    typeof(Func<T>),
                    getter,
                    false) as Func<T>;
            }
            catch
            {
                return null;
            }
        }
    }
}