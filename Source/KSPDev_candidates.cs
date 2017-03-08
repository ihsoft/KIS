// This is an intermediate module for methods and classes that are considred as candidates for
// KSPDev Utilities. Ideally, this module is always empty but there may be short period of time
// when new functionality lives here and not in KSPDev.

using KSPDev.ModelUtils;
using KSPDev.LogUtils;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KSPDev.ProcessingUtils {

/// <summary>Candidate to merge with KSPDev.ProcessingUtils.AsyncCall</summary>
public static class AsyncCall2 {
  /// <summary>Async version of <see cref="WaitForPhysics"/>.</summary>
  /// <returns>Enumerator that can be used as coroutine target.</returns>
  /// <seealso cref="WaitForPhysics"/>
  /// <example>
  /// This method is useful when synchronous wait is needed within a coroutine. Instead of
  /// implementing own loops just return the waiting enumerator. The code below will log 10 waiting
  /// lines between "Started" and "Ended" records. 
  /// <code><![CDATA[
  /// class MyComponent : MonoBehaviour {
  ///   void Awake() {
  ///     StartCoroutine(MyDelayedFn());
  ///   }
  ///   IEnumerator MyDelayedFn() {
  ///     Debug.Log("Started!");
  ///     yield return AsyncCall.AsyncWaitForPhysics(
  ///        10,
  ///        () => false,
  ///        update: frame => Debug.LogFormat("...waiting frame {0}...", frame));
  ///     Debug.Log("Ended!");
  ///   }
  /// }
  /// ]]></code>
  /// </example>
  public static IEnumerator AsyncWaitForPhysics(int maxFrames, Func<bool> waitUntilFn,
                                                Action success = null,
                                                Action failure = null,
                                                Action<int> update = null,
                                                bool traceUpdates = false) {
    for (var i = 0; i < maxFrames; i++) {
      if (update != null) {
        update(i);
      }
      var res = waitUntilFn();
      if (traceUpdates) {
        Debug.LogFormat("Waiting for physics: frame={0}, condition={1}", i, res);
      }
      if (res) {
        break;
      }
      yield return new WaitForFixedUpdate();
    }
    if (waitUntilFn()) {
      if (success != null) {
        success();
      }
    } else {
      if (failure != null) {
        failure();
      }
    }
  }

  /// <summary>
  /// Waits for the specified condition for limited number of fixed frame updates.
  /// </summary>
  /// <remarks>
  /// Can be used when a particular state of the game is required to perform an action. Method
  /// provides ability to define for how long to wait, what to do while waiting, and what to execute
  /// when target state is reached or missed.
  /// </remarks>
  /// <param name="mono">
  /// Unity object to run coroutine on. If this object dies then waiting will be aborted without
  /// calling any callbacks.
  /// </param>
  /// <param name="maxFrames">Number of fixed frame update to wait before giving up.</param>
  /// <param name="waitUntilFn">
  /// State checking function. It should return <c>true</c> once target state is reached.
  /// </param>
  /// <param name="success">Callback to execute when state has been successfully reached.</param>
  /// <param name="failure">
  /// Callabck to execute when state has not been reached before frame update limit is exhausted.
  /// </param>
  /// <param name="update">
  /// Callback to execute every fixed frame update while waiting. This callabck will be called at
  /// least once, and it happens immediately.
  /// </param>
  /// <param name="traceUpdates">When <c>true</c> every wiating cycle will be logged.</param>
  /// <returns>Enumerator that can be used as coroutine target.</returns>
  /// <seealso cref="WaitForPhysics"/>
  public static Coroutine WaitForPhysics(
      MonoBehaviour mono,  int maxFrames, Func<bool> waitUntilFn,
      Action success = null,
      Action failure = null,
      Action<int> update = null,
      bool traceUpdates = false) {
    return mono.StartCoroutine(
        AsyncWaitForPhysics(maxFrames, waitUntilFn, success, failure, update, traceUpdates));
  }
}

}  // namespace

namespace KSPDev.LogUtils {

/// <summary>
/// Merge with KSPDev.LogUtils.DbgFormatter
/// </summary>
public static class DbgFormatter2 {
  /// <summary>Returns string represenation of a vector with more precision.</summary>
  /// <param name="vec">Vector to dump.</param>
  /// <returns>String representation.</returns>
  public static string Vector(Vector3 vec) {
    return string.Format("({0:0.0###}, {1:0.0###}, {2:0.0###})", vec.x, vec.y, vec.z);
  }

  /// <summary>Returns string represenation of a quaternion with more precision.</summary>
  /// <param name="rot">Quaternion to dump.</param>
  /// <returns>String representation.</returns>
  public static string Quaternion(Quaternion rot) {
    return string.Format("({0:0.0###}, {1:0.0###}, {2:0.0###}, {3:0.0###})",
                         rot.x, rot.y, rot.z, rot.w);
  }

  /// <summary>Returns full string path for the tranform.</summary>
  /// <param name="obj">Object to make path for.</param>
  /// <param name="parent">Optional parent to use a root.</param>
  /// <returns>Full string path to the root.</returns>
  public static string TranformPath(Transform obj, Transform parent = null) {
    return string.Join("/", Hierarchy.GetFullPath(obj, parent));
  }

  /// <summary>Returns full string path for the game object.</summary>
  /// <param name="obj">Object to make path for.</param>
  /// <param name="parent">Optional parent to use a root.</param>
  /// <returns>Full string path to the root.</returns>
  public static string TranformPath(GameObject obj, Transform parent = null) {
    return TranformPath(obj.transform, parent);
  }
}

}  // namespace

namespace KSPDev.ModelUtils {
  
/// <summary>
/// Merge with KSPDev.ModelUtils.Hierarchy
/// </summary>
public static class Hierarchy2 {
  /// <summary>
  /// Checks target string against a simple pattern which allows prefix, suffix, and contains match.
  /// The match is case-sensitive.
  /// </summary>
  /// <param name="pattern">
  /// Pattern to match for:
  /// <list type="bullet">
  /// <item>If pattern ends with <c>*</c> then it's a match by prefix.</item>
  /// <item>If pattern starts with <c>*</c> then it's a match by suffix.</item>
  /// <item>
  /// If pattern starts and ends with <c>*</c> then pattern is searched anywhere in the target.
  /// </item>
  /// </list>
  /// </param>
  /// <param name="target">Target string to check.</param>
  /// <returns><c>true</c> if pattern matches the target.</returns>
  public static bool PatternMatch(string pattern, string target) {
    if (pattern.Length > 0 && pattern[0] == '*') {
      return target.EndsWith(pattern.Substring(1), StringComparison.Ordinal);
    }
    if (pattern.Length > 0 && pattern[pattern.Length - 1] == '*') {
      return target.StartsWith(pattern.Substring(0, pattern.Length - 1), StringComparison.Ordinal);
    }
    if (pattern.Length > 1 && pattern[0] == '*' && pattern[pattern.Length - 1] == '*') {
      return target.Contains(pattern.Substring(1, pattern.Length - 2));
    }
    return target == pattern;
  }

  /// <summary>Finds transform in the hirerachy by a provided path.</summary>
  /// <remarks>
  /// Every element of the path may specify an exact transform name or a partial match pattern:
  /// <list type="bullet">
  /// <item>
  /// <c>*</c> - any name matches. Such patterns can be nested to specify the desired level of
  /// nesting. E.g. <c>*/*/a</c> will look for name <c>a</c> in the grandchildren.
  /// </item>
  /// <item>
  /// <c>*</c> as a prefix - the name is matched by suffix. E.g. <c>*a</c> matches any name that
  /// ends with <c>a</c>.
  /// </item>
  /// <item>
  /// <c>*</c> as a suffix - the name is matched by prefix. E.g. <c>a*</c> matches any name that
  /// starts with <c>a</c>.
  /// </item>
  /// <item>
  /// <c>**</c> - any <i>path</i> matches. What will eventually be found depends on the pattern to
  /// the right of <c>**</c>. The path match pattern does a "breadth-first" search, i.e. it tries to
  /// find the shortest path possible. E.g. <c>**/a/b</c> will go through all the nodes starting
  /// from the parent until path <c>a/b</c> is found. Be careful with this pattern since in case of
  /// not matching anything it will walk thought the <i>whole</i> hirerachy.
  /// </item>
  /// </list>
  /// <para>
  /// All patterns except <c>**</c> may have a matching index. It can be used to resolve matches
  /// when there are multiple objects found with the same name and at the <i>same level</i>. E.g. if
  /// there are two objects with name "a" at the root level then the first one can be accessed by
  /// pattern <c>a:0</c>, and the second one by pattern <c>a:1</c>.
  /// </para>
  /// <para>
  /// Path search is <i>slow</i> since it needs walking though the hierarchy nodes. In the worst
  /// case all the nodes will be visited. Don't use this method in the performance demanding
  /// methods.
  /// </para>
  /// </remarks>
  /// <param name="parent">Transfrom to start looking from.</param>
  /// <param name="names">Path elements.</param>
  /// <returns>Transform or <c>null</c> if nothing found.</returns>
  /// <example>
  /// Given the following hierarchy:
  /// <code lang="c++"><![CDATA[
  /// // a
  /// // + b
  /// // | + c
  /// // | | + c1
  /// // | | + d
  /// // | + c
  /// // |   + d
  /// // |     + e
  /// // |       + e1
  /// // + abc
  /// ]]></code>
  /// <para>The following pattern/output will be possible:</para>
  /// <code><![CDATA[
  /// // a/b/c/d/e/e1 => node "e1"
  /// // a/b/*/d/e/e1 => node "e1"
  /// // a/b/*/*/e/e1 => node "e1"
  /// // a/b/c/c1 => node "с1"
  /// // a/b/*:0 => branch "a/b/c/c1", node "c"
  /// // a/b/*:1 => branch "a/b/c/d/e/e1", node "c"
  /// // a/b/c:1/d => branch "a/b/c/d/e/e1", node "d"
  /// // **/e1 => node "e1"
  /// // **/c1 => node "c1"
  /// // **/c/d => AMBIGUITY! The first found branch will be taken
  /// // a/**/e1 => node "e1"
  /// // *bc => node "abc"
  /// // ab* => node "abc"
  /// // *b* => node "abc"
  /// ]]></code>
  /// </example>
  public static Transform FindTransformByPath(Transform parent, string[] names) {
    if (names.Length == 0) {
      return parent;
    }
    // Try each child of the parent.
    var pair = names[0].Split(':');  // Separate index specifier if any.
    var pattern = pair[0];
    var reducedNames = names.Skip(1).ToArray();
    var index = pair.Length > 1 ? Math.Abs(int.Parse(pair[1])) : -1;
    for (var i = 0; i < parent.childCount; ++i) {
      var child = parent.GetChild(i);
      Transform branch = null;
      // "**" means "zero or more levels", so try parent's level first.
      if (pattern == "**") { 
        branch = FindTransformByPath(parent, reducedNames);
      }
      // Try all children treating "**" as "*" (one level).
      if (branch == null
          && (pattern == "*" || pattern == "**" || PatternMatch(pattern, child.name))) {
        if (index == -1 || index-- == 0) {
          branch = FindTransformByPath(child, reducedNames);
        }
      }
      if (branch != null) {
        return branch;
      }
    }

    // If "**" didn't match at this level the try it at the lover levels. For this just make a new
    // path "*/**" to go thru all the children and try "**" on them.
    if (pattern == "**") {
      var extendedNames = names.ToList();
      extendedNames.Insert(0, "*");
      return FindTransformByPath(parent, extendedNames.ToArray());
    }
    return null;
  }
}

}  // namespace
