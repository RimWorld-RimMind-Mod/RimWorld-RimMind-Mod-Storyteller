using System;

namespace RimMind.Storyteller
{
    public sealed class StorytellerRequestState<TIncident>
        where TIncident : class
    {
        private TIncident? _pendingIncident;
        private long _generation;
        private long _activeRequestToken;

        public bool HasPendingRequest { get; private set; }
        public bool HasPendingResult => _pendingIncident != null;
        public int LastDispatchTick { get; private set; } = -99999;
        public int LastSuccessTick { get; private set; } = -99999;
        public int LastFailTick { get; private set; } = -99999;

        public bool TryDispatch(
            Action<long> dispatch,
            int failureTick,
            out Exception? error)
        {
            if (dispatch == null)
                throw new ArgumentNullException(nameof(dispatch));

            if (HasPendingRequest)
            {
                error = null;
                return false;
            }

            HasPendingRequest = true;
            LastDispatchTick = failureTick;
            _activeRequestToken = ++_generation;
            long token = _activeRequestToken;
            try
            {
                dispatch(token);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                Fail(token, failureTick);
                error = ex;
                return false;
            }
        }

        public bool IsCurrent(long token)
            => HasPendingRequest && _activeRequestToken == token;

        public bool CancelIfTimedOut(int currentTick, int timeoutTicks = 15000)
        {
            if (!HasPendingRequest || LastDispatchTick < 0)
                return false;

            if (currentTick - LastDispatchTick <= timeoutTicks)
                return false;

            CancelRequest();
            return true;
        }

        public void CancelRequest()
        {
            if (!HasPendingRequest)
                return;

            HasPendingRequest = false;
            _activeRequestToken = 0;
            _generation++;
        }

        public bool Fail(long token, int tick)
        {
            if (!IsCurrent(token))
                return false;

            HasPendingRequest = false;
            _activeRequestToken = 0;
            LastFailTick = tick;
            return true;
        }

        public bool Publish(long token, TIncident incident, int tick)
        {
            if (!IsCurrent(token))
                return false;

            HasPendingRequest = false;
            _activeRequestToken = 0;
            _pendingIncident = incident;
            LastSuccessTick = tick;
            return true;
        }

        public bool TryTake(out TIncident? incident)
        {
            incident = _pendingIncident;
            _pendingIncident = null;
            return incident != null;
        }
    }
}
