using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Skelecortex.Synchronization
{
    internal sealed class SyncCoordinator
    {
        private readonly Queue<WeakReference<SyncContext>> _awaiting = new Queue<WeakReference<SyncContext>>();
        private readonly object _syncRoot;
        private WeakReference<SyncContext> _heldBy;
        private volatile int _acquireCount;

        public SyncCoordinator (object syncRoot)
        {
            Debug.Assert(syncRoot != null);
            _syncRoot = syncRoot;
        }

        public SyncContext HeldBy
        {
            get => GetContext(_heldBy);
            private set
            {
                if (value is null)
                {
                    _heldBy = null;
                    return;
                }

                _heldBy = new WeakReference<SyncContext>(value);
            }
        }

        public int AcquireCount => _acquireCount;

        public void Acquire (SyncContext context)
        {
            Debug.Assert(context != null);

            lock (this)
            {
                if (context.IsDisposed)
                    throw new ObjectDisposedException("The sync context was disposed while attempting to acquire a lock.");

                if (TryIncrementHoldCount(context) ||
                    TryAcquireIfNotHeld(context))
                {
                    return;
                }

                Enqueue(context);

                var acquired = false;

                do
                {
                    Monitor.Wait(this);

                    acquired =
                        TryIncrementHoldCount(context) ||
                        TryAcquire(context);

                    if (!acquired)
                    {
                        if (!EnsureStillAwaiting(context))
                            throw new ObjectDisposedException("The sync context was disposed while attempting to acquire a lock.");

                        PulseIfNotHeld();
                    }

                } while (!acquired);
            }
        }

        public bool TryAcquire (SyncContext context, int timeout)
        {
            Debug.Assert(context != null);

            if (timeout == Timeout.Infinite)
            {
                Acquire(context);
                return true;
            }

            if (timeout < 0)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            var remaining = timeout;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (Monitor.TryEnter(this, remaining))
            {
                if (context.IsDisposed)
                    return false;

                remaining = CalculateRemaining(timeout, stopwatch);

                try
                {
                    if (TryIncrementHoldCount(context) ||
                        TryAcquireIfNotHeld(context))
                    {
                        return true;
                    }

                    Enqueue(context);

                    while (remaining > 0 && !context.IsDisposed)
                    {
                        if (!TryWaitForChange(context, remaining))
                            return false;

                        if (TryIncrementHoldCount(context) ||
                            TryAcquire(context))
                        {
                            return true;
                        }

                        if (!EnsureStillAwaiting(context))
                            return false;

                        PulseIfNotHeld();

                        remaining = CalculateRemaining(timeout, stopwatch);
                    }
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }

            return false;
        }

        public void Release (SyncContext context)
        {
            Debug.Assert(context != null);

            lock (this)
            {
                if (ReferenceEquals(_heldBy, context))
                {
                    _acquireCount--;
                    if (_acquireCount == 0)
                    {
                        _heldBy = null;
                        PulseIfAwaiting();
                    }
                }
            }
        }

        public void ReleaseForDisposedContext (SyncContext context)
        {
            Debug.Assert(context != null);

            lock (this)
            {
                if (ReferenceEquals(_heldBy, context))
                {
                    _acquireCount = 0;
                    _heldBy = null;

                    PulseIfAwaiting();
                }
                else if (Dequeue(context))
                {
                    Monitor.Pulse(this);
                }
            }
        }

        private SyncContext Peek () => _awaiting.Count == 0 ? null :
    GetContext(_awaiting.Peek());

        private bool Enqueue (SyncContext context)
        {
            Debug.Assert(Monitor.IsEntered(this));

            if (context is null || ReferenceEquals(context, HeldBy))
                return false;

            if (IsAwaiting(context))
                return true;

            _awaiting.Enqueue(new WeakReference<SyncContext>(context));
            return true;
        }

        private bool IsAwaiting (SyncContext context)
        {
            Debug.Assert(Monitor.IsEntered(this));

            if (context is null || _awaiting.Count == 0)
                return false;

            if (ReferenceEquals(GetContext(_awaiting.Peek()), context))
                return true;

            foreach (var contextRef in _awaiting)
            {
                var other = GetContext(contextRef);
                if (other is null)
                {
                    RemoveDeadAwaiters();
                    continue;
                }

                if (ReferenceEquals(context, other))
                    return true;
            }

            return false;
        }

        private bool Dequeue (SyncContext context)
        {
            Debug.Assert(Monitor.IsEntered(this));

            if (context is null || _awaiting.Count == 0)
                return false;

            if (ReferenceEquals(GetContext(_awaiting.Peek()), context))
            {
                _awaiting.Dequeue();
                return true;
            }

            foreach (var contextRef in _awaiting)
            {
                if (!contextRef.TryGetTarget(out SyncContext other))
                {
                    RemoveDeadAwaiters();
                    continue;
                }

                if (ReferenceEquals(context, other))
                {
                    var deadReferences = false;

                    var newQueue = new Queue<WeakReference<SyncContext>>(_awaiting.Count);
                    while (_awaiting.Count > 0)
                    {
                        var current = _awaiting.Dequeue();
                        var currentContext = GetContext(current);
                        if (ReferenceEquals(currentContext, context))
                            break;

                        if (currentContext != null)
                        {
                            newQueue.Enqueue(current);
                        }
                        else
                        {
                            deadReferences = true;
                        }
                    }

                    while (newQueue.Count > 0)
                        _awaiting.Enqueue(newQueue.Dequeue());

                    if (deadReferences)
                    {
                        PulseIfAwaiting();
                    }

                    return true;
                }
            }

            return false;
        }

        private void RemoveDeadAwaiters ()
        {
            Debug.Assert(Monitor.IsEntered(this));

            var newQueue = new Queue<WeakReference<SyncContext>>(_awaiting.Count);
            while (_awaiting.Count > 0)
            {
                var current = _awaiting.Dequeue();
                var currentContext = GetContext(current);
                if (currentContext is null)
                    continue;

                newQueue.Enqueue(current);
            }

            while (newQueue.Count > 0)
                _awaiting.Enqueue(newQueue.Dequeue());
        }

        private void PulseIfNotHeld ()
        {
            if (HeldBy is null)
            {
                _heldBy = null;

                // When not acquired, but the lock is available,
                // allow the next thread to try for a lock
                PulseIfAwaiting();
            }
        }

        private bool TryWaitForChange (SyncContext context, int timeout)
        {
            if (!Monitor.Wait(this, timeout))
            {
                Dequeue(context);
                return false;
            }

            return true;
        }

        private bool EnsureStillAwaiting (SyncContext context)
        {
            if (!IsAwaiting(context))
            {
                // Unexpected condition, but cover it just in case by allowing the 
                // next thread to try for lock
                PulseIfAwaiting();
                return false;
            }
            return true;
        }

        private bool TryIncrementHoldCount (SyncContext context)
        {
            if (ReferenceEquals(_heldBy, context))
            {
                _acquireCount++;

                // No need to notify - no awaiting if previously held
                return true;
            }

            return false;
        }

        private bool TryAcquireIfNotHeld (SyncContext context)
        {
            if (_awaiting.Count == 0 && _heldBy is null)
            {
                _heldBy = new WeakReference<SyncContext>(context);
                _acquireCount++;

                // No need to notify - no awaiters in queue
                return true;
            }

            return false;
        }

        private bool TryAcquire (SyncContext context)
        {
            var acquired = _awaiting.Count > 0 && HeldBy is null && ReferenceEquals(Peek(), context);
            if (acquired)
            {
                _heldBy = _awaiting.Dequeue();
                _acquireCount++;
                acquired = true;

                // Other threads may be waiting to acquire for the same context (let them know by saying "next")
                PulseIfAwaiting();
            }

            return acquired;
        }

        private void PulseIfAwaiting ()
        {
            Debug.Assert(Monitor.IsEntered(this));

            if (_awaiting.Count > 0)
            {
                Monitor.Pulse(this);
            }
        }

        private static int CalculateRemaining (int timeout, Stopwatch stopwatch) =>
            (int)Math.Max(0, timeout - stopwatch.ElapsedMilliseconds);

        private static SyncContext GetContext (WeakReference<SyncContext> contextReference) =>
            contextReference is null ? null : (contextReference.TryGetTarget(out SyncContext context) ? context : null);
    }
}
