// ────── ╔╗
// ╔═╦╦═╦╦╬╣ Dispatcher.cs
// ║║║║╬║╔╣║ Implements the IDispatcher interface for the browser (Blazor WebAssembly)
// ╚╩═╩═╩╝╚╝ ───────────────────────────────────────────────────────────────────────────────────────
using System.Collections.Concurrent;
namespace Nori;

#region class WebDispatcher ------------------------------------------------------------------------
/// <summary>WebDispatcher implements an IDispatcher on top of the browser event loop</summary>
/// The browser (and .NET on browser-wasm) is single-threaded: the one thread that runs all
/// managed code IS the UI thread, so CheckAccess is almost always true. Post/InvokeAsync must
/// still never run inline (WPF semantics, see GLFWDispatcher) - queued work is drained by a
/// zero-delay System.Threading.Timer, which .NET-on-wasm schedules onto the browser event loop
/// (there are no real threads involved).
class WebDispatcher : IDispatcher {
   // Methods ------------------------------------------------------------------
   /// <summary>Returns true if we are currently on the UI thread</summary>
   public bool CheckAccess () => mThreadID == Environment.CurrentManagedThreadId;

   /// <summary>Schedules an action to be executed asynchronously (returns an awaitable Task)</summary>
   public Task InvokeAsync (Action act) {
      TaskCompletionSource<object?> tcs = new (TaskCreationOptions.RunContinuationsAsynchronously);
      Enqueue (() => {
         try { act (); tcs.SetResult (null); } catch (Exception ex) { tcs.SetException (ex); }
      });
      return tcs.Task;
   }

   /// <summary>Schedules a function to be executed asynchronously (returns an awaitable Task with result)</summary>
   public Task<T> InvokeAsync<T> (Func<T> func) {
      TaskCompletionSource<T> tcs = new (TaskCreationOptions.RunContinuationsAsynchronously);
      Enqueue (() => {
         try { tcs.SetResult (func ()); } catch (Exception ex) { tcs.SetException (ex); }
      });
      return tcs.Task;
   }

   /// <summary>Posts a fire-and-forget action onto the browser event loop</summary>
   public void Post (Action act) => Enqueue (act);

   // Implementation -----------------------------------------------------------
   void Enqueue (Action act) {
      mQueue.Enqueue (act);
      // Schedule a drain on the browser event loop (never inline, even from the UI thread)
      if (Interlocked.CompareExchange (ref mScheduled, 1, 0) == 0)
         mDrainTimer = new Timer (_ => Drain (), null, 0, Timeout.Infinite);
   }

   void Drain () {
      mScheduled = 0;
      mDrainTimer?.Dispose (); mDrainTimer = null;
      while (mQueue.TryDequeue (out var act)) {
         try { act (); }
         catch (Exception ex) { Lib.Trace ($"Dispatcher work exception: {ex}"); }
      }
   }

   readonly int mThreadID = Environment.CurrentManagedThreadId;
   readonly ConcurrentQueue<Action> mQueue = [];
   int mScheduled;
   Timer? mDrainTimer;
}
#endregion

#region class WebSyncContext -----------------------------------------------------------------------
// SynchronizationContext that delegates to the WebDispatcher (mirrors GLFWSyncContext).
// Installed by WebHost.Init so that Lib.Post and async/await continuations flow through
// the dispatcher.
class WebSyncContext (IDispatcher dispatcher) : SynchronizationContext {
   public override void Post (SendOrPostCallback d, object? state) => mDispatcher.Post (() => d (state));
   public override void Send (SendOrPostCallback d, object? state) => mDispatcher.Send (() => d (state));
   public override SynchronizationContext CreateCopy () => this;

   readonly IDispatcher mDispatcher = dispatcher;
}
#endregion
