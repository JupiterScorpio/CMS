﻿using System.Threading.Tasks;
using MrCMS.Batching.Entities;

namespace MrCMS.Batching
{
    public class DefaultBatchJobExecutor : IBatchJobExecutor
    {
        private readonly ISetBatchJobExecutionStatus _setBatchJobExecutionStatus;

        public DefaultBatchJobExecutor(ISetBatchJobExecutionStatus setBatchJobExecutionStatus)
        {
            _setBatchJobExecutionStatus = setBatchJobExecutionStatus;
        }

        public async Task<BatchJobExecutionResult> Execute(BatchJob batchJob)
        {
            var message = string.Format("There is no executor for this job. To create one, implement {0}<{1}>",
                typeof(IBatchJobExecutor).FullName,
                batchJob.GetType().FullName);
            var batchJobExecutionResult = BatchJobExecutionResult.Failure(message);
            await _setBatchJobExecutionStatus.Complete(batchJob, batchJobExecutionResult);
            return await Task.FromResult(batchJobExecutionResult);
        }


        public bool UseAsync { get; private set; }
    }
}