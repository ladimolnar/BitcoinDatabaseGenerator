//-----------------------------------------------------------------------
// <copyright file="TaskDispatcher.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ZeroHelpers
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Class <see cref="TaskDispatcher"/> can be used to dispatch the execution of code on a number of background threads.
    /// </summary>
    public class TaskDispatcher
    {
        /// <summary>
        /// Used for internal synchronization.
        /// </summary>
        private readonly object lockObject;

        /// <summary>
        /// An array containing <see cref="Task"/> instances or null.
        /// Each <see cref="Task"/> instance corresponds to a unit of work that was dispatched to a background thread.
        /// </summary>
        private readonly Task[] taskArray;

        /// <summary>
        /// The first exception encountered by any background thread.
        /// </summary>
        private Exception taskException;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDispatcher" /> class.
        /// </summary>
        /// <param name="maxThreads">
        /// The number of background threads that will be made available for executing code.
        /// If 0 then will  default to the number of processors as reported by <c>Environment.ProcessorCount</c>.
        /// </param>
        public TaskDispatcher(int maxThreads = 0)
        {
            if (maxThreads == 0)
            {
                maxThreads = Environment.ProcessorCount;
            }

            this.lockObject = new object();
            this.taskArray = new Task[maxThreads];

            //// Debug.WriteLine("TaskDispatcher. Initialization with {0} tasks.", maxThreads);
        }

        /// <summary>
        /// Dispatches work on a background thread and if necessary waits until a thread that can take that work becomes available. 
        /// The async task that is returned completes when the work specified by <paramref name="action"/>
        /// was assigned to a thread and not when that work actually completes.
        /// </summary>
        /// <param name="action">
        /// A delegate that will carry out the work that must be dispatched.
        /// </param>
        /// <returns>
        /// A instance of <see cref="Task"/> class that completes when 
        /// the work specified by <paramref name="action"/> was assigned to a thread.
        /// </returns>
        public async Task DispatchWorkAsync(Action action)
        {
            bool isWorkDispatched = false;

            while (isWorkDispatched == false)
            {
                Task[] pendingTasks;

                if (this.taskException != null)
                {
                    throw this.taskException;
                }

                lock (this.lockObject)
                {
                    pendingTasks = this.taskArray.Where(t => t != null).ToArray();

                    if (pendingTasks.Length < this.taskArray.Length)
                    {
                        // There must be at least one element in taskList that became available. 
                        // Let's find out which one and assign it some work.
                        for (int taskIndex = 0; taskIndex < this.taskArray.Length; taskIndex++)
                        {
                            if (this.taskArray[taskIndex] == null)
                            {
                                // Note that we will not wait for the completion of the actual work.
                                //      This is by design and allows this method to complete when the work was dispatched and not when it actually completed.
                                Task t = this.DispatchWork(action, taskIndex);

                                isWorkDispatched = true;
                                break;
                            }
                        }
                    }
                }

                if (isWorkDispatched == false)
                {
                    await Task.WhenAny(pendingTasks);
                }
            }
        }

        /// <summary>
        /// Waits until all pending work is complete. 
        /// Do not call DispatchWork after a call to WaitForAllWorkToComplete and before WaitForAllWorkToComplete completes.
        /// </summary>
        /// <returns>
        /// A Task that completes when all pending work completes.
        /// </returns>
        public async Task WaitForAllWorkToComplete()
        {
            Task[] pendingTasks = null;

            lock (this.lockObject)
            {
                pendingTasks = this.taskArray.Where(t => t != null).ToArray();
            }

            await Task.WhenAll(pendingTasks);

            if (this.taskException != null)
            {
                throw this.taskException;
            }
        }

        /// <summary>
        /// Dispatches and executes work on a background thread.
        /// </summary>
        /// <param name="action">
        /// A delegate that will carry out the work that must be dispatched.
        /// </param>
        /// <param name="taskIndex">
        /// An index corresponding to an element in <c>taskArray</c>. That element will be set 
        /// to the <see cref="Task"/> instance associated with the work being executed.
        /// </param>
        /// <returns>
        /// A instance of <see cref="Task"/> class that completes when 
        /// the work specified by <paramref name="action"/> completes.
        /// </returns>
        private async Task DispatchWork(Action action, int taskIndex)
        {
            //// Debug.WriteLine("TaskDispatcher. Task #{0}. Processing start.", taskIndex);
            this.taskArray[taskIndex] = Task.Run(action);

            try
            {
                await this.taskArray[taskIndex];
            }
            finally
            {
                if (this.taskArray[taskIndex].Status == TaskStatus.Faulted && this.taskException == null)
                {
                    this.taskException = this.taskArray[taskIndex].Exception;
                }

                lock (this.lockObject)
                {
                    //// Debug.WriteLine("TaskDispatcher. Task #{0}. Processing complete. Task Status: {1}.", taskIndex, this.taskArray[taskIndex].Status);
                    this.taskArray[taskIndex] = null;
                }
            }
        }
    }
}
