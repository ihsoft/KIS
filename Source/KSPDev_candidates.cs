// This is an intermediate module for methods and classes that are considred as candidates for
// KSPDev Utilities. Ideally, this module is always empty but there may be short period of time
// when new functionality lives here and not in KSPDev.

using System;
using System.Collections;
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
