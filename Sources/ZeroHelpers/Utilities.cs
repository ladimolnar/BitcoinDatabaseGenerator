//-----------------------------------------------------------------------
// <copyright file="Utilities.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ZeroHelpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Contains various utilities with a general character.
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Raises an event accounting for the case when there are no subscription to the event and for 
        /// race conditions where the event could be unsubscribed from just after the test for a null event was done.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="targetEvent">The event that must be raised.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "This method is a helper supporting client code to actually raise an event.")]
        public static void RaiseEvent(object sender, EventHandler targetEvent)
        {
            // Prevent a race condition by copying the event handler.
            EventHandler targetEventCopy = targetEvent;

            if (targetEventCopy != null)
            {
                targetEventCopy(sender, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raises an event accounting for the case when there are no subscription to the event and for 
        /// race conditions where the event could be unsubscribed from just after the test for a null event was done.
        /// </summary>
        /// <typeparam name="T">The type of the event arguments.</typeparam>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="targetEvent">The event that must be raised.</param>
        /// <param name="eventArgs">The event arguments.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "This method is a helper supporting client code to actually raise an event.")]
        public static void RaiseEvent<T>(object sender, EventHandler<T> targetEvent, T eventArgs) where T : EventArgs
        {
            // Prevent a race condition by copying the event handler.
            EventHandler<T> targetEventCopy = targetEvent;

            if (targetEventCopy != null)
            {
                targetEventCopy(sender, eventArgs);
            }
        }
    }
}
