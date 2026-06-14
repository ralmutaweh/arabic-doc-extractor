using System.Diagnostics;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.SemanticKernel;

namespace ArabicPdfReader.Observability
{
    public class TracingFunctionInvocationFilter : IFunctionInvocationFilter
    {
        private readonly ILogger<TracingFunctionInvocationFilter> logger;

        public TracingFunctionInvocationFilter(ILogger<TracingFunctionInvocationFilter> logger)
        {
            this.logger = logger;
        }

        public async Task OnFunctionInvocationAsync(
            FunctionInvocationContext context,
            Func<FunctionInvocationContext, Task> next
        )
        {
            await next(context);

            var activity = Activity.Current;

            if (activity is null)
            {
                logger.LogWarning("No Activity found for function {Function}.", context.Function.Name);
                return;
            }

            var result = context.Result;

            // Diagnostic: log metadata shape (remove once Usage shape confirmed)
            if (result.Metadata is not null)
            {
                logger.LogInformation("Metadata keys for {Function}: {Keys}",
                    context.Function.Name, string.Join(", ", result.Metadata.Keys));

                if (result.Metadata.TryGetValue("Usage", out var usage) && usage is not null)
                {
                    logger.LogInformation("Usage type for {Function}: {Type}, value: {Value}",
                        context.Function.Name, usage.GetType().FullName, usage);
                }
            }

            // Rendered prompt (only present for prompt functions)
            if (!string.IsNullOrEmpty(result.RenderedPrompt))
            {
                activity.AddEvent(new ActivityEvent(
                "semantic_kernel.prompt",
                tags: new ActivityTagsCollection
                {
                    { "semantic_kernel.function.prompt", result.RenderedPrompt }
                }));
            }

            // Response text
            var response = result.GetValue<object>()?.ToString();
            if (!string.IsNullOrEmpty(response))
            {
                activity.AddEvent(new ActivityEvent(
                "semantic_kernel.response",
                tags: new ActivityTagsCollection
                {
                    { "semantic_kernel.function.response", response }
                }));
            }

            // Token usage — extracted defensively since shape depends on connector
            if (result.Metadata?.TryGetValue("Usage", out var usageObj) == true && usageObj is not null)
            {
                int? promptTokens = null;
                int? completionTokens = null;

                if (usageObj is Microsoft.Extensions.AI.UsageDetails ud)
                {
                    promptTokens = (int?)ud.InputTokenCount;
                    completionTokens = (int?)ud.OutputTokenCount;
                }
                if (promptTokens is not null && completionTokens is not null)
                {
                    activity.SetTag("semantic_kernel.usage.prompt_tokens", promptTokens);
                    activity.SetTag("semantic_kernel.usage.completion_tokens", completionTokens);
                }
                else
                {
                    logger.LogWarning("Unable to extract token usage for {Function} from type {Type}.",
                        context.Function.Name, usageObj.GetType().FullName);
                }
            }
        }
    }
}