using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;
namespace Game
{
    public class Utils
    {
        public static System.Random random = new System.Random();
        // Mathf.Clamp only works for float and int. we need some more versions:
        public static long Clamp(long value, long min, long max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // is any of the keys UP?
        public static bool AnyKeyUp(KeyCode[] keys)
        {
            // avoid Linq.Any because it is HEAVY(!) on GC and performance
            foreach (KeyCode key in keys)
                if (Input.GetKeyUp(key))
                    return true;
            return false;
        }

        // is any of the keys DOWN?
        public static bool AnyKeyDown(KeyCode[] keys)
        {
            // avoid Linq.Any because it is HEAVY(!) on GC and performance
            foreach (KeyCode key in keys)
                if (Input.GetKeyDown(key))
                    return true;
            return false;
        }

        // is any of the keys PRESSED?
        public static bool AnyKeyPressed(KeyCode[] keys)
        {
            // avoid Linq.Any because it is HEAVY(!) on GC and performance
            foreach (KeyCode key in keys)
                if (Input.GetKey(key))
                    return true;
            return false;
        }

        // helper function to calculate a bounds radius in WORLD SPACE
        // -> collider.radius is local scale
        // -> collider.bounds is world scale
        // -> use x+y extends average just to be sure (for capsules, x==y extends)
        // -> use 'extends' instead of 'size' because extends are the radius.
        //    in other words: if we come from the right, we only want to stop at
        //    the radius aka half the size, not twice the radius aka size.
        public static float BoundsRadius(Bounds bounds) =>
            (bounds.extents.x + bounds.extents.z) / 2;

        // Distance between two ClosestPoints
        // this is needed in cases where entities are really big. in those cases,
        // we can't just move to entity.transform.position, because it will be
        // unreachable. instead we have to go the closest point on the boundary.
        //
        // Vector3.Distance(a.transform.position, b.transform.position):
        //    _____        _____
        //   |     |      |     |
        //   |  x==|======|==x  |
        //   |_____|      |_____|
        //
        //
        // Utils.ClosestDistance(a.collider, b.collider):
        //    _____        _____
        //   |     |      |     |
        //   |     |x====x|     |
        //   |_____|      |_____|
        //
        // IMPORTANT:
        //   we always pass Entity instead of Collider, because
        //   entity.transform.position is animation independent while
        //   collider.transform.position changes during animations (the hips)!
        public static float ClosestDistance(Entity a, Entity b)
        {
            // IMPORTANT: DO NOT use the collider itself. the position changes
            //            during animations, causing situations where attacks are
            //            interrupted because the target's hips moved a bit out of
            //            attack range, even though the target didn't actually move!
            //            => use transform.position and collider.radius instead!
            //
            //            this is probably faster than collider.ClosestPoints too

            // at first calculate the distance from A to B, subtract both radius
            // IMPORTANT: use entity.transform.position not
            //            collider.transform.position. that would still be the hip!
            float distance = Vector3.Distance(a.transform.position, b.transform.position);

            // calculate both collider radius
            float radiusA = BoundsRadius(a.collider.bounds);
            float radiusB = BoundsRadius(b.collider.bounds);

            // subtract both radius
            float distanceInside = distance - radiusA - radiusB;

            // return distance. if it's <0 because they are inside each other, then
            // return 0.
            return Mathf.Max(distanceInside, 0);
        }

        // closest point from an entity's collider to another point
        // this is used all over the place, so let's put it into one place so it's
        // easier to modify the method if needed
        public static Vector3 ClosestPoint(Entity entity, Vector3 point)
        {
            // IMPORTANT: DO NOT use the collider itself. the position changes
            //            during animations, causing situations where attacks are
            //            interrupted because the target's hips moved a bit out of
            //            attack range, even though the target didn't actually move!
            //            => use transform.position and collider.radius instead!
            //
            //            this is probably faster than collider.ClosestPoints too

            // first of all, get radius but in WORLD SPACE not in LOCAL SPACE.
            // otherwise parent scales are not applied.
            float radius = BoundsRadius(entity.collider.bounds);

            // now get the direction from point to entity
            // IMPORTANT: use entity.transform.position not
            //            collider.transform.position. that would still be the hip!
            Vector3 direction = entity.transform.position - point;
            //Debug.DrawLine(point, point + direction, Color.red, 1, false);

            // subtract radius from direction's length
            Vector3 directionSubtracted = Vector3.ClampMagnitude(direction, direction.magnitude - radius);

            // return the point
            //Debug.DrawLine(point, point + directionSubtracted, Color.green, 1, false);
            return point + directionSubtracted;
        }

        // raycast while ignoring self (by setting layer to "Ignore Raycasts" first)
        // => setting layer to IgnoreRaycasts before casting is the easiest way to do it
        // => raycast + !=this check would still cause hit.point to be on player
        // => raycastall is not sorted and child objects might have different layers etc.
        public static bool RaycastWithout(Ray ray, out RaycastHit hit, GameObject ignore)
        {
            // remember layers
            Dictionary<Transform, int> backups = new Dictionary<Transform, int>();

            // set all to ignore raycast
            foreach (Transform tf in ignore.GetComponentsInChildren<Transform>(true))
            {
                backups[tf] = tf.gameObject.layer;
                tf.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }

            // raycast
            bool result = Physics.Raycast(ray, out hit);

            // restore layers
            foreach (KeyValuePair<Transform, int> kvp in backups)
                kvp.Key.gameObject.layer = kvp.Value;

            return result;
        }

        // calculate encapsulating bounds of all child renderers
        public static Bounds CalculateBoundsForAllRenderers(GameObject go)
        {
            Bounds bounds = new Bounds();
            bool initialized = false;
            foreach (Renderer rend in go.GetComponentsInChildren<Renderer>())
            {
                // initialize or encapsulate
                if (!initialized)
                {
                    bounds = rend.bounds;
                    initialized = true;
                }
                else bounds.Encapsulate(rend.bounds);
            }
            return bounds;
        }

        // helper function to find the nearest Transform from a point 'from'
        public static Transform GetNearestTransform(List<Transform> transforms, Vector3 from)
        {
            // note: avoid Linq for performance / GC
            // => players can respawn frequently, and the game could have many start
            //    positions so this function does matter even if not in hot path.
            Transform nearest = null;
            foreach (Transform tf in transforms)
            {
                // better candidate if we have no candidate yet, or if closer
                if (nearest == null ||
                    Vector3.Distance(tf.position, from) < Vector3.Distance(nearest.position, from))
                    nearest = tf;
            }
            return nearest;
        }

        // pretty print seconds as hours:minutes:seconds(.milliseconds/100)s
        public static string PrettySeconds(float seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            string res = "";
            if (t.Days > 0) res += t.Days + "d";
            if (t.Hours > 0) res += " " + t.Hours + "h";
            if (t.Minutes > 0) res += " " + t.Minutes + "m";
            // 0.5s, 1.5s etc. if any milliseconds. 1s, 2s etc. if any seconds
            if (t.Milliseconds > 0) res += " " + t.Seconds + "." + (t.Milliseconds / 100) + "s";
            else if (t.Seconds > 0) res += " " + t.Seconds + "s";
            // if the string is still empty because the value was '0', then at least
            // return the seconds instead of returning an empty string
            return res != "" ? res : "0s";
        }

        // hard mouse scrolling that is consistent between all platforms
        //   Input.GetAxis("Mouse ScrollWheel") and
        //   Input.GetAxisRaw("Mouse ScrollWheel")
        //   both return values like 0.01 on standalone and 0.5 on WebGL, which
        //   causes too fast zooming on WebGL etc.
        // normally GetAxisRaw should return -1,0,1, but it doesn't for scrolling
        public static float GetAxisRawScrollUniversal()
        {
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (scroll < 0) return -1;
            if (scroll > 0) return  1;
            return 0;
        }

        // two finger pinch detection
        // source: https://docs.unity3d.com/Manual/PlatformDependentCompilation.html
        public static float GetPinch()
        {
            if (Input.touchCount == 2)
            {
                // Store both touches.
                Touch touchZero = Input.GetTouch(0);
                Touch touchOne = Input.GetTouch(1);

                // Find the position in the previous frame of each touch.
                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                // Find the magnitude of the vector (the distance) between the touches in each frame.
                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                // Find the difference in the distances between each frame.
                return touchDeltaMag - prevTouchDeltaMag;
            }
            return 0;
        }

        // universal zoom: mouse scroll if mouse, two finger pinching otherwise
        public static float GetZoomUniversal()
        {
            if (Input.mousePresent)
                return GetAxisRawScrollUniversal();
            else if (Input.touchSupported)
                return GetPinch();
            return 0;
        }

        // parse last upper cased noun from a string, e.g.
        //   EquipmentWeaponBow => Bow
        //   EquipmentShield => Shield
        static Regex lastNountRegEx = new Regex(@"([A-Z][a-z]*)"); // cache to avoid allocations. this is used a lot.
        public static string ParseLastNoun(string text)
        {
            MatchCollection matches = lastNountRegEx.Matches(text);
            return matches.Count > 0 ? matches[matches.Count-1].Value : "";
        }

        // check if the cursor is over a UI or OnGUI element right now
        // note: for UI, this only works if the UI's CanvasGroup blocks Raycasts
        // note: for OnGUI: hotControl is only set while clicking, not while zooming
        public static bool IsCursorOverUserInterface()
        {
            // IsPointerOverGameObject check for left mouse (default)
            if (EventSystem.current.IsPointerOverGameObject())
                return true;

            // IsPointerOverGameObject check for touches
            for (int i = 0; i < Input.touchCount; ++i)
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;

            // OnGUI check
            return GUIUtility.hotControl != 0;
        }

        // PBKDF2 hashing recommended by NIST:
        // http://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-132.pdf
        // salt should be at least 128 bits = 16 bytes
        public static string PBKDF2Hash(string text, string salt)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(text, saltBytes, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        // invoke multiple functions by prefix via reflection.
        // -> works for static classes too if object = null
        // -> cache it so it's fast enough for Update calls
        static Dictionary<KeyValuePair<Type,string>, MethodInfo[]> lookup = new Dictionary<KeyValuePair<Type,string>, MethodInfo[]>();
        public static MethodInfo[] GetMethodsByPrefix(Type type, string methodPrefix)
        {
            KeyValuePair<Type,string> key = new KeyValuePair<Type,string>(type, methodPrefix);
            if (!lookup.ContainsKey(key))
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                        .Where(m => m.Name.StartsWith(methodPrefix))
                                        .ToArray();
                lookup[key] = methods;
            }
            return lookup[key];
        }

        public static void InvokeMany(Type type, object onObject, string methodPrefix, params object[] args)
        {
            foreach (MethodInfo method in GetMethodsByPrefix(type, methodPrefix))
                method.Invoke(onObject, args);
        }
        public static string TimeAgo(TimeSpan tspan, int level = 0) {
            string how_long_ago = "now";
            if (level >= 2) return how_long_ago;
            if (tspan.Days > 1)
                how_long_ago = string.Format("{0} Day", tspan.Days);
            else if (tspan.Days == 1)
                how_long_ago = string.Format("1 Day {0}", TimeAgo(new TimeSpan(tspan.Hours, tspan.Minutes, tspan.Seconds), level + 1));
            else if (tspan.Hours >= 1)
                how_long_ago = string.Format("{0} {1} {2}", tspan.Hours, (tspan.Hours > 1) ? "Hours" : "Hour", TimeAgo(new TimeSpan(0, tspan.Minutes, tspan.Seconds), level + 1));
            else if (tspan.Minutes >= 1)
                how_long_ago = string.Format("{0} {1} {2}", tspan.Minutes, (tspan.Minutes > 1) ? "Minutes" : "Minute", TimeAgo(new TimeSpan(0, 0, tspan.Seconds), level + 1));
            else if (tspan.Seconds >= 1)
                how_long_ago = string.Format("{0} {1} ago", tspan.Seconds, (tspan.Seconds > 1) ? "Seconds" : "Second");        
            return how_long_ago;
        }
        public static string TimeAgoShorted(TimeSpan tspan) {
            string how_long_ago = "now";
            if (tspan.Days > 1)
                how_long_ago = string.Format("{0}d", tspan.Days);
            else if (tspan.Days == 1)
                how_long_ago = string.Format("1d");
            else if (tspan.Hours >= 1)
                how_long_ago = string.Format("{0}h", tspan.Hours);
            else if (tspan.Minutes >= 1)
                how_long_ago = string.Format("{0}m", tspan.Minutes);
            else if (tspan.Seconds >= 1)
                how_long_ago = string.Format("{0}s", tspan.Seconds);        
            return how_long_ago;
        }
        public static string TimeFromNowShorted(double futureTime) {
            TimeSpan tspan = new TimeSpan(DateTime.FromOADate(DateTime.Now.ToOADate() - futureTime).Ticks);
            string how_long_ago = "now";
            if (tspan.Days > 1)
                how_long_ago = string.Format("{0}d", tspan.Days);
            else if (tspan.Days == 1)
                how_long_ago = string.Format("1d");
            else if (tspan.Hours >= 1)
                how_long_ago = string.Format("{0}h", tspan.Hours);
            else if (tspan.Minutes >= 1)
                how_long_ago = string.Format("{0}m", tspan.Minutes);
            else if (tspan.Seconds >= 1)
                how_long_ago = string.Format("{0}s", tspan.Seconds);        
            return how_long_ago;
        }
        public static string MinifyLong(long value) {
            if (value >= 100000000000)
                return (value / 1000000000).ToString("#,0") + "B";
            if (value >= 10000000000)
                return (value / 1000000000D).ToString("0.#") + "B";
            if (value >= 100000000)
                return (value / 1000000).ToString("#,0") + "M";
            if (value >= 10000000)
                return (value / 1000000D).ToString("0.#") + "M";
            if (value >= 100000)
                return (value / 1000).ToString("#,0") + "K";
            if (value >= 10000)
                return (value / 1000D).ToString("0.#") + "K";
            return value.ToString("#,0");
        }
        public static int SecondsFromNowTo(DateTime target, int delay = 0) {
            return (int)target.Subtract(DateTime.Now).TotalSeconds;
        }
        public static int SecondsUntilNewDay(int delay = 0) {
            return SecondsFromNowTo(DateTime.Today.AddDays(1).AddSeconds(delay));
        }
        public static string ArrayToString(int[] holder, string saprator = ",") {
            string result = "";
            for(int i = 0; i < holder.Length; i++) {
                result += i < holder.Length - 1 ? $"{holder[i]}{saprator}" : $"{holder[i]}";
            }
            return result;
        }
        public static int[] StringToIntArray(string text) {
            return text != "" ? Array.ConvertAll<string, int>(text.Split(','), int.Parse) : new int[]{};
        }
        public static byte[] StringToByteArray(string text) {
            return text != "" ? Array.ConvertAll<string, byte>(text.Split(','), byte.Parse) : new byte[]{};
        }
    }
}