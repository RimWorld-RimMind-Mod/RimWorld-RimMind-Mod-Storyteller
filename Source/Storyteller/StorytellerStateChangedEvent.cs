using System;
using RimMind.Domain.Events;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Api;

namespace RimMind.Storyteller
{
    /// <summary>
    /// Published on the Core bus when storyteller state that feeds context changes (player chat
    /// turns, incidents, tension). The cached storyteller context keys name <see cref="EventName"/>
    /// as their invalidation trigger.
    /// </summary>
    public sealed class StorytellerStateChangedEvent : AgentBusEvent
    {
        /// <summary>Bus event name used by context-key invalidation triggers.</summary>
        public const string EventName = "StorytellerEvent";

        /// <summary>Registers <see cref="EventName"/> with the Core bus. Call before registering context keys.</summary>
        internal static void Register()
        {
            try { RimMindAPI.Bus.RegisterEventType<StorytellerStateChangedEvent>(EventName); }
            catch (Exception ex) { RimMindErrors.Warn($"[RimMind-Storyteller] Event type registration failed: {ex.Message}"); }
        }

        /// <summary>Publishes from any thread; never throws.</summary>
        internal static void Publish()
        {
            try { RimMindAPI.Bus.PublishOnMainThread(new StorytellerStateChangedEvent()); }
            catch (Exception ex) { RimMindErrors.Warn($"[RimMind-Storyteller] State change notification failed: {ex.Message}"); }
        }
    }
}
